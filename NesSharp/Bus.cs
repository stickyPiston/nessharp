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
        private PPU.PPU ppu;
        private List<IAddressable> chips = new List<IAddressable>();
        private Dictionary<Range, IAddressable> ranges = new Dictionary<Range, IAddressable>();

        private byte clock = 0;

        //public Run(string romFilepath) { when the emulator accepts roms
        public void RunFrame() {
            int frames = ppu.FrameCycleCount();
            for(int i = 0; i < frames; i++)
            {
                this.Tick();
            }
        }

        public void Tick() {
            ppu.Cycle();
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

        public void Register(PPU.PPU ppu)
        {
            this.ppu = ppu;
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
