using System;

namespace NesSharp
{

    using Cycle = Func<CPU, bool>;

    public partial class CPU
    {
        // Micro-micro-instructions
        private static byte ReadAddr(CPU cpu, ushort addr) {
        #if DEBUG
            cpu._read = true;
            cpu._addr = addr;
            cpu._data = cpu.bus.Read(addr);
            return cpu._data;
        #else
            return cpu.bus.Read(addr);
        #endif
        }

        private static void WriteAddr(CPU cpu, ushort addr, byte b) {
        #if DEBUG
            cpu._read = false;
            cpu._addr = addr;
            cpu._data = b;
        #endif
            cpu.bus.Write(addr, b);
        }

        private static void Branch(CPU cpu, bool b) {
            byte operand = cpu.val;
            byte next = ReadAddr(cpu, cpu.PC);

            if (b) {
                // Branch
                // Save PCH for later
                unchecked {
                    sbyte a = (sbyte) operand;
                    ushort branch = (ushort) (cpu.PC + a);
                    cpu.val = (byte) (branch >> 8);
                }

                // Set PCL
                byte PCL = (byte) cpu.PC;
                unchecked { PCL += operand; }
                cpu.PC &= 0xFF00;
                cpu.PC |= PCL;
            } else {
                // Next instruction
                unchecked { cpu.PC += 1; }
                cpu.instr = instructions[next];
                cpu.cycle = 0;
            #if DEBUG
                cpu._instr = cpu.instr.Value.Name;
            #endif
            }
        }

        // Micro-instructions
        private static Cycle DummyFetchPC = cpu => {
            ReadAddr(cpu, cpu.PC);
            return true;
        };

        private static Cycle FetchPC = cpu => {
            cpu.val = ReadAddr(cpu, cpu.PC);
            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle DummyPeekStack = cpu => {
            ReadAddr(cpu, (ushort) (0x100 | cpu.S));
            return true;
        };

        private static Cycle DummyPushStack = cpu => {
            ReadAddr(cpu, (ushort) (0x100 | cpu.S));
            unchecked { cpu.S -= 1; }
            return true;
        };

        private static Cycle PushPCH = cpu => {
            WriteAddr(cpu, (ushort) (0x100 | cpu.S), (byte) (cpu.PC >> 8));
            unchecked { cpu.S -= 1; }
            return true;
        };

        private static Cycle PushPCL = cpu => {
            WriteAddr(cpu, (ushort) (0x100 | cpu.S), (byte) cpu.PC);
            unchecked { cpu.S -= 1; }
            return true;
        };

        private static Cycle PushA = cpu => {
            WriteAddr(cpu, (ushort) (0x100 | cpu.S), cpu.A);
            unchecked { cpu.S -= 1; }
            return true;
        };

        private static Cycle IncSP = cpu => {
            ReadAddr(cpu, (ushort) (0x100 | cpu.S));
            unchecked { cpu.S += 1; }
            return true;
        };

        private static Cycle PullPCL = cpu => {
            cpu.PC &= 0xFF00;
            cpu.PC |= ReadAddr(cpu, (ushort) (0x100 | cpu.S));
            unchecked { cpu.S += 1; }
            return true;
        };

        private static Cycle PullPCH = cpu => {
            cpu.PC &= 0x00FF;
            cpu.PC |= (ushort) (ReadAddr(cpu, (ushort) (0x100 | cpu.S)) << 8);
            return true;
        };

        private static Cycle PullA = cpu => {
            cpu.A = ReadAddr(cpu, (ushort) (0x100 | cpu.S));
            cpu.P.N = (byte) (cpu.A >> 7);
            cpu.P.Z = Flags.Zero(cpu.A);
            return true;
        };

        private static Cycle PullP = cpu => {
            cpu.P.Read(ReadAddr(cpu, (ushort) (0x100 | cpu.S)));
            return true;
        };

        private static Cycle ANDimm = cpu => {
            cpu.A &= ReadAddr(cpu, cpu.PC);
            cpu.P.N = (byte) (cpu.A >> 7);
            cpu.P.Z = Flags.Zero(cpu.A);
            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle ORAimm = cpu => {
            cpu.A |= ReadAddr(cpu, cpu.PC);
            cpu.P.N = (byte) (cpu.A >> 7);
            cpu.P.Z = Flags.Zero(cpu.A);
            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle EORimm = cpu => {
            cpu.A ^= ReadAddr(cpu, cpu.PC);
            cpu.P.N = (byte) (cpu.A >> 7);
            cpu.P.Z = Flags.Zero(cpu.A);
            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle ADCimm = cpu => {
            byte add = ReadAddr(cpu, cpu.PC);

            // Get 9-bit result
            ushort res = (ushort) ((ushort) cpu.A + add + cpu.P.C);
            cpu.P.V = (byte) (((~(cpu.A ^ add)) & (cpu.A ^ (byte) res)) >> 7);
            cpu.P.C = (byte) (res >> 8);

            // Store in accumulator
            cpu.A = (byte) res;
            cpu.P.N = (byte) (cpu.A >> 7);
            cpu.P.Z = Flags.Zero(cpu.A);

            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle SBCimm = cpu => {
            byte add = (byte) (ReadAddr(cpu, cpu.PC) ^ 0xFF);

            // Get 9-bit result
            ushort res = (ushort) ((ushort) cpu.A + add + cpu.P.C);
            cpu.P.V = (byte) (((~(cpu.A ^ add)) & (cpu.A ^ (byte) res)) >> 7);
            cpu.P.C = (byte) (res >> 8);

            // Store in accumulator
            cpu.A = (byte) res;
            cpu.P.N = (byte) (cpu.A >> 7);
            cpu.P.Z = Flags.Zero(cpu.A);

            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle CMPimm = cpu => {
            ushort q = (ushort) (((ushort) cpu.A | 0x0100) - ReadAddr(cpu, cpu.PC));
            cpu.P.N = (byte) (q >> 7 & 1);
            cpu.P.Z = Flags.Zero((byte) q);
            cpu.P.C = (byte) (q >> 8);

            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle CPXimm = cpu => {
            ushort q = (ushort) (((ushort) cpu.X | 0x0100) - ReadAddr(cpu, cpu.PC));
            cpu.P.N = (byte) (q >> 7 & 1);
            cpu.P.Z = Flags.Zero((byte) q);
            cpu.P.C = (byte) (q >> 8);

            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle CPYimm = cpu => {
            ushort q = (ushort) (((ushort) cpu.Y | 0x0100) - ReadAddr(cpu, cpu.PC));
            cpu.P.N = (byte) (q >> 7 & 1);
            cpu.P.Z = Flags.Zero((byte) q);
            cpu.P.C = (byte) (q >> 8);

            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle JumpPC = cpu => {
            // fetch high address
            cpu.PC = (ushort) (ReadAddr(cpu, cpu.PC) << 8);

            // copy low address byte
            cpu.PC |= cpu.val;
            return true;
        };

        private static Cycle LDXPC = cpu => {
            cpu.X = ReadAddr(cpu, cpu.PC);
            cpu.P.N = (byte) (cpu.X >> 7);
            cpu.P.Z = Flags.Zero(cpu.X);

            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle LDYPC = cpu => {
            cpu.Y = ReadAddr(cpu, cpu.PC);
            cpu.P.N = (byte) (cpu.Y >> 7);
            cpu.P.Z = Flags.Zero(cpu.Y);

            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle LDAPC = cpu => {
            cpu.A = ReadAddr(cpu, cpu.PC);
            cpu.P.N = (byte) (cpu.A >> 7);
            cpu.P.Z = Flags.Zero(cpu.A);

            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle STXzpg = cpu => {
            WriteAddr(cpu, cpu.val, cpu.X);
            return true;
        };

        private static Cycle STAzpg = cpu => {
            WriteAddr(cpu, cpu.val, cpu.A);
            return true;
        };

        private static Cycle BITzpg = cpu => {
            byte test = ReadAddr(cpu, cpu.val);
            cpu.P.N = (byte) (test >> 7);
            cpu.P.V = (byte) (test >> 6 & 1);
            cpu.P.Z = Flags.Zero((byte) (test & cpu.A));
            return true;
        };

        private static Cycle SEC = cpu => {
            cpu.P.C = 1;
            ReadAddr(cpu, cpu.PC);
            return true;
        };

        private static Cycle CLC = cpu => {
            cpu.P.C = 0;
            ReadAddr(cpu, cpu.PC);
            return true;
        };

        private static Cycle SEI = cpu => {
            cpu.P.I = 1;
            ReadAddr(cpu, cpu.PC);
            return true;
        };

        private static Cycle SED = cpu => {
            cpu.P.D = 1;
            ReadAddr(cpu, cpu.PC);
            return true;
        };

        private static Cycle CLD = cpu => {
            cpu.P.D = 0;
            ReadAddr(cpu, cpu.PC);
            return true;
        };

        private static Cycle CLV = cpu => {
            cpu.P.V = 0;
            ReadAddr(cpu, cpu.PC);
            return true;
        };

        private static Cycle BCS = cpu => {
            Branch(cpu, cpu.P.C == 1);
            return true;
        };

        private static Cycle BCC = cpu => {
            Branch(cpu, cpu.P.C == 0);
            return true;
        };

        private static Cycle BEQ = cpu => {
            Branch(cpu, cpu.P.Z == 1);
            return true;
        };

        private static Cycle BNE = cpu => {
            Branch(cpu, cpu.P.Z == 0);
            return true;
        };

        private static Cycle BVS = cpu => {
            Branch(cpu, cpu.P.V == 1);
            return true;
        };

        private static Cycle BVC = cpu => {
            Branch(cpu, cpu.P.V == 0);
            return true;
        };

        private static Cycle BPL = cpu => {
            Branch(cpu, cpu.P.N == 0);
            return true;
        };

        private static Cycle BMI = cpu => {
            Branch(cpu, cpu.P.N == 1);
            return true;
        };

        private static Cycle FixPC = cpu => {
            byte PCH = cpu.val;
            byte next = ReadAddr(cpu, cpu.PC);

            if ((byte) (cpu.PC >> 8) != PCH) {
                // Different page
                cpu.PC &= 0x00FF;
                cpu.PC |= (ushort) (PCH << 8);
            } else {
                // Next instruction
                unchecked { cpu.PC += 1; }
                cpu.instr = instructions[next];
                cpu.cycle = 0;
            #if DEBUG
                cpu._instr = cpu.instr.Value.Name;
            #endif
            }

            return true;
        };

        private static Cycle PushP(bool B)
        {
            if (B) return cpu => {
                // Interrupt hijack
                if (cpu.pending == HardwareInterrupt.NMI) cpu.instr = NMIInstruction;
                if (cpu.pending == HardwareInterrupt.IRQ) cpu.instr = IRQInstruction;

                cpu.P.B = 1; // Set B flag

            #if DEBUG
                cpu._instr = cpu.instr.Value.Name;
            #endif

                WriteAddr(cpu, (ushort) (0x100 | cpu.S), cpu.P.Write());
                unchecked { cpu.S -= 1; }
                return true;
            };
            else return cpu => {
                // Interrupt hijack
                if (cpu.pending == HardwareInterrupt.NMI) cpu.instr = NMIInstruction;

                cpu.P.B = 0; // Unset B flag

            #if DEBUG
                cpu._instr = cpu.instr.Value.Name;
            #endif

                WriteAddr(cpu, (ushort) (0x100 | cpu.S), cpu.P.Write());
                unchecked { cpu.S -= 1; }
                return true;
            };
        }

        private static Cycle FetchPCLow(ushort addr)
        {
            return cpu => {
                cpu.PC &= 0xFF00;
                cpu.PC |= ReadAddr(cpu, addr);
                cpu.P.I = 1; // Set I flag
                return true;
            };
        }

        private static Cycle FetchPCHigh(ushort addr)
        {
            return cpu => {
                cpu.PC &= 0x00FF;
                cpu.PC |= (ushort) (ReadAddr(cpu, addr) << 8);
                return true;
            };
        }

        public class OpcodeException : Exception {
            public OpcodeException(string msg) : base(msg) {}
        }

        private static Cycle Jam = cpu => {
            byte opcode = cpu.val;
            throw new OpcodeException(string.Format("Unknown opcode {0:X2}", opcode));
        };

        private static Instruction IRQInstruction = new Instruction("IRQ", new Cycle[] {
            DummyFetchPC,       // dummy read
            DummyFetchPC,       // dummy read
            PushPCH,            // push PC to stack 
            PushPCL,
            PushP(false),       // push P to stack with B = false
            FetchPCLow(0xFFFE), // fetch PC, set I flag
            FetchPCHigh(0xFFFF),
        });
        private static Instruction NMIInstruction = new Instruction("NMI", new Cycle[] {
            DummyFetchPC,       // dummy read
            DummyFetchPC,       // dummy read
            PushPCH,            // push PC to stack 
            PushPCL,
            PushP(false),       // push P to stack with B = false
            FetchPCLow(0xFFFA), // fetch PC, set I flag
            FetchPCHigh(0xFFFB),
        });
        private static Instruction ResetInstruction = new Instruction("RESET", new Cycle[] {
            DummyFetchPC,   DummyFetchPC,   DummyFetchPC,   // dummy reads (I'm not sure why there are 3)
            DummyPushStack, DummyPushStack, DummyPushStack, // decrement stack 3 times
            FetchPCLow(0xFFFC),                             // fetch PC, set I flag
            FetchPCHigh(0xFFFD),
        });
        private static Instruction[] instructions = {
/* 00 */    new Instruction("BRK", new Cycle[] { FetchPC, FetchPC, PushPCH, PushPCL, PushP(true), FetchPCLow(0xFFFE), FetchPCHigh(0xFFFF) }),
/* 01 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 02 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 03 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 04 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 05 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 06 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 07 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 08 */    new Instruction("PHP impl", new Cycle[] { FetchPC, DummyFetchPC, PushP(true) }),
/* 09 */    new Instruction("ORA impl", new Cycle[] { FetchPC, ORAimm }),
/* 0a */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 0b */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 0c */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 0d */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 0e */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 0f */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 10 */    new Instruction("BPL rel", new Cycle[] { FetchPC, FetchPC, BPL, FixPC }),
/* 11 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 12 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 13 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 14 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 15 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 16 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 17 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 18 */    new Instruction("CLC impl", new Cycle[] { FetchPC, CLC }),
/* 19 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 1a */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 1b */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 1c */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 1d */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 1e */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 1f */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 20 */    new Instruction("JSR abs", new Cycle[] { FetchPC, FetchPC, DummyPeekStack, PushPCH, PushPCL, JumpPC }),
/* 21 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 22 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 23 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 24 */    new Instruction("BIT zpg", new Cycle[] { FetchPC, FetchPC, BITzpg }),
/* 25 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 26 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 27 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 28 */    new Instruction("PLP impl", new Cycle[] { FetchPC, DummyFetchPC, IncSP, PullP }),
/* 29 */    new Instruction("AND #", new Cycle[] { FetchPC, ANDimm }),
/* 2a */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 2b */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 2c */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 2d */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 2e */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 2f */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 30 */    new Instruction("BMI rel", new Cycle[] { FetchPC, FetchPC, BMI, FixPC }),
/* 31 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 32 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 33 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 34 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 35 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 36 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 37 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 38 */    new Instruction("SEC impl", new Cycle[] { FetchPC, SEC }),
/* 39 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 3a */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 3b */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 3c */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 3d */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 3e */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 3f */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 40 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 41 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 42 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 43 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 44 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 45 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 46 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 47 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 48 */    new Instruction("PHA impl", new Cycle[] { FetchPC, DummyFetchPC, PushA }),
/* 49 */    new Instruction("EOR #", new Cycle[] { FetchPC, EORimm }),
/* 4a */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 4b */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 4c */    new Instruction("JMP abs", new Cycle[] { FetchPC, FetchPC, JumpPC }),
/* 4d */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 4e */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 4f */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 50 */    new Instruction("BVC rel", new Cycle[] { FetchPC, FetchPC, BVC, FixPC }),
/* 51 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 52 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 53 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 54 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 55 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 56 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 57 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 58 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 59 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 5a */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 5b */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 5c */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 5d */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 5e */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 5f */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 60 */    new Instruction("RTS impl", new Cycle[] { FetchPC, DummyFetchPC, IncSP, PullPCL, PullPCH, FetchPC }),
/* 61 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 62 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 63 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 64 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 65 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 66 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 67 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 68 */    new Instruction("PLA impl", new Cycle[] { FetchPC, DummyFetchPC, IncSP, PullA }),
/* 69 */    new Instruction("ADC #", new Cycle[] { FetchPC, ADCimm }),
/* 6a */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 6b */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 6c */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 6d */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 6e */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 6f */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 70 */    new Instruction("BVS rel", new Cycle[] { FetchPC, FetchPC, BVS, FixPC }),
/* 71 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 72 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 73 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 74 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 75 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 76 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 77 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 78 */    new Instruction("SEI impl", new Cycle[] { FetchPC, SEI }),
/* 79 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 7a */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 7b */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 7c */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 7d */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 7e */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 7f */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 80 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 81 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 82 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 83 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 84 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 85 */    new Instruction("STA zpg", new Cycle[] { FetchPC, FetchPC, STAzpg }),
/* 86 */    new Instruction("STX zpg", new Cycle[] { FetchPC, FetchPC, STXzpg }),
/* 87 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 88 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 89 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 8a */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 8b */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 8c */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 8d */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 8e */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 8f */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 90 */    new Instruction("BCC rel", new Cycle[] { FetchPC, FetchPC, BCC, FixPC }),
/* 91 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 92 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 93 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 94 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 95 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 96 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 97 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 98 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 99 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 9a */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 9b */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 9c */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 9d */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 9e */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* 9f */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* a0 */    new Instruction("LDY #", new Cycle[] { FetchPC, LDYPC }),
/* a1 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* a2 */    new Instruction("LDX #", new Cycle[] { FetchPC, LDXPC }),
/* a3 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* a4 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* a5 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* a6 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* a7 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* a8 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* a9 */    new Instruction("LDA #", new Cycle[] { FetchPC, LDAPC }),
/* aa */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* ab */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* ac */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* ad */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* ae */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* af */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* b0 */    new Instruction("BCS rel", new Cycle[] { FetchPC, FetchPC, BCS, FixPC }),
/* b1 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* b2 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* b3 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* b4 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* b5 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* b6 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* b7 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* b8 */    new Instruction("CLV impl", new Cycle[] { FetchPC, CLV }),
/* b9 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* ba */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* bb */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* bc */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* bd */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* be */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* bf */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* c0 */    new Instruction("CPY #", new Cycle[] { FetchPC, CPYimm }),
/* c1 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* c2 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* c3 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* c4 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* c5 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* c6 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* c7 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* c8 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* c9 */    new Instruction("CMP #", new Cycle[] { FetchPC, CMPimm }),
/* ca */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* cb */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* cc */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* cd */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* ce */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* cf */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* d0 */    new Instruction("BNE rel", new Cycle[] { FetchPC, FetchPC, BNE, FixPC }),
/* d1 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* d2 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* d3 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* d4 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* d5 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* d6 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* d7 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* d8 */    new Instruction("CLD impl", new Cycle[] { FetchPC, CLD }),
/* d9 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* da */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* db */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* dc */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* dd */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* de */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* df */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* e0 */    new Instruction("CPX #", new Cycle[] { FetchPC, CPXimm }),
/* e1 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* e2 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* e3 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* e4 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* e5 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* e6 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* e7 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* e8 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* e9 */    new Instruction("SBC #", new Cycle[] { FetchPC, SBCimm }),
/* ea */    new Instruction("NOP impl", new Cycle[] { FetchPC, DummyFetchPC }),
/* eb */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* ec */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* ed */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* ee */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* ef */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* f0 */    new Instruction("BEQ rel", new Cycle[] { FetchPC, FetchPC, BEQ, FixPC }),
/* f1 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* f2 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* f3 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* f4 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* f5 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* f6 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* f7 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* f8 */    new Instruction("SED impl", new Cycle[] { FetchPC, SED }),
/* f9 */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* fa */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* fb */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* fc */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* fd */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* fe */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
/* ff */    new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
        };
    }
}
