using System;

namespace NesSharp
{

    using Cycle = Func<CPU, bool>;

    public partial class CPU
    {
        // Bus
        private IAddressable bus;
        
        // Registers
        public ushort PC { get; private set; }
        private byte S, P, A, X, Y;

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

            P = 32; // Unused bit that is always 1

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

        public void CycleInstruction() {
            Cycle();
            while (instr != null) Cycle();
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
            if (instr.Value.Cycles[cycle](this) && instr.Value.Cycles.Length > cycle + 1)
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
                A, X, Y, Convert.ToString(P, 2).PadLeft(8, '0'), S, PC, _instr.PadRight(9, ' '), cycle - 1, ptr, addr, val, _read ? "READ " : "WRITE", _addr, _data);
        #else
            return string.Format("#{6} | ptr:{7:X4} addr:{8:X4} val:{9:X2} | A:{0:X2} X:{1:X2} Y:{2:X2} P:{3} S:01{4:X2} PC:{5:X4}",
                A, X, Y, Convert.ToString(P, 2).PadLeft(8, '0'), S, PC, cycle - 1, ptr, addr, val);
        #endif
        }
    }
}
