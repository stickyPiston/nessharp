using NUnit.Framework;
using NesSharp;

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
        ChipA a;
        ChipB b;

        [SetUp]
        public void Setup() {
            var bus = new Bus();
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
    }
}
