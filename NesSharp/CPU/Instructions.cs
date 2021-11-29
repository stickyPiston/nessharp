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
            WriteAddr(cpu, (ushort) (0x100 | cpu.S), (byte) (cpu.PC & 0xFF));
            unchecked { cpu.S -= 1; }
            return true;
        };

        private static Cycle JumpPC = cpu => {
            // fetch high address
            cpu.PC = (ushort) (ReadAddr(cpu, cpu.PC) << 8);

            // copy low address byte
            cpu.PC |= cpu.val;
            return true;
        };

        private static Cycle LoadXPC = cpu => {
            cpu.X = ReadAddr(cpu, cpu.PC);
            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle StoreXzpg = cpu => {
            WriteAddr(cpu, cpu.val, cpu.X);
            return true;
        };

        private static Cycle NOP = cpu => {
            return true;
        };

        private static Cycle PushP(bool B)
        {
            if (B) return cpu => {
                // Interrupt hijack
                if (cpu.pending == HardwareInterrupt.NMI) cpu.instr = NMIInstruction;
                if (cpu.pending == HardwareInterrupt.IRQ) cpu.instr = IRQInstruction;

                cpu.P |= 16; // Set B flag

            #if DEBUG
                cpu._instr = cpu.instr.Value.Name;
            #endif

                WriteAddr(cpu, (ushort) (0x100 | cpu.S), cpu.P);
                unchecked { cpu.S -= 1; }
                return true;
            };
            else return cpu => {
                // Interrupt hijack
                if (cpu.pending == HardwareInterrupt.NMI) cpu.instr = NMIInstruction;

                cpu.P &= 255 - 16; // Unset B flag

            #if DEBUG
                cpu._instr = cpu.instr.Value.Name;
            #endif

                WriteAddr(cpu, (ushort) (0x100 | cpu.S), cpu.P);
                unchecked { cpu.S -= 1; }
                return true;
            };
        }

        private static Cycle FetchPCLow(ushort addr)
        {
            return cpu => {
                cpu.PC = ReadAddr(cpu, addr);
                cpu.P  |= 4; // Set I flag
                return true;
            };
        }

        private static Cycle FetchPCHigh(ushort addr)
        {
            return cpu => {
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
            new Instruction("BRK", new Cycle[] { FetchPC, FetchPC, PushPCH, PushPCL, PushP(true), FetchPCLow(0xFFFE), FetchPCHigh(0xFFFF) }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JSR abs", new Cycle[] { FetchPC, FetchPC, DummyPeekStack, PushPCH, PushPCL, JumpPC }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JMP abs", new Cycle[] { FetchPC, FetchPC, JumpPC }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("STX zpg", new Cycle[] { FetchPC, FetchPC, StoreXzpg }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("LDX #", new Cycle[] { FetchPC, LoadXPC }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("NOP impl", new Cycle[] { FetchPC, DummyFetchPC }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
            new Instruction("JAM", new Cycle[] { FetchPC, Jam }),
        };
    }
}
