using NesSharp;
using NesSharp.PPU;
using SFML.Graphics;
using NUnit.Framework;
using System.IO;
using System;

using Range = NesSharp.Range;

namespace NesSharpTests {
    public class PPUTests {
        Bus bus;
        CPU cpu;
        RAM ram;
        PPU ppu;
    
        [SetUp]
        public void Setup() {
            bus = new Bus();

            ram = new RAM(0x10000);
            bus.Register(ram, new Range[] { new Range(0x0000, 0x07FF), new Range(0x6000, 0xFFFF), new Range(0x4000, 0x4017) });

            cpu = new CPU(bus);
            bus.Register(cpu);

            PPU ppu = new PPU(null, bus);
            PPUMemoryBus ppubus = ppu.bus;
            ppubus.Palettes = new PPUPalettes();
            ppubus.Nametables = new RandomRam();
            ppubus.Patterntables = new RandomRam();
            bus.Register(ppu);
            bus.Register(new Repeater(ppu, 0x2000, 8), new Range[] { new Range(0x2000, 0x3FFF) });
        }

        public void ReadNES(string file) {
            byte[] bytes = File.ReadAllBytes("../../../roms/ppu_vbl_nmi/rom_singles/" + file);
            for (int i = 0; i + 0x8000 < 65536 && i + 16 < bytes.Length; i++) {
                bus.Write((ushort) (i + 0x8000), bytes[i + 16]);
            }
        }

        public void ReadOutput() {
            ushort read = 0x6004;
            while (ram.Read(read).Item1 != 0 && read < 0x8000) {
                Console.Write((char) ram.Read(read).Item1);
                read += 1;
            }
        }

        public void Run() {
            bool started = false;

            int cycle = 0;

            while (true) {
                bus.Tick();
                bus.Tick();
                bus.Tick();
                /* Console.WriteLine(cpu.DumpCycle()); */
                if (!started && ram.Read(0x6000).Item1 == 0x80) {
                    started = true;
                }
                else if (started && ram.Read(0x6000).Item1 != 0x80) {
                    break;
                }
                if (cpu.instr.Illegal) {
                    Console.WriteLine(cpu.DumpCycle());
                    Assert.Fail();
                    break;
                }
                if (cpu.instr.Name != "RESET" && cpu.PC < 0x8000) {
                    Console.WriteLine(cpu.DumpCycle());
                    Assert.Fail();
                    break;
                }
                if (cycle++ > 10_000_000) {
                    Console.WriteLine(cpu.DumpCycle());
                    Assert.Fail();
                    break;
                }
            }
            
            ReadOutput();
        }

        [Test]
        public void VblBasics() {
            ReadNES("01-vbl_basics.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void VblSetTime() {
            ReadNES("02-vbl_set_time.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        public void VblClearTime() {
            ReadNES("03-vbl_clear_time.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        public void NmiControl() {
            ReadNES("04-nmi_control.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        public void NmiTiming() {
            ReadNES("05-nmi_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        public void Suppression() {
            ReadNES("06-suppression.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        public void NmiOnTiming() {
            ReadNES("07-nmi_on_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        public void NmiOffTiming() {
            ReadNES("08-nmi_off_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        public void EvenOddFrames() {
            ReadNES("09-even_odd_frames.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        public void EvenOddTiming() {
            ReadNES("10-even_odd_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
    }
}
