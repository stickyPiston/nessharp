using System;

namespace NesSharp
{

    using Cycle = Action<CPU>;

    public partial class CPU
    {
        // Micro-micro-instructions
        private bool CheckPending(bool irq = true)
        {
            if (pending != null && (irq || pending == HardwareInterrupt.NMI))
            {
                // Poll next interrupt
                SetInstruction(pending == HardwareInterrupt.NMI ? NMIInstruction : IRQInstruction);
                return true;
            }
            return false;
        }

        private void SetInstructionCheckPending(Instruction instr)
        {
            if (CheckPending())
            {
                this.cycle = 255; // wraps back to 0 next cycle
            }
            else
            {
                SetInstruction(instr);
                this.cycle = 0;
            }
        }

        private byte Read(ushort addr)
        {
        #if DEBUG
            this._read = true;
            this._addr = addr;
            this._data = this.bus.Read(addr);
            return this._data;
        #else
            return this.bus.Read(addr);
        #endif
        }

        private void Write(ushort addr, byte b)
        {
        #if DEBUG
            this._read = false;
            this._addr = addr;
            this._data = b;
        #endif
            this.bus.Write(addr, b);
        }

        private void SetFlags(byte b)
        {
            this.P.N = (byte) (b >> 7);
            this.P.Z = Flags.Zero(b);
        }

        private byte Add(byte a, byte b, byte c)
        {
            ushort res = (ushort) ((ushort) a + b + c);
            this.P.V = (byte) (((~(a ^ b)) & (a ^ (byte) res)) >> 7);
            this.P.C = (byte) (res >> 8);
            return (byte) res;
        }

        private void Branch(bool b)
        {
            byte operand = this.val;
            byte next = Read(this.PC);

            if (b) {
                // Branch
                // Save PCH for later
                unchecked {
                    sbyte a = (sbyte) operand;
                    ushort branch = (ushort) (this.PC + a);
                    this.val = (byte) (branch >> 8);
                }

                // Set PCL
                byte PCL = (byte) this.PC;
                unchecked { PCL += operand; }
                this.PC &= 0xFF00;
                this.PC |= PCL;
            } else {
                // Next instruction
                unchecked { this.PC += 1; }
                SetInstructionCheckPending(instructions[next]);
            }
        }

        // Micro-instructions
        private static Cycle DummyFetchPC = cpu => cpu.Read(cpu.PC);
        private static Cycle DummyPeekStack = cpu => cpu.Read((ushort) (0x100 | cpu.S));

        private static Cycle FetchPC = cpu => {
            cpu.val = cpu.Read(cpu.PC);
            unchecked { cpu.PC += 1; }
        };

        private static Cycle DummyPushStack = cpu => {
            cpu.Read((ushort) (0x100 | cpu.S));
            unchecked { cpu.S -= 1; }
        };

        private static Cycle PushPCH = cpu => {
            cpu.Write((ushort) (0x100 | cpu.S), (byte) (cpu.PC >> 8));
            unchecked { cpu.S -= 1; }
        };

        private static Cycle PushPCL = cpu => {
            cpu.Write((ushort) (0x100 | cpu.S), (byte) cpu.PC);
            unchecked { cpu.S -= 1; }
        };

        private static Cycle PushA = cpu => {
            cpu.Write((ushort) (0x100 | cpu.S), cpu.A);
            unchecked { cpu.S -= 1; }
        };

        private static Cycle IncSP = cpu => {
            cpu.Read((ushort) (0x100 | cpu.S));
            unchecked { cpu.S += 1; }
        };

        private static Cycle PullPCL = cpu => {
            cpu.PC &= 0xFF00;
            cpu.PC |= cpu.Read((ushort) (0x100 | cpu.S));
            unchecked { cpu.S += 1; }
        };

        private static Cycle PullPCH = cpu => {
            cpu.PC &= 0x00FF;
            cpu.PC |= (ushort) (cpu.Read((ushort) (0x100 | cpu.S)) << 8);
        };

        private static Cycle PullA = cpu => {
            cpu.A = cpu.Read((ushort) (0x100 | cpu.S));
            cpu.SetFlags(cpu.A);
        };

        private static Cycle PullP = cpu => {
            cpu.P.Read(cpu.Read((ushort) (0x100 | cpu.S)));
        };

        private static Cycle ANDimm = cpu => {
            cpu.A &= cpu.Read(cpu.PC);
            cpu.SetFlags(cpu.A);
            unchecked { cpu.PC += 1; }
        };

        private static Cycle ORAimm = cpu => {
            cpu.A |= cpu.Read(cpu.PC);
            cpu.SetFlags(cpu.A);
            unchecked { cpu.PC += 1; }
        };

        private static Cycle EORimm = cpu => {
            cpu.A ^= cpu.Read(cpu.PC);
            cpu.SetFlags(cpu.A);
            unchecked { cpu.PC += 1; }
        };

        private static Cycle ADCPC = cpu => {
            byte add = cpu.Read(cpu.PC);

            cpu.A = cpu.Add(cpu.A, add, cpu.P.C);
            cpu.SetFlags(cpu.A);

            unchecked { cpu.PC += 1; }
        };

        private static Cycle SBCPC = cpu => {
            byte add = (byte) ~cpu.Read(cpu.PC);

            cpu.A = cpu.Add(cpu.A, add, cpu.P.C);
            cpu.SetFlags(cpu.A);

            unchecked { cpu.PC += 1; }
        };

        private static Cycle CMPPC = cpu => {
            ushort q = (ushort) (((ushort) cpu.A | 0x0100) - cpu.Read(cpu.PC));

            cpu.P.C = (byte) (q >> 8);
            cpu.SetFlags((byte) q);

            unchecked { cpu.PC += 1; }
        };

        private static Cycle CPXPC = cpu => {
            ushort q = (ushort) (((ushort) cpu.X | 0x0100) - cpu.Read(cpu.PC));

            cpu.P.C = (byte) (q >> 8);
            cpu.SetFlags((byte) q);

            unchecked { cpu.PC += 1; }
        };

        private static Cycle CPYPC = cpu => {
            ushort q = (ushort) (((ushort) cpu.Y | 0x0100) - cpu.Read(cpu.PC));

            cpu.P.C = (byte) (q >> 8);
            cpu.SetFlags((byte) q);

            unchecked { cpu.PC += 1; }
        };

        private static Cycle JumpPC = cpu => {
            // fetch high address
            cpu.PC = (ushort) (cpu.Read(cpu.PC) << 8);

            // copy low address byte
            cpu.PC |= cpu.val;
        };

        private static Cycle LDAPC = cpu => {
            cpu.A = cpu.Read(cpu.PC);
            cpu.SetFlags(cpu.A);

            unchecked { cpu.PC += 1; }
        };

        private static Cycle LDXPC = cpu => {
            cpu.X = cpu.Read(cpu.PC);
            cpu.SetFlags(cpu.X);

            unchecked { cpu.PC += 1; }
        };

        private static Cycle LDYPC = cpu => {
            cpu.Y = cpu.Read(cpu.PC);
            cpu.SetFlags(cpu.Y);

            unchecked { cpu.PC += 1; }
        };

        private static Cycle STAzpg = cpu => cpu.Write(cpu.val, cpu.A);
        private static Cycle STXzpg = cpu => cpu.Write(cpu.val, cpu.X);

        private static Cycle BITzpg = cpu => {
            byte test = cpu.Read(cpu.val);
            cpu.P.N = (byte) (test >> 7);
            cpu.P.V = (byte) (test >> 6 & 1);
            cpu.P.Z = Flags.Zero((byte) (test & cpu.A));
        };

        private static Cycle SEC = cpu => {
            cpu.P.C = 1;
            cpu.Read(cpu.PC);
        };

        private static Cycle CLC = cpu => {
            cpu.P.C = 0;
            cpu.Read(cpu.PC);
        };

        private static Cycle SEI = cpu => {
            cpu.P.I = 1;
            cpu.Read(cpu.PC);
        };

        private static Cycle SED = cpu => {
            cpu.P.D = 1;
            cpu.Read(cpu.PC);
        };

        private static Cycle CLD = cpu => {
            cpu.P.D = 0;
            cpu.Read(cpu.PC);
        };

        private static Cycle CLV = cpu => {
            cpu.P.V = 0;
            cpu.Read(cpu.PC);
        };

        private static Cycle BCS = cpu => cpu.Branch(cpu.P.C == 1);
        private static Cycle BCC = cpu => cpu.Branch(cpu.P.C == 0);
        private static Cycle BEQ = cpu => cpu.Branch(cpu.P.Z == 1);
        private static Cycle BNE = cpu => cpu.Branch(cpu.P.Z == 0);
        private static Cycle BVS = cpu => cpu.Branch(cpu.P.V == 1);
        private static Cycle BVC = cpu => cpu.Branch(cpu.P.V == 0);
        private static Cycle BMI = cpu => cpu.Branch(cpu.P.N == 1);
        private static Cycle BPL = cpu => cpu.Branch(cpu.P.N == 0);

        private static Cycle FixPC = cpu => {
            byte PCH = cpu.val;
            byte next = cpu.Read(cpu.PC);

            if ((byte) (cpu.PC >> 8) != PCH) {
                // Different page
                cpu.PC &= 0x00FF;
                cpu.PC |= (ushort) (PCH << 8);
            } else {
                // Next instruction
                unchecked { cpu.PC += 1; }

                // "a taken non-page-crossing branch ignores IRQ/NMI during its last clock, so that next instruction executes before the IRQ"
                if (cpu.pending != null && cpu.previous == null) {
                    cpu.SetInstruction(instructions[next]);
                } else {
                    cpu.SetInstructionCheckPending(instructions[next]);
                }
            }

        };

        private static Cycle PushP(bool B)
        {
            if (B) return cpu => {
                // Interrupt hijack (all hardware interrupts)
                cpu.CheckPending(true);

                cpu.P.B = 1; // Set B flag

                cpu.Write((ushort) (0x100 | cpu.S), cpu.P.Write());
                unchecked { cpu.S -= 1; }
            };
            else return cpu => {
                // Interrupt hijack (only NMI)
                cpu.CheckPending(false);

                if (cpu.pending == HardwareInterrupt.NMI) {
                    // Reset NMI on handle
                    cpu.pending = null;
                }

                cpu.P.B = 0; // Unset B flag

                cpu.Write((ushort) (0x100 | cpu.S), cpu.P.Write());
                unchecked { cpu.S -= 1; }
            };
        }

        private static Cycle FetchPCLow(ushort addr)
        {
            return cpu => {
                cpu.PC &= 0xFF00;
                cpu.PC |= cpu.Read(addr);
                cpu.P.I = 1; // Set I flag
            };
        }

        private static Cycle FetchPCHigh(ushort addr)
        {
            return cpu => {
                cpu.PC &= 0x00FF;
                cpu.PC |= (ushort) (cpu.Read(addr) << 8);
            };
        }

        public class OpcodeException : Exception {
            public OpcodeException(string msg) : base(msg) {}
        }

        private static Cycle Jam = cpu => {
            byte opcode = cpu.val;
            throw new OpcodeException(string.Format("Unknown opcode {0:X2}", opcode));
        };
    }
}
