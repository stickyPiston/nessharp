using NUnit.Framework;
using NesSharp;
using NesSharp.PPU;
using SFML.Graphics;

namespace NesSharpTests {
    class ChipA : IAddressable {
        private Bus bus;
        public ChipA(Bus bus) { this.bus = bus; }

        public void exec() {
            bus.Write(0x120, 0x10);   
        }

        public byte Read(ushort addr) {
            return (addr == 0x12) ? (byte)0x20 : (byte)0x00;
        }
        public void Write(ushort _, byte __) { }
    }

    class ChipB : IAddressable {
        private Bus bus;
        public ChipB(Bus bus) { this.bus = bus; }

        public byte  data = 0;
        public ushort addr = 0;
        public void Write(ushort addr, byte data) {
            this.data = data;
            this.addr = addr;
        }

        public byte exec() { return bus.Read(0x12); }

        public byte Read(ushort _) { return 0; }
    }

    public class Tests {
        Bus bus;
        ChipA a;
        ChipB b;

        [SetUp]
        public void Setup() {
            bus = new Bus();
            a = new ChipA(bus); b = new ChipB(bus);
            bus.Register(a, new Range[]{new Range(0, 0x100)});
            bus.Register(b, new Range[]{new Range(0x101, 0x200)});
        }

        [Test]
        public void Test1() {
            a.exec();
            Assert.AreEqual(b.data, 0x10);
            Assert.AreEqual(b.addr, 0x120);
        }

        [Test]
        public void Test2() {
            byte val = b.exec();
            Assert.AreEqual(val, 0x20);
        }

        [Test]
        public void CPUTest() {
            RAM ram = new RAM(65536);
            ram.Write(0xFFFC, 0x00); // RESET $C000
            ram.Write(0xFFFD, 0xC0);
            ram.Write(0xC000, 0xA5); // LDA $12
            ram.Write(0xC001, 0x12);
            ram.Write(0xC002, 0x8D); // STA $0144
            ram.Write(0xC003, 0x44);
            ram.Write(0xC004, 0x01);

            bus.Register(ram, new Range[] { new Range(0xC000, 0xFFFF) });
            
            var cpu = new CPU(bus);

            PPU ppu = new PPU(null);
            PPUMemoryBus ppubus = ppu.bus;
            ppubus.Palettes = new PPUPalettes();
            ppubus.Palettes.Backgrounds = new[]
            {
                new Palette(new[] {Color.Red, Color.White, Color.Yellow}),
                new Palette(new[] {Color.Magenta, Color.Cyan, Color.Red,}),
                new Palette(new[] {Color.Green, Color.Red, Color.Blue,}), Palette.BasicColors,
            };
            ppubus.Nametables = new RandomRam();

            bus.Register(cpu);
            bus.Register(ppu);

            for (int i = 0; i < 15 * 3; i++) bus.Tick();

            Assert.AreEqual(0x20, b.data);
            Assert.AreEqual(0x0144, b.addr);
        }
    }
}
