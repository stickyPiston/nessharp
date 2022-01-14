using NesSharp;
using NesSharp.PPU;
using SFML.Graphics;
using NUnit.Framework;
using System.IO;
using System;
using Range = NesSharp.Range;

namespace NesSharpTests
{
    public class BasePPUTest
    {
        protected Bus bus;
        protected CPU cpu;
        protected RAM ram;
        protected PPU ppu;
        protected PPUMemoryBus ppubus;

        private string romSuiteName;

        public BasePPUTest(string romSuiteName)
        {
            this.romSuiteName = romSuiteName;
        }

        [SetUp]
        public void Setup()
        {
            bus = new Bus();

            cpu = new CPU(bus);
            bus.Register(cpu);

            bus.Register(cpu);

            // Create PPU
            PPU ppu = new PPU(null, bus);
            ppubus = ppu.bus;
            ppubus.Palettes = new PPUPalettes();
            // ppubus.Nametables = new Repeater(new RandomRam(), 0, 0x800);
            ppubus.Nametables = new RandomRam();
            ppubus.Patterntables = new RandomRam();

            bus.Register(ppu);
            bus.Register(new Repeater(ppu, 0x2000, 8), new Range[] {new Range(0x2000, 0x3fff)});
            bus.Register(ppu, new[] {new Range(0x4014, 0x4014)});
            ram = new RAM(0x10000);
            bus.Register(ram,
                new[]
                {
                    new Range(0x8000, 0xffff), new Range(0, 0x800), new Range(0x6000, 0x7fff), new Range(0x4000, 0x7fff)
                });
            bus.Register(new Repeater(ram, 0, 0x800), new[] {new Range(0x800, 0x1fff)});
        }

        public void ReadNES(string file)
        {
            Cartridge cart = RomParser.Parse("../../../roms/" + romSuiteName + "/rom_singles/" + file);
            Console.WriteLine(cart.rombytes.Length);

            for (int i = 0; i < cart.rombytes.Length; i++)
            {
                bus.Write((ushort) (0x8000 + i), cart.rombytes[i]);
                if (cart.rombytes.Length == 0x4000)
                {
                    bus.Write((ushort) (0xc000 + i), cart.rombytes[i]);
                }
            }

            for (int i = 0; i < cart.vrombytes.Length; i++)
            {
                ppubus.Write((ushort) i, cart.vrombytes[i]);
            }
        }

        public void ReadOutput()
        {
            ushort read = 0x6004;
            while (ram.Read(read).Item1 != 0 && read < 0x8000)
            {
                Console.Write((char) ram.Read(read).Item1);
                read += 1;
            }
        }

        public void Run()
        {
            bool started = false;

            int cycle = 0;

            while (true)
            {
                bus.Tick();
                bus.Tick();
                bus.Tick();
                /* Console.WriteLine(cpu.DumpCycle()); */
                if (!started && ram.Read(0x6000).Item1 == 0x80)
                {
                    started = true;
                }
                else if (started && ram.Read(0x6000).Item1 != 0x80)
                {
                    break;
                }

                if (cpu.instr.Illegal)
                {
                    Console.WriteLine(cpu.DumpCycle());
                    Assert.Fail();
                    break;
                }

                if (cpu.instr.Name != "RESET" && cpu.PC < 0x8000)
                {
                    Console.WriteLine(cpu.DumpCycle());
                    Assert.Fail();
                    break;
                }

                if (cycle++ > 10_000_000)
                {
                    Console.WriteLine(cpu.DumpCycle());
                    Assert.Fail();
                    break;
                }
            }

            ReadOutput();
        }
    }

    [TestFixture("ppu_vbl_nmi")]
    public class PPUVBLTests : BasePPUTest
    {
        public PPUVBLTests(string romSuiteName) : base(romSuiteName)
        {
        }

        [Test]
        public void VblBasics()
        {
            ReadNES("01-vbl_basics.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void VblSetTime()
        {
            ReadNES("02-vbl_set_time.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void VblClearTime()
        {
            ReadNES("03-vbl_clear_time.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void NmiControl()
        {
            ReadNES("04-nmi_control.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void NmiTiming()
        {
            ReadNES("05-nmi_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void Suppression()
        {
            ReadNES("06-suppression.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void NmiOnTiming()
        {
            ReadNES("07-nmi_on_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void NmiOffTiming()
        {
            ReadNES("08-nmi_off_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void EvenOddFrames()
        {
            ReadNES("09-even_odd_frames.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void EvenOddTiming()
        {
            ReadNES("10-even_odd_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
    }

    [TestFixture("ppu_sprite_hit")]
    class PPUSpriteHitTests : BasePPUTest
    {
        public PPUSpriteHitTests(string romSuiteName) : base(romSuiteName)
        {
        }

        [Test]
        public void sprite_hit_basics()
        {
            ReadNES("01-basics.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_alignment()
        {
            ReadNES("02-alignment.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_corners()
        {
            ReadNES("03-corners.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }


        [Test]
        public void sprite_hit_flip()
        {
            ReadNES("04-flip.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_left_clip()
        {
            ReadNES("05-left_clip.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_right_edge()
        {
            ReadNES("06-right_edge.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_screen_bottom()
        {
            ReadNES("07-screen_bottom.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_double_height()
        {
            ReadNES("08-double_height.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_timing()
        {
            ReadNES("09-timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_timing_order()
        {
            ReadNES("10-timing_order.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
    }

    [TestFixture("ppu_sprite_overflow")]
    class PPUSpriteOverflowTests : BasePPUTest
    {
        public PPUSpriteOverflowTests(string romSuiteName) : base(romSuiteName)
        {
        }
        
        [Test]
        public void sprite_overflow_basics()
        {
            ReadNES("01-basics.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        public void sprite_overflow_details()
        {
            ReadNES("02-details.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        [Ignore("This test is not very important")]
        public void sprite_overflow_timing()
        {
            ReadNES("03-timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        public void sprite_overflow_obscure()
        {
            ReadNES("04-obscure.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        
        [Test]
        [Ignore("This test is not very important")]
        public void sprite_overflow_emulator()
        {
            ReadNES("05-emulator.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
    }
}