using System.Threading;
using System.Collections.Generic;
using System;
using SFML.Audio;
using System.IO;
using System.Text;

using NesSharp.Mappers;

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

    public class Combinator : IAddressable {
        private IAddressable[] parents;
        private ushort start;
        private ushort repeat;

        public Combinator(IAddressable[] parents, ushort start, ushort repeat) {
            this.parents = parents;
            this.start = start;
            this.repeat = repeat;
        }

        public (byte, byte) Read(ushort addr) {
            return parents[(addr - start) / repeat].Read(addr);
        }

        public void Write(ushort addr, byte data) {
            parents[(addr - start) / repeat].Write(addr, data);
        }
    }

    public class Bus {
        private CPU cpu;
        internal PPU.PPU ppu;
        private X2A03 apu;
        internal BaseMapper mapper;
        private Dictionary<Range, IAddressable> ranges = new Dictionary<Range, IAddressable>();

        private byte clock = 3;
        private byte open = 0;

        private ushort OAMDMACycles = 0;
        private byte OAMDATA = 0;
        private ushort DMACopyAddr;

        public bool IsIRQHigh() {
            return mapper != null && mapper.IRQ;
        }

        public void SaveState(BinaryWriter writer) {
            writer.Write(clock);
            writer.Write(open);
            writer.Write(OAMDMACycles);
            writer.Write(OAMDATA);
            writer.Write(DMACopyAddr);

            cpu.SaveState(writer);
            ppu.SaveState(writer);
            mapper.SaveState(writer);
        }

        public void LoadState(BinaryReader reader) {
            clock = reader.ReadByte();
            open = reader.ReadByte();
            OAMDMACycles = reader.ReadUInt16();
            OAMDATA = reader.ReadByte();
            DMACopyAddr = reader.ReadUInt16();

            cpu.LoadState(reader);
            ppu.LoadState(reader);
            mapper.LoadState(reader);
        }

        //public Run(string romFilepath) { when the emulator accepts roms
        public void RunFrame() {
            do
            {
                this.Tick();
            }
            while (ppu.scanline != 0 || ppu.pixel != 0);
        }

        public void RunPreVblank() {
            do
            {
                this.Tick();
            }
            while (ppu.scanline != 241 || ppu.pixel != 2);
        }

        public string DumpCycle() {
            return $"{cpu.DumpCycle()} SL {ppu.scanline} CYC {ppu.pixel}";
        }

        public void Reset() {
            cpu.Reset();
            ppu.Reset();
            OAMDMACycles = 0;
            OAMDATA = 0;
            clock = 0;
        }

        public void BeginOAM(ushort DMACopyAddr) {
            OAMDMACycles = (ushort) (clock % 6 < 3 ? 514 : 513);
            this.DMACopyAddr = (ushort)(DMACopyAddr & 0xff00);
        }

        private int sampleSize = 16000;
        private short[] samples = new short[16000];
        private ushort sampleCounter = 0;
        private Sound sound = new Sound();

        public void Tick() {
            if (clock % 3 == 0)
            {
                if (OAMDMACycles == 0) {
                    cpu.Cycle();
                } else {
                    if (OAMDMACycles <= 512) {
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
                    OAMDMACycles--;
                }
            }

            ppu.Cycle();

            if (clock % 3 == 0 && OAMDMACycles == 0)
            {
                cpu.CycleEnd();
            }


            apu.Cycle();
            
#if !SERVER
            if (clock == 0)
            {
                if (sampleCounter == sampleSize)
                {
                    var buffer = new SoundBuffer(samples, 1, 44100);
                    sound.SoundBuffer = buffer;
                    sound.Play();
                    sampleCounter = 0;
                    Array.Clear(samples, 0, sampleSize);
                }
                else
                {
                    samples[sampleCounter++] = (short)(apu.noiseOutput() * 10000);
                }
            }
#endif

            clock += 1;
            clock %= 120;
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

        public void Register(IAddressable chip, Range[] ranges) {
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
            ppu.bus.Palettes = new PPU.PPUPalettes();

            Register(new Repeater(ppu, 0x2000, 8), new Range[] { new Range(0x2000, 0x3fff)});
            Register(ppu, new []{new Range(0x4014, 0x4014)});
        }

        public void Register(BaseMapper mapper)
        {
            this.mapper = mapper;
            ppu.bus.Nametables = mapper.Nametables;
            ppu.bus.Patterntables = mapper.CHR;
            Register(mapper.PRG, new[] { new Range(0x8000, 0xffff) });
            
            if (mapper.PRGRAM != null) {
                Register(mapper.PRGRAM, new Range[] {new Range(0x6000, 0x7FFF)});
            }
        }

        public void Register(X2A03 apu)
        {
            this.apu = apu;
            Register(apu, new Range[] { new Range(0x4000, 0x4013), new Range(0x4015, 0x4015), new Range(0x4017, 0x4017), new Range(0x400A, 0x400A), new Range(0x400B, 0x400B), new Range(0x400C, 0x400C), new Range(0x400E, 0x400E), new Range(0x400F, 0x400F) });

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

           // throw new Exception($"Can't write to {addr:x4}");
        }
    };
}
