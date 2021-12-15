using System.Threading;
using System.Collections.Generic;
using System;

namespace NesSharp {
    public struct Range {
        public ushort start;
        public ushort end;

        public Range(ushort start, ushort end) {
            this.start = start;
            this.end = end;
        }
    };

    public class Bus {
        private CPU cpu;
        private List<IAddressable> chips = new List<IAddressable>();
        private Dictionary<Range, IAddressable> ranges = new Dictionary<Range, IAddressable>();

        private byte clock = 0;
        private byte open = 0;

        //public Run(string romFilepath) { when the emulator accepts roms
        public void Run() {
            for(int i = 0; i < 29780 * 3; i++)
            {
                this.Tick();
            }
        }

        public void Tick() {
            // ppu.Cycle();
            if (clock == 0) cpu.Cycle();
            // apu.Cycle(); // apu works on ppu clock speed because of the sweepers' inherently higher clock speed

            // TODO: OAM DMA

            clock += 1;
            clock %= 3;
        }

        /// <summary>Sends a non-maskable interrupt to the CPU</summary>
        public void PullNMI()
        {
            cpu.PullNMI();
        }

        /// <summary>Keeps the IRQ line from the sender to the CPU high, until LowIRQ is called.</summary>
        public void HighIRQ(object sender)
        {
            cpu.HighIRQ(sender);
        }

        /// <summary>Resets the IRQ line from the sender to the CPU.</summary>
        public void LowIRQ(object sender)
        {
            cpu.LowIRQ(sender);
        }

        public void Register(IAddressable chip, Range[] ranges) {
            chips.Add(chip);
            foreach(var range in ranges)
            {
                this.ranges.Add(range, chip);
            }
        }

        public void Register(CPU cpu)
        {
            this.cpu = cpu;
        }

        public byte Read(ushort addr) {
            foreach(KeyValuePair<Range, IAddressable> range in ranges)
            {
                if(addr >= range.Key.start && addr <= range.Key.end)
                {
                    (byte read, byte setbits) = range.Value.Read(addr);
                    open = (byte) (read | (~setbits & open));
                    return open;
                }
            }
            return open;
        }

        public void Write(ushort addr, byte data) {
           foreach(KeyValuePair<Range, IAddressable> range in ranges)
           {
                if(addr >= range.Key.start && addr <= range.Key.end)
                {
                    range.Value.Write(addr, data);
                    return;
                }
           }
        }
    };
}
