using System;

namespace NesSharp
{

    using Cycle = Action<CPU>;

    public partial class CPU
    {
        // Bus
        private IAddressable bus;
        
        // Registers
        public ushort PC { get; private set; }
        public byte S { get; private set; }
        public byte A { get; private set; }
        public byte X { get; private set; }
        public byte Y { get; private set; }

        public struct Flags
        {
            public byte N, V, B, D, I, Z, C;

            public string Dump() {
                return string.Format("{0}{1}{2}{3}{4}{5}{6}{7}", N, V, 1, B, D, I, Z, C);
            }

            /// <returns>0x01 if the byte is nonzero and 0x00 if the byte is zero</returns>
            public static byte NonZero(byte b) {
                return (byte) (((b | (~b + 1)) >> 7) & 1);
            }

            /// <returns>0x00 if the byte is nonzero and 0x01 if the byte is zero</returns>
            public static byte Zero(byte b) {
                return (byte) (((b | (~b + 1)) >> 7) & 1 ^ 1);
            }

            public byte Write() {
                return (byte) (
                    N << 7 |
                    V << 6 |
                    1 << 5 |
                    B << 4 |
                    D << 3 |
                    I << 2 |
                    Z << 1 |
                    C
                );
            }

            public void Read(byte P) {
                N = NonZero((byte) (P & 0b10000000));
                V = NonZero((byte) (P & 0b01000000));
                //  NonZero((byte) (P & 0b00100000));
                B = NonZero((byte) (P & 0b00010000));
                D = NonZero((byte) (P & 0b00001000));
                I = NonZero((byte) (P & 0b00000100));
                Z = NonZero((byte) (P & 0b00000010));
                C = NonZero((byte) (P & 0b00000001));
            }
        }

        public Flags P;

        // Micro-instruction data
        enum AddressingMode
        {
            NONE,
            ACC, IMP, IMM, REL,
            ZERO, ZEROX, ZEROY,
            IND, INDX, INDY, INDYW,
            ABS, ABSX, ABSXW, ABSY, ABSYW,
        }

        struct Instruction
        {
            public string Name;
            public AddressingMode Mode;
            public Cycle[] Cycles;

            public Instruction(string name, AddressingMode mode, Cycle[] cycles) {
                Name = name;
                Mode = mode;

                Cycle[] modeCycles = addressingInstructions[(int) mode];
                Cycles = new Cycle[modeCycles.Length + cycles.Length];
                modeCycles.CopyTo(Cycles, 0);
                cycles.CopyTo(Cycles, modeCycles.Length);
            }
        }

        private Instruction instr;
        private byte cycle, val;
        private ushort ptr, addr;

    #if DEBUG
        private bool _read;
        private ushort _addr;
        private byte _data;
    #endif

        // Interrupts
        enum HardwareInterrupt
        {
            NMI, IRQ
        }

        private HardwareInterrupt? incoming; // next cycle
        private HardwareInterrupt? pending;  // this cycle
        private HardwareInterrupt? previous; // previous cycle

        public CPU(IAddressable bus)
        {
            this.bus = bus;

            // On power-up, all registers are ZERO
            // This goes against the wiki, but in 2010 this has been attested using a transistor-level emulator
            // Source: https://www.pagetable.com/?p=410

            Reset();
        }
        
        public void Reset()
        {
            SetInstruction(RESETInstruction);
            this.cycle = 0;
        }

        public void AssertNMI()
        {
            incoming = HardwareInterrupt.NMI;
        }

        public void AssertIRQ()
        {
            // An incoming NMI interrupt has priority
            // Ignore if interrupt disable flag is 1
            if (incoming != HardwareInterrupt.NMI && P.I == 0)
            {
                incoming = HardwareInterrupt.IRQ;
            }
        }

        private void SetInstruction(Instruction instr)
        {
            this.instr = instr;
        }

        private void CycleEnd() {
            previous = pending;

            // An incoming NMI interrupt has priority
            // Reset IRQ signal (should only be high for 1 cycle)
            if (pending == HardwareInterrupt.IRQ) pending = incoming;

            incoming = null;
        }

        public void Cycle(int amount) {
            for (int i = 0; i < amount; i++) Cycle();
        }

        public int CycleInstruction() {
            int cycles = 1;

            if (this.cycle != 1) {
                Cycle();
                cycles += 1;
            }
            Cycle();
            
            while (this.cycle > 1 && this.instr.Cycles.Length > this.cycle) {
                Cycle();
                cycles += 1;
            } 

            return cycles;
        }

        public void Cycle()
        {
            // Get next instruction
            if (instr.Cycles.Length <= cycle)
            {
                if (pending != null)
                {
                    // Poll next interrupt
                    SetInstruction(pending == HardwareInterrupt.NMI ? NMIInstruction : IRQInstruction);
                    cycle = 0;
                }
                else
                {
                    // Fetch next instruction, execute first cycle
                    FetchPC(this);
                    SetInstruction(instructions[val]);
                    
                    cycle = 1;
                    CycleEnd();

                    // Return so we don't execute another cycle
                    return;
                }
            }

            // Execute instruction cycle
            instr.Cycles[cycle](this);

            // Continue to next cycle
            CycleEnd();
            unchecked { cycle += 1; }
        }

        public string DumpCycle() {
        #if DEBUG
            return string.Format("{6} #{7} | {11} ${12:X4} = {13:X2} | ptr:{8:X4} addr:{9:X4} val:{10:X2} | A:{0:X2} X:{1:X2} Y:{2:X2} P:{3} SP:01{4:X2} PC:{5:X4}",
                A, X, Y, P.Dump(), S, PC, instr.Name.PadRight(9, ' '), cycle - 1, ptr, addr, val, _read ? "READ " : "WRITE", _addr, _data);
        #else
            return string.Format("{6} #{7} | ptr:{8:X4} addr:{9:X4} val:{10:X2} | A:{0:X2} X:{1:X2} Y:{2:X2} P:{3} S:01{4:X2} PC:{5:X4}",
                A, X, Y, P.Dump(), S, PC, instr.Name.PadRight(9, ' '), cycle - 1, ptr, addr, val);
        #endif
        }
    }
}
