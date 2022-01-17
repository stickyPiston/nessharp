using NesSharp;
using NesSharp.PPU;
using SFML.Graphics;
using NUnit.Framework;
using System.IO;
using System;
using Range = NesSharp.Range;

namespace NesSharpTests
{
    public class PPUTests
    {
        Bus bus;
        CPU cpu;
        RAM ram;
        PPU ppu;
        private PPUMemoryBus ppubus;

        [SetUp]
        public void Setup()
        {
            bus = new Bus();

            cpu = new CPU(bus);
            bus.Register(cpu);

            // Create PPU
            PPU ppu = new PPU(null, bus);
            bus.Register(ppu);

            ram = new RAM(0x10000);
            bus.Register(ram, new []{ new Range(0, 0x800), new Range(0x6000, 0x7fff) });
            bus.Register(new Repeater(ram, 0, 0x800), new []{new Range(0x800, 0x1fff)});
        }

        public void ReadNES(string file)
        {
            Cartridge cart = RomParser.Parse("../../../roms/" + file);
            bus.Register(cart.mapper);
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

        #region vblTests

        [Test]
        public void VblBasics()
        {
            ReadNES("ppu_vbl_nmi/rom_singles/01-vbl_basics.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void VblSetTime()
        {
            ReadNES("ppu_vbl_nmi/rom_singles/02-vbl_set_time.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void VblClearTime()
        {
            ReadNES("ppu_vbl_nmi/rom_singles/03-vbl_clear_time.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void NmiControl()
        {
            ReadNES("ppu_vbl_nmi/rom_singles/04-nmi_control.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void NmiTiming()
        {
            ReadNES("ppu_vbl_nmi/rom_singles/05-nmi_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void Suppression()
        {
            ReadNES("ppu_vbl_nmi/rom_singles/06-suppression.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void NmiOnTiming()
        {
            ReadNES("ppu_vbl_nmi/rom_singles/07-nmi_on_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void NmiOffTiming()
        {
            ReadNES("ppu_vbl_nmi/rom_singles/08-nmi_off_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void EvenOddFrames()
        {
            ReadNES("ppu_vbl_nmi/rom_singles/09-even_odd_frames.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void EvenOddTiming()
        {
            ReadNES("ppu_vbl_nmi/rom_singles/10-even_odd_timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        #endregion

        #region spriteHitTests
        
        [Test]
        public void sprite_hit_basics()
        {
            ReadNES("ppu_sprite_hit/01-basics.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        [Test]
        public void sprite_hit_alignment()
        {
            ReadNES("ppu_sprite_hit/02-alignment.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_corners()
        {
            ReadNES("ppu_sprite_hit/03-corners.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        

        [Test]
        public void sprite_hit_flip()
        {
            ReadNES("ppu_sprite_hit/04-flip.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_left_clip()
        {
            ReadNES("ppu_sprite_hit/05-left_clip.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        [Test]
        public void sprite_hit_right_edge()
        {
            ReadNES("ppu_sprite_hit/06-right_edge.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_screen_bottom()
        {
            ReadNES("ppu_sprite_hit/07-screen_bottom.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        [Test]
        public void sprite_hit_double_height()
        {
            ReadNES("ppu_sprite_hit/08-double_height.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        [Test]
        public void sprite_hit_timing()
        {
            ReadNES("ppu_sprite_hit/09-timing.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }
        [Test]
        public void sprite_hit_timing_order()
        {
            ReadNES("ppu_sprite_hit/10-timing_order.nes");
            Run();
            Assert.AreEqual(0, ram.Read(0x6000).Item1);
        }

        #endregion
    }
}
