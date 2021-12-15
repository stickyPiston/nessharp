using System;

namespace NesSharp
{

    using Cycle = Action<CPU>;

    public partial class CPU
    {
        // Micro-micro-instructions

        /// <summary>Checks if an interrupt is pending.
        /// If so, it switches execution to said interrupt without changing the cycle number and returns true.
        /// Otherwise it does nothing and returns false.</summary>
        private bool CheckPending(bool irq = true)
        {
            if (prevpolled != null && (irq || prevpolled == HardwareInterrupt.NMI))
            {
                // Poll next interrupt
                SetInstruction(prevpolled == HardwareInterrupt.NMI ? NMIInstruction : IRQInstruction);
                return true;
            }
            return false;
        }

        /// <summary>Checks if an interrupt is pending.
        /// If so, it switches execution to said interrupt and executes the first cycle.
        /// Otherwise it reads the PC and switches to the next instruction at cycle 1.</summary>
        private void NextInstruction(bool checkPending = true)
        {
            if (checkPending && CheckPending())
            {
                cycle = 0;
                instr.Cycles[0](this);
            }
            else
            {
                cycle = 0;
                byte next = Read(PC);
                unchecked { PC += 1; }
                SetInstruction(instructions[next]);
            }
        }

        /// <summary>Reads a value from the bus at the specified adress.</summary>
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

        /// <summary>Writes a value to the bus at the specified adress.</summary>
        private void Write(ushort addr, byte b)
        {
        #if DEBUG
            this._read = false;
            this._addr = addr;
            this._data = b;
        #endif
            this.bus.Write(addr, b);
        }

        /// <summary>Sets the N and Z flags based on a value.</summary>
        private void SetFlags(byte b)
        {
            this.P.N = (byte) (b >> 7);
            this.P.Z = Flags.Zero(b);
        }

        /// <summary>Adds two numbers a and b, and a flag c, returning the value. The V and C flags are set based on the performed operation.</summary>
        private byte Add(byte a, byte b, byte c)
        {
            ushort res = (ushort) ((ushort) a + b + c);
            this.P.V = (byte) (((~(a ^ b)) & (a ^ (byte) res)) >> 7);
            this.P.C = (byte) (res >> 8);
            return (byte) res;
        }

        /// <summary>Branches to a new spot if the bool b is true. Otherwise it fetches the next instruction.</summary>
        private void Branch(bool b)
        {
            byte operand = this.val;

            if (b) {
                // Branch
                Read(this.PC);

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
                NextInstruction();
            }
        }

        // Micro-instructions

        /// <summary>Fixes the high byte of the PC after a branch, as it can sometimes be off by 0x0100.</summary>
        private static Cycle FixPC = cpu => {
            byte PCH = cpu.val;

            if ((byte) (cpu.PC >> 8) != PCH) {
                // Different page
                cpu.Read(cpu.PC);

                cpu.PC &= 0x00FF;
                cpu.PC |= (ushort) (PCH << 8);
            } else {
                // Next instruction
                // "a taken non-page-crossing branch ignores IRQ/NMI during its last clock, so that next instruction executes before the IRQ"
                cpu.NextInstruction(cpu.prevprevpolled != null);
            }

        };

        /// <summary>Reads at the PC and throws the value away.</summary>
        private static Cycle DummyReadAtPC = cpu => cpu.Read(cpu.PC);

        /// <summary>Reads at the SP and throws the value away.</summary>
        private static Cycle DummyReadAtSP = cpu => cpu.Read((ushort) (0x100 | cpu.S));

        /// <summary>Fetches the value from the PC.</summary>
        private static Cycle ValFromPC = cpu => {
            cpu.val = cpu.Read(cpu.PC);
            unchecked { cpu.PC += 1; }
        };

        /// <summary>Fetches the value from the address.</summary>
        private static Cycle ValFromAddr = cpu => {
            cpu.val = cpu.Read(cpu.addr);
        };

        /// <summary>Reads at the PC, throws the value away and sets the value to the A register.</summary>
        private static Cycle ValFromAcc = cpu => {
            cpu.Read(cpu.PC);
            cpu.val = cpu.A;
        };

        /// <summary>Reads at the value pointer, throws the value away and adds X to the value.</summary>
        private static Cycle ValAddX = cpu => {
            cpu.Read(cpu.val);
            unchecked { cpu.val += cpu.X; }
        };

        /// <summary>Reads at the address, throws the value away and adds X to the low byte of the address.</summary>
        private static Cycle LowAddX = cpu => {
            cpu.Read(cpu.addr);
            unchecked { cpu.addr += cpu.X; }
            cpu.addr &= 0x00FF;
        };

        /// <summary>Reads at the address, throws the value away and adds Y to the low byte of the address.</summary>
        private static Cycle LowAddY = cpu => {
            cpu.Read(cpu.addr);
            unchecked { cpu.addr += cpu.Y; }
            cpu.addr &= 0x00FF;
        };

        /// <summary>Fetches the low address from the value pointer.</summary>
        private static Cycle LowFromVal = cpu => {
            cpu.addr = cpu.Read(cpu.val);
        };

        /// <summary>Fetches the high address from the value pointer.</summary>
        private static Cycle HighFromVal = cpu => {
            unchecked { cpu.addr |= (ushort) (cpu.Read((byte) (cpu.val + 1)) << 8); }
        };

        /// <summary>Fetches the high address from value pointer, and adds Y to the low byte (which later will have to be fixed).</summary>
        private static Cycle HighFromValAddY = cpu => {
            unchecked { cpu.addr |= (ushort) (cpu.Read((byte) (cpu.val + 1)) << 8); }

            // Save addr H for later
            unchecked {
                ushort branch = (ushort) (cpu.addr + cpu.Y);
                cpu.val = (byte) (branch >> 8);
            }

            // Set addr L
            byte low = (byte) cpu.addr;
            unchecked { low += cpu.Y; }
            cpu.addr &= 0xFF00;
            cpu.addr |= low;
        };

        /// <summary>Fetches the high address from the PC, and adds Y to the low byte (which later will have to be fixed).</summary>
        private static Cycle HighFromPCAddY = cpu => {
            cpu.addr |= (ushort) (cpu.Read(cpu.PC) << 8);
            unchecked { cpu.PC += 1; }

            // Save addr H for later
            unchecked {
                ushort branch = (ushort) (cpu.addr + cpu.Y);
                cpu.val = (byte) (branch >> 8);
            }

            // Set addr L
            byte low = (byte) cpu.addr;
            unchecked { low += cpu.Y; }
            cpu.addr &= 0xFF00;
            cpu.addr |= low;
        };

        /// <summary>Fetches the high address from the PC, and adds X to the low byte (which later will have to be fixed).</summary>
        private static Cycle HighFromPCAddX = cpu => {
            cpu.addr |= (ushort) (cpu.Read(cpu.PC) << 8);
            unchecked { cpu.PC += 1; }

            // Save addr H for later
            unchecked {
                ushort branch = (ushort) (cpu.addr + cpu.X);
                cpu.val = (byte) (branch >> 8);
            }

            // Set addr L
            byte low = (byte) cpu.addr;
            unchecked { low += cpu.X; }
            cpu.addr &= 0xFF00;
            cpu.addr |= low;
        };

        /// <summary>Fixes the high byte of the address after an indexed read, as it can sometimes be off by 0x0100.</summary>
        private static Cycle FixAddr = cpu => {
            byte high = cpu.val;
            cpu.val = cpu.Read(cpu.addr);

            if ((byte) (cpu.addr >> 8) != high)
            {
                // Page cross!
                cpu.addr &= 0x00FF;
                cpu.addr |= (ushort) (high << 8);
            }
            else if (cpu.instr.ReadsAddr && !cpu.instr.WritesAddr)
            {
                // No page cross!
                cpu.cycle += 1; // Skip another read
            }
        };

        /// <summary>Writes the value to the adress.</summary>
        private static Cycle WriteVal = cpu => cpu.Write(cpu.addr, cpu.val);

        /// <summary>Fetches the low address from the PC.</summary>
        private static Cycle LowFromPC = cpu => {
            cpu.addr = cpu.Read(cpu.PC);
            unchecked { cpu.PC += 1; }
        };

        /// <summary>Fetches the high address from the PC.</summary>
        private static Cycle HighFromPC = cpu => {
            cpu.addr |= (ushort) (cpu.Read(cpu.PC) << 8);
            unchecked { cpu.PC += 1; }
        };

        /// <summary>Reads the stack and decrements the stack pointer.</summary>
        private static Cycle DummyPushStack = cpu => {
            cpu.Read((ushort) (0x100 | cpu.S));
            unchecked { cpu.S -= 1; }
        };

        /// <summary>Pushes the high byte of the PC to the stack.</summary>
        private static Cycle PushPCH = cpu => {
            cpu.Write((ushort) (0x100 | cpu.S), (byte) (cpu.PC >> 8));
            unchecked { cpu.S -= 1; }
        };

        /// <summary>Pushes the low byte of the PC to the stack.</summary>
        private static Cycle PushPCL = cpu => {
            cpu.Write((ushort) (0x100 | cpu.S), (byte) cpu.PC);
            unchecked { cpu.S -= 1; }
        };

        /// <summary>Pushes the A register to the stack.</summary>
        private static Cycle PushA = cpu => {
            cpu.Write((ushort) (0x100 | cpu.S), cpu.A);
            unchecked { cpu.S -= 1; }
        };

        /// <summary>Increments the stack pointer.</summary>
        private static Cycle IncSP = cpu => {
            cpu.Read((ushort) (0x100 | cpu.S));
            unchecked { cpu.S += 1; }
        };

        /// <summary>Pulls the low byte of the PC from the stack.</summary>
        private static Cycle PullPCL = cpu => {
            cpu.PC &= 0xFF00;
            cpu.PC |= cpu.Read((ushort) (0x100 | cpu.S));
            unchecked { cpu.S += 1; }
        };

        /// <summary>Pulls the high byte of the PC from the stack.</summary>
        private static Cycle PullPCH = cpu => {
            cpu.PC &= 0x00FF;
            cpu.PC |= (ushort) (cpu.Read((ushort) (0x100 | cpu.S)) << 8);
        };

        private static Cycle PullA = cpu => {
            cpu.A = cpu.Read((ushort) (0x100 | cpu.S));
            cpu.SetFlags(cpu.A);
        };

        /// <summary>Pulls the P register from the stack.</summary>
        private static Cycle PullP(bool inc)
        {
            if (inc)
            {
                return cpu => {
                    cpu.P.Read(cpu.Read((ushort) (0x100 | cpu.S)));
                    unchecked { cpu.S += 1; }
                };
            }
            else
            {
                return cpu => {
                    cpu.P.Read(cpu.Read((ushort) (0x100 | cpu.S)));
                };
            }
        }

        /// <summary>Pushes P register to the stack.</summary>
        private static Cycle PushP(bool B, bool hijack = true)
        {
            if (B) return cpu => {
                // Interrupt hijack (all hardware interrupts)
                if (hijack) {
                    cpu.CheckPending(true);
                    if (cpu.polled == HardwareInterrupt.NMI) {
                        // Reset NMI on handle
                        cpu.polled = null;
                    }
                }

                cpu.P.B = 1; // Set B flag

                cpu.Write((ushort) (0x100 | cpu.S), cpu.P.Write());
                unchecked { cpu.S -= 1; }
            };
            else return cpu => {
                // Interrupt hijack (only NMI)
                if (hijack) {
                    cpu.CheckPending(false);
                    if (cpu.polled == HardwareInterrupt.NMI) {
                        // Reset NMI on handle
                        cpu.polled = null;
                    }
                }

                cpu.P.B = 0; // Unset B flag

                cpu.Write((ushort) (0x100 | cpu.S), cpu.P.Write());
                unchecked { cpu.S -= 1; }
            };
        }

        /// <summary>Fetches the low byte of the PC from a static address.</summary>
        private static Cycle PCLowFromAddr(ushort addr)
        {
            return cpu => {
                cpu.PC &= 0xFF00;
                cpu.PC |= cpu.Read(addr);
                cpu.P.I = 1; // Set I flag
            };
        }

        /// <summary>Fetches the high byte of the PC from a static address.</summary>
        private static Cycle PCHighFromAddr(ushort addr)
        {
            return cpu => {
                cpu.PC &= 0x00FF;
                cpu.PC |= (ushort) (cpu.Read(addr) << 8);
            };
        }

        // Cycles for specific instructions

        private static Cycle AND = cpu => {
            cpu.A &= cpu.val;
            cpu.SetFlags(cpu.A);
            cpu.NextInstruction();
        };

        private static Cycle ORA = cpu => {
            cpu.A |= cpu.val;
            cpu.SetFlags(cpu.A);
            cpu.NextInstruction();
        };

        private static Cycle EOR = cpu => {
            cpu.A ^= cpu.val;
            cpu.SetFlags(cpu.A);
            cpu.NextInstruction();
        };

        private static Cycle ADC = cpu => {
            cpu.A = cpu.Add(cpu.A, cpu.val, cpu.P.C);
            cpu.SetFlags(cpu.A);
            cpu.NextInstruction();
        };

        private static Cycle SBC = cpu => {
            cpu.A = cpu.Add(cpu.A, (byte) ~cpu.val, cpu.P.C);
            cpu.SetFlags(cpu.A);
            cpu.NextInstruction();
        };

        private static Cycle CMP = cpu => {
            ushort q = (ushort) (((ushort) cpu.A | 0x0100) - cpu.val);

            cpu.P.C = (byte) (q >> 8);
            cpu.SetFlags((byte) q);
            cpu.NextInstruction();
        };

        private static Cycle CPX = cpu => {
            ushort q = (ushort) (((ushort) cpu.X | 0x0100) - cpu.val);

            cpu.P.C = (byte) (q >> 8);
            cpu.SetFlags((byte) q);
            cpu.NextInstruction();
        };

        private static Cycle CPY = cpu => {
            ushort q = (ushort) (((ushort) cpu.Y | 0x0100) - cpu.val);

            cpu.P.C = (byte) (q >> 8);
            cpu.SetFlags((byte) q);
            cpu.NextInstruction();
        };

        private static Cycle INX = cpu => {
            unchecked { cpu.X += 1; }
            cpu.SetFlags(cpu.X);
            cpu.NextInstruction();
        };

        private static Cycle INY = cpu => {
            unchecked { cpu.Y += 1; }
            cpu.SetFlags(cpu.Y);
            cpu.NextInstruction();
        };

        private static Cycle DEX = cpu => {
            unchecked { cpu.X -= 1; }
            cpu.SetFlags(cpu.X);
            cpu.NextInstruction();
        };

        private static Cycle DEY = cpu => {
            unchecked { cpu.Y -= 1; }
            cpu.SetFlags(cpu.Y);
            cpu.NextInstruction();
        };

        private static Cycle TAX = cpu => {
            cpu.X = cpu.A;
            cpu.SetFlags(cpu.X);
            cpu.NextInstruction();
        };

        private static Cycle TAY = cpu => {
            cpu.Y = cpu.A;
            cpu.SetFlags(cpu.Y);
            cpu.NextInstruction();
        };

        private static Cycle TXA = cpu => {
            cpu.A = cpu.X;
            cpu.SetFlags(cpu.A);
            cpu.NextInstruction();
        };

        private static Cycle TYA = cpu => {
            cpu.A = cpu.Y;
            cpu.SetFlags(cpu.A);
            cpu.NextInstruction();
        };

        private static Cycle TSX = cpu => {
            cpu.X = cpu.S;
            cpu.SetFlags(cpu.X);
            cpu.NextInstruction();
        };

        private static Cycle TXS = cpu => {
            cpu.S = cpu.X;
            // no flags are set!
            cpu.NextInstruction();
        };

        private static Cycle LSR = cpu => {
            byte operand = cpu.val;
            cpu.val = (byte) (operand >> 1);
            cpu.P.C = (byte) (operand & 1);
            cpu.SetFlags(cpu.val);

            if (cpu.instr.Mode == AddressingMode.ACC) {
                cpu.A = cpu.val;
                cpu.NextInstruction();
            } else {
                cpu.Write(cpu.addr, operand);
            }
        };

        private static Cycle ROR = cpu => {
            byte operand = cpu.val;
            cpu.val = (byte) (operand >> 1 | cpu.P.C << 7);
            cpu.P.C = (byte) (operand & 1);
            cpu.SetFlags(cpu.val);

            if (cpu.instr.Mode == AddressingMode.ACC) {
                cpu.A = cpu.val;
                cpu.NextInstruction();
            } else {
                cpu.Write(cpu.addr, operand);
            }
        };

        private static Cycle ASL = cpu => {
            byte operand = cpu.val;
            cpu.val = (byte) (operand << 1);
            cpu.P.C = (byte) (operand >> 7);
            cpu.SetFlags(cpu.val);

            if (cpu.instr.Mode == AddressingMode.ACC) {
                cpu.A = cpu.val;
                cpu.NextInstruction();
            } else {
                cpu.Write(cpu.addr, operand);
            }
        };

        private static Cycle ROL = cpu => {
            byte operand = cpu.val;
            cpu.val = (byte) (operand << 1 | cpu.P.C);
            cpu.P.C = (byte) (operand >> 7);
            cpu.SetFlags(cpu.val);

            if (cpu.instr.Mode == AddressingMode.ACC) {
                cpu.A = cpu.val;
                cpu.NextInstruction();
            } else {
                cpu.Write(cpu.addr, operand);
            }
        };

        private static Cycle JumpPC = cpu => {
            // fetch high address
            cpu.PC = (ushort) (cpu.Read(cpu.PC) << 8);

            // copy low address byte
            cpu.PC |= cpu.val;
        };

        private static Cycle JumpAddr = cpu => {
            // fetch high address
            byte addr = (byte) cpu.addr;
            unchecked { addr += 1; }
            ushort effective = (ushort) ((cpu.addr & 0xFF00) | addr);
            cpu.PC = (ushort) (cpu.Read(effective) << 8);

            // copy low address byte
            cpu.PC |= cpu.val;
        };

        private static Cycle LDA = cpu => {
            cpu.A = cpu.val;
            cpu.SetFlags(cpu.A);
            cpu.NextInstruction();
        };

        private static Cycle LAX = cpu => {
            cpu.X = cpu.A = cpu.val;
            cpu.SetFlags(cpu.val);
            cpu.NextInstruction();
        };

        private static Cycle LDX = cpu => {
            cpu.X = cpu.val;
            cpu.SetFlags(cpu.X);
            cpu.NextInstruction();
        };

        private static Cycle LDY = cpu => {
            cpu.Y = cpu.val;
            cpu.SetFlags(cpu.Y);
            cpu.NextInstruction();
        };

        private static Cycle STA = cpu => cpu.Write(cpu.addr, cpu.A);
        private static Cycle STX = cpu => cpu.Write(cpu.addr, cpu.X);
        private static Cycle STY = cpu => cpu.Write(cpu.addr, cpu.Y);
        private static Cycle SAX = cpu => cpu.Write(cpu.addr, (byte) (cpu.A & cpu.X));

        private static Cycle DEC = cpu => {
            cpu.Write(cpu.addr, cpu.val);
            unchecked { cpu.val -= 1; }
            cpu.SetFlags(cpu.val);
        };

        private static Cycle INC = cpu => {
            cpu.Write(cpu.addr, cpu.val);
            unchecked { cpu.val += 1; }
            cpu.SetFlags(cpu.val);
        };

        private static Cycle ISB = cpu => {
            // INC
            cpu.Write(cpu.addr, cpu.val);
            unchecked { cpu.val += 1; }

            // SBC
            cpu.A = cpu.Add(cpu.A, (byte) ~cpu.val, cpu.P.C);
            cpu.SetFlags(cpu.A);
        };

        private static Cycle DCP = cpu => {
            // DEC
            cpu.Write(cpu.addr, cpu.val);
            unchecked { cpu.val -= 1; }

            // CMP
            ushort q = (ushort) (((ushort) cpu.A | 0x0100) - cpu.val);

            cpu.P.C = (byte) (q >> 8);
            cpu.SetFlags((byte) q);
        };

        private static Cycle SLO = cpu => {
            // ASL
            byte operand = cpu.val;
            cpu.val = (byte) (operand << 1);
            cpu.P.C = (byte) (operand >> 7);
            cpu.Write(cpu.addr, operand);

            // ORA
            cpu.A |= cpu.val;
            cpu.SetFlags(cpu.A);
        };

        private static Cycle RLA = cpu => {
            // ROL
            byte operand = cpu.val;
            cpu.val = (byte) (operand << 1 | cpu.P.C);
            cpu.P.C = (byte) (operand >> 7);
            cpu.Write(cpu.addr, operand);

            // AND
            cpu.A &= cpu.val;
            cpu.SetFlags(cpu.A);
        };

        private static Cycle SRE = cpu => {
            // LSR
            byte operand = cpu.val;
            cpu.val = (byte) (operand >> 1);
            cpu.P.C = (byte) (operand & 1);
            cpu.Write(cpu.addr, operand);

            // EOR
            cpu.A ^= cpu.val;
            cpu.SetFlags(cpu.A);
        };

        private static Cycle RRA = cpu => {
            // ROR
            byte operand = cpu.val;
            cpu.val = (byte) (operand >> 1 | cpu.P.C << 7);
            cpu.P.C = (byte) (operand & 1);
            cpu.Write(cpu.addr, operand);

            // ADC
            cpu.A = cpu.Add(cpu.A, cpu.val, cpu.P.C);
            cpu.SetFlags(cpu.A);
        };

        private static Cycle BIT = cpu => {
            cpu.P.N = (byte) (cpu.val >> 7);
            cpu.P.V = (byte) (cpu.val >> 6 & 1);
            cpu.P.Z = Flags.Zero((byte) (cpu.val & cpu.A));
            cpu.NextInstruction();
        };

        private static Cycle SEC = cpu => {
            cpu.P.C = 1;
            cpu.NextInstruction();
        };

        private static Cycle CLC = cpu => {
            cpu.P.C = 0;
            cpu.NextInstruction();
        };

        private static Cycle SEI = cpu => {
            cpu.P.I = 1;
            cpu.NextInstruction();
        };

        private static Cycle CLI = cpu => {
            cpu.P.I = 0;
            cpu.NextInstruction();
        };

        private static Cycle SED = cpu => {
            cpu.P.D = 1;
            cpu.NextInstruction();
        };

        private static Cycle CLD = cpu => {
            cpu.P.D = 0;
            cpu.NextInstruction();
        };

        private static Cycle CLV = cpu => {
            cpu.P.V = 0;
            cpu.NextInstruction();
        };

        private static Cycle NOP = cpu => {
            cpu.NextInstruction();
        };

        private static Cycle BCS = cpu => cpu.Branch(cpu.P.C == 1);
        private static Cycle BCC = cpu => cpu.Branch(cpu.P.C == 0);
        private static Cycle BEQ = cpu => cpu.Branch(cpu.P.Z == 1);
        private static Cycle BNE = cpu => cpu.Branch(cpu.P.Z == 0);
        private static Cycle BVS = cpu => cpu.Branch(cpu.P.V == 1);
        private static Cycle BVC = cpu => cpu.Branch(cpu.P.V == 0);
        private static Cycle BMI = cpu => cpu.Branch(cpu.P.N == 1);
        private static Cycle BPL = cpu => cpu.Branch(cpu.P.N == 0);

        public class OpcodeException : Exception {
            public OpcodeException(string msg) : base(msg) {}
        }

        private static Cycle Jam = cpu => {
            byte opcode = cpu.val;
            throw new OpcodeException(string.Format("Unknown opcode {0:X2}", opcode));
        };
    }
}
