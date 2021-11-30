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
        struct Instruction
        {
        #if DEBUG
            public string Name;
        #endif
            public Cycle[] Cycles;

            public Instruction(string name, Cycle[] cycles) {
            #if DEBUG
                Name = name;
            #endif
                Cycles = cycles;
            }
        }

        private Instruction? instr;
        private byte cycle, val;
        private ushort ptr, addr;

    #if DEBUG
        private string _instr;
        private bool _read;
        private ushort _addr;
        private byte _data;
    #endif

        // Interrupts
        enum HardwareInterrupt
        {
            NMI, IRQ
        }

        private HardwareInterrupt? pending;

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
            instr = ResetInstruction;
        #if DEBUG
            _instr = instr.Value.Name;
        #endif
        }

        public void AssertNMI()
        {
            pending = HardwareInterrupt.NMI;
        }

        public void AssertIRQ()
        {
            // A pending NMI interrupt has priority
            if (pending != HardwareInterrupt.NMI)
            {
                pending = HardwareInterrupt.IRQ;
            }
        }

        public void Cycle(int amount) {
            for (int i = 0; i < amount; i++) Cycle();
        }

        public int CycleInstruction() {
            Cycle();
            int cycles = 1;
            while (instr != null) {
                Cycle();
                cycles += 1;
            }
            return cycles;
        }

        public void Cycle()
        {
            // Get next instruction
            if (instr == null)
            {
                if (pending != null)
                {
                    // Poll next interrupt
                    instr = pending == HardwareInterrupt.NMI ? NMIInstruction : IRQInstruction;
                #if DEBUG
                    _instr = instr.Value.Name;
                #endif
                    cycle = 0;
                }
                else
                {
                    // Fetch next instruction, execute first cycle
                    FetchPC(this);
                    instr = instructions[val];
                #if DEBUG
                    _instr = instr.Value.Name;
                #endif
                    cycle = 1;

                    // Reset IRQ signal (should only be high for 1 cycle)
                    if (pending == HardwareInterrupt.IRQ) pending = null;

                    // Return so we don't execute another cycle
                    return;
                }
            }

            // Execute instruction cycle
            instr.Value.Cycles[cycle](this);
            
            if (instr.Value.Cycles.Length > cycle + 1)
            {
                // Reset IRQ signal (should only be high for 1 cycle)
                if (pending == HardwareInterrupt.IRQ) pending = null;
            }
            else
            {
                // Instruction ended
                instr = null;

                // Reset NMI signal (should be reset after NMI handling)
                pending = null;
            }

            // Continue to next cycle
            cycle += 1;
        }

        public string DumpCycle() {
        #if DEBUG
            return string.Format("{6} #{7} | {11} ${12:X4} = {13:X2} | ptr:{8:X4} addr:{9:X4} val:{10:X2} | A:{0:X2} X:{1:X2} Y:{2:X2} P:{3} SP:01{4:X2} PC:{5:X4}",
                A, X, Y, P.Dump(), S, PC, _instr.PadRight(9, ' '), cycle - 1, ptr, addr, val, _read ? "READ " : "WRITE", _addr, _data);
        #else
            return string.Format("#{6} | ptr:{7:X4} addr:{8:X4} val:{9:X2} | A:{0:X2} X:{1:X2} Y:{2:X2} P:{3} S:01{4:X2} PC:{5:X4}",
                A, X, Y, P.Dump(), S, PC, cycle - 1, ptr, addr, val);
        #endif
        }
    }
}
