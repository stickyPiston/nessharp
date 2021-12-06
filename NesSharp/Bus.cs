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

    public class Bus : IAddressable {
        private CPU cpu;
        private List<IAddressable> chips = new List<IAddressable>();
        private Dictionary<Range, IAddressable> ranges = new Dictionary<Range, IAddressable>();

        private byte clock = 0;

        public Bus() {
            cpu = new CPU(this);
        }

        //public Run(string romFilepath) { when the emulator accepts roms
        public void Run() {
            throw new NotImplementedException();
        }

        public void Tick() {
            // ppu.Cycle();
            if (clock % 3 == 0) cpu.Cycle();
            // if (clock % 6 == 0) apu.Cycle();

            // TODO: OAM DMA

            clock += 1;
            clock %= 6;
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

        public byte Read(ushort addr) {
            foreach(KeyValuePair<Range, IAddressable> range in ranges)
            {
                if(addr >= range.Key.start && addr <= range.Key.end)
                {
                    return range.Value.Read(addr);
                }
            }
            return 0;
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
