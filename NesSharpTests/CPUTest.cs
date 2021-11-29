using NUnit.Framework;
using System;
using NesSharp;

namespace NesSharpTests
{
    class BigRAM : IAddressable
    {
        private byte[] data = new byte[65536];

        public byte Read(ushort addr)
        {
            return this.data[addr];
        }

        public void Write(ushort addr, byte data)
        {
            this.data[addr] = data;
        }
    }
    public class CPUTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Jam()
        {
            IAddressable bus = new BigRAM();
            CPU cpu = new CPU(bus);

            bus.Write(0xFFFC, 0x69);
            bus.Write(0xFFFD, 0x42);
            bus.Write(0x4269, 0x02); // should jam

            cpu.Cycle();
            Console.WriteLine(cpu.DumpCycle());
            cpu.Cycle();
            Console.WriteLine(cpu.DumpCycle());
            cpu.Cycle();
            Console.WriteLine(cpu.DumpCycle());
            cpu.Cycle();
            Console.WriteLine(cpu.DumpCycle());
            cpu.Cycle();
            Console.WriteLine(cpu.DumpCycle());
            cpu.Cycle();
            Console.WriteLine(cpu.DumpCycle());
            cpu.Cycle();
            Console.WriteLine(cpu.DumpCycle());
            cpu.Cycle();
            Console.WriteLine(cpu.DumpCycle());
            
            cpu.Cycle();
            Console.WriteLine(cpu.DumpCycle());

            try {
                // Should crash due to a jam
                cpu.Cycle();
                Console.WriteLine(cpu.DumpCycle());
                Assert.Fail();
            } catch {
                Assert.Pass();
            }
        }
    }
}