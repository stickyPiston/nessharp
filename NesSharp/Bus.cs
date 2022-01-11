using System.Threading;
using System.Collections.Generic;
using System;
using SFML.Audio;

namespace NesSharp {
    public struct Range {
        public ushort start;
        public ushort end;

        public Range(ushort start, ushort end) {
            this.start = start;
            this.end = end;
        }
    };

    public class Repeater : IAddressable {
        private IAddressable parent;
        private ushort start;
        private ushort repeat;

        public Repeater(IAddressable parent, ushort start, ushort repeat) {
            this.parent = parent;
            this.start = start;
            this.repeat = repeat;
        }

        public (byte, byte) Read(ushort addr) {
            return parent.Read((ushort)((addr - start) % repeat + start));
        }

        public void Write(ushort addr, byte data) {
            parent.Write((ushort)((addr - start) % repeat + start), data);
        }
    }

    public class Bus {
        private CPU cpu;
        private PPU.PPU ppu;
        private X2A03 apu;
        private List<IAddressable> chips = new List<IAddressable>();
        private Dictionary<Range, IAddressable> ranges = new Dictionary<Range, IAddressable>();

        private byte clock = 0;
        private byte open = 0;

        private int OAMDMACycles = 0;
        private byte OAMDATA = 0;
        private ushort DMACopyAddr;

        public void RunFrame() {
            int frames = ppu.FrameCycleCount();
            for(int i = 0; i < frames; i++)
            {
                this.Tick();
            }
        }

        public void BeginOAM(ushort DMACopyAddr) {
            OAMDMACycles = clock < 3 ? 514 : 513;
            this.DMACopyAddr = (ushort)(DMACopyAddr & 0xff00);
        }

        private short[] samples = new short[13538];
        private ushort sampleCounter = 0;

        public void Tick() {
            ppu.Cycle();

            if (clock % 3 == 0)
            {
                
                if (OAMDMACycles == 0) {
                    cpu.Cycle();
                } else if (OAMDMACycles <= 512) {
                    int cycle = 512 - OAMDMACycles;
                    switch (cycle & 1) {
                        case 0:
                            OAMDATA = Read((ushort) (DMACopyAddr | (cycle >> 1)));
                            break;
                        case 1:
                            ppu.Write(0x2004, OAMDATA);
                            break;
                    }
                }
                // Console.WriteLine(cpu.DumpCycle());
            if (OAMDMACycles > 0) OAMDMACycles--;
            }


            apu.Cycle();

            if (clock == 0)
            {
                if (sampleCounter == 13538)
                {// 512, 768, 1024, 2048, 2304, 2321, 2560
                    var buffer = new SoundBuffer(samples, 1, 44100);//44100
                    var sound = new Sound(buffer);
                    sound.Play();
                    Console.WriteLine(apu.output() * 10000);
                    sampleCounter = 0;
                    Array.Clear(samples, 0, 13538);
                }
                else
                {
                    samples[sampleCounter++] = (short)(apu.output() * 10000);
                }
            }

            clock += 1;
            clock %= 121;
        }

        /// <summary>Sends a non-maskable interrupt to the CPU</summary>
        public void LowNMI()
        {
            cpu.LowNMI();
        }

        /// <summary>If the NMI is set high before the CPU could read the NMI status, the NMI is ignored</summary>
        public void HighNMI()
        {
            cpu.HighNMI();
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

        public void Register(X2A03 apu)
        {
            this.apu = apu;
        }

        public byte Read(ushort addr) {
            // Console.WriteLine($"read {addr:x4}");

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
            // Console.WriteLine($"{addr:x4} = {data:x2}");
           foreach(KeyValuePair<Range, IAddressable> range in ranges)
           {
                if(addr >= range.Key.start && addr <= range.Key.end)
                {
                    range.Value.Write(addr, data);
                    return;
                }
           }

           throw new Exception($"Can't write to {addr:x4}");
        }
    };
}
