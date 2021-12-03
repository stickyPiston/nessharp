using System;
using System.Collections.Generic;

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
        public enum AddressingMode
        {
            NONE,
            ACC, IMP, IMM, REL,
            ZERO, ZEROX, ZEROY,
            INDX, INDY,
            ABS, ABSX, ABSY,
        }

        public struct Instruction
        {
            public string Name;
            public AddressingMode Mode;
            public Cycle[] Cycles;
            public bool ReadsAddr;
            public bool WritesAddr;

            public Instruction(string name, AddressingMode mode, bool readsAddr, Cycle[] cycles, bool writesAddr = false) {
                Name = name;
                Mode = mode;
                ReadsAddr = readsAddr;
                WritesAddr = writesAddr;

                Cycle[] modeCycles = addressingInstructions[(int) mode];
                Cycles = new Cycle[modeCycles.Length + cycles.Length + (readsAddr ? 1 : 0) + (writesAddr ? 1 : 0)];

                modeCycles.CopyTo(Cycles, 0);
                if (readsAddr)
                    Cycles[modeCycles.Length] = ValFromAddr;
                cycles.CopyTo(Cycles, modeCycles.Length + (readsAddr ? 1 : 0));
                if (writesAddr)
                    Cycles[modeCycles.Length + cycles.Length + (readsAddr ? 1 : 0)] = WriteVal;
            }
        }

        public Instruction instr { get; private set; }
        public byte cycle { get; private set; }
        public byte val { get; private set; }
        public ushort addr { get; private set; }

    #if DEBUG
        private bool _read;
        private ushort _addr;
        private byte _data;
    #endif

        // Interrupts
        public enum HardwareInterrupt
        {
            NMI, IRQ
        }

        private ISet<object> incomingIRQ = new HashSet<object>();
        private bool incomingNMI;

        public HardwareInterrupt? pending  { get; private set; } // this cycle
        public HardwareInterrupt? previous { get; private set; } // previous cycle

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

        public void PullNMI()
        {
            incomingNMI = true;
        }

        public void HighIRQ(object sender)
        {
            incomingIRQ.Add(sender);
        }

        public void LowIRQ(object sender)
        {
            incomingIRQ.Remove(sender);
        }

        private void SetInstruction(Instruction instr)
        {
            this.instr = instr;
        }

        private void CycleEnd() {
            previous = pending;

            // An pending NMI interrupt has priority
            if (incomingNMI)
            {
                pending = HardwareInterrupt.NMI;
                incomingNMI = false;
            }
            else if (incomingIRQ.Count > 0 && P.I == 0)
            {
                pending = HardwareInterrupt.IRQ;
            }
            // NMI stays pending until it is handled
            else if (pending != HardwareInterrupt.NMI)
            {
                pending = null;
            }
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
                    cycle = 0;
                    SetInstruction(pending == HardwareInterrupt.NMI ? NMIInstruction : IRQInstruction);
                }
                else
                {
                    // Fetch next instruction, execute first cycle
                    cycle = 0;
                    ValFromPC(this);
                    SetInstruction(instructions[val]);
                    
                    // Continue to next cycle
                    CycleEnd();
                    cycle += 1;

                    // Return so we don't execute another cycle
                    return;
                }
            }

            // Execute instruction cycle
            instr.Cycles[cycle](this);

            // Continue to next cycle
            CycleEnd();
            cycle += 1;
        }

        public string DumpCycle() {
        #if DEBUG
            return string.Format("{6} #{7} | {10} ${11:X4} = {12:X2} | addr:{8:X4} val:{9:X2} | A:{0:X2} X:{1:X2} Y:{2:X2} P:{3} SP:01{4:X2} PC:{5:X4}",
                A, X, Y, P.Dump(), S, PC, instr.Name.PadRight(9, ' '), cycle - 1, addr, val, _read ? "READ " : "WRITE", _addr, _data);
        #else
            return string.Format("{6} #{7} | addr:{8:X4} val:{9:X2} | A:{0:X2} X:{1:X2} Y:{2:X2} P:{3} S:01{4:X2} PC:{5:X4}",
                A, X, Y, P.Dump(), S, PC, instr.Name.PadRight(9, ' '), cycle - 1, addr, val);
        #endif
        }
    }
}
