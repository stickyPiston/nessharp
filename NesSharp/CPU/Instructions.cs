using System;

namespace NesSharp
{

    using Cycle = Func<CPU, bool>;

    public partial class CPU
    {
        // Micro-instructions
        private static Cycle ReadPC = cpu => {
        #if DEBUG
            cpu._read = true;
            cpu._addr = cpu.PC;
            cpu._data = cpu.bus.Read(cpu.PC);
        #else
            cpu.bus.Read(cpu.PC);
        #endif
            return true;
        };

        private static Cycle FetchPC = cpu => {
        #if DEBUG
            cpu._read = true;
            cpu._addr = cpu.PC;
            cpu._data = cpu.bus.Read(cpu.PC);
            cpu.val = cpu._data;
        #else
            cpu.val = cpu.bus.Read(cpu.PC);
        #endif
            unchecked { cpu.PC += 1; }
            return true;
        };

        private static Cycle ReadStackDec = cpu => {
        #if DEBUG
            cpu._read = true;
            cpu._addr = (ushort) (0x100 | cpu.S);
            cpu._data = cpu.bus.Read(cpu.addr);
        #else
            cpu.bus.Read((ushort) (0x100 | cpu.S));
        #endif
            unchecked { cpu.S -= 1; }
            return true;
        };

        private static Cycle PushPCH = cpu => {
        #if DEBUG
            cpu._read = false;
            cpu._addr = (ushort) (0x100 | cpu.S);
            cpu._data = (byte) (cpu.PC >> 8);
        #endif
            cpu.bus.Write((ushort) (0x100 | cpu.S), (byte) (cpu.PC >> 8));
            unchecked { cpu.S -= 1; }
            return true;
        };

        private static Cycle PushPCL = cpu => {
        #if DEBUG
            cpu._read = false;
            cpu._addr = (ushort) (0x100 | cpu.S);
            cpu._data = (byte) (cpu.PC & 0xFF);
        #endif
            cpu.bus.Write((ushort) (0x100 | cpu.S), (byte) (cpu.PC & 0xFF));
            unchecked { cpu.S -= 1; }
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
                cpu._read = false;
                cpu._addr = (ushort) (0x100 | cpu.S);
                cpu._data = cpu.P;
            #endif

                cpu.bus.Write((ushort) (0x100 | cpu.S), cpu.P);
                unchecked { cpu.S -= 1; }
                return true;
            };
            else return cpu => {
                // Interrupt hijack
                if (cpu.pending == HardwareInterrupt.NMI) cpu.instr = NMIInstruction;

                cpu.P &= 255 - 16; // Unset B flag

            #if DEBUG
                cpu._instr = cpu.instr.Value.Name;
                cpu._read = false;
                cpu._addr = (ushort) (0x100 | cpu.S);
                cpu._data = cpu.P;
            #endif

                cpu.bus.Write((ushort) (0x100 | cpu.S), cpu.P);
                unchecked { cpu.S -= 1; }
                return true;
            };
        }

        private static Cycle FetchPCLow(ushort addr)
        {
            return cpu => {
            #if DEBUG
                cpu._addr = addr;
                cpu._data = cpu.bus.Read(addr);
                cpu.PC |= cpu._data;
            #else
                cpu.PC |= cpu.bus.Read(addr);
            #endif

                cpu.P  |= 4; // Set I flag
                return true;
            };
        }

        private static Cycle FetchPCHigh(ushort addr)
        {
            return cpu => {
            #if DEBUG
                cpu._addr = addr;
                cpu._data = cpu.bus.Read(addr);
                cpu.PC |= (ushort) (cpu._data << 8);
            #else
                cpu.PC |= (ushort) (cpu.bus.Read(addr) << 8);
            #endif
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
            ReadPC,             // dummy read
            ReadPC,             // dummy read
            PushPCH,            // push PC to stack 
            PushPCL,
            PushP(false),       // push P to stack with B = false
            FetchPCLow(0xFFFE), // fetch PC, set I flag
            FetchPCHigh(0xFFFF),
        });
        private static Instruction NMIInstruction = new Instruction("NMI", new Cycle[] {
            ReadPC,             // dummy read
            ReadPC,             // dummy read
            PushPCH,            // push PC to stack 
            PushPCL,
            PushP(false),       // push P to stack with B = false
            FetchPCLow(0xFFFA), // fetch PC, set I flag
            FetchPCHigh(0xFFFB),
        });
        private static Instruction ResetInstruction = new Instruction("RESET", new Cycle[] {
            ReadPC,       ReadPC,       ReadPC,       // dummy reads (I'm not sure why there are 3)
            ReadStackDec, ReadStackDec, ReadStackDec, // decrement stack 3 times
            FetchPCLow(0xFFFC),                       // fetch PC, set I flag
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
