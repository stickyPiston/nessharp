using NUnit.Framework;
using System;
using System.IO;
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
        private IAddressable bus;
        private CPU cpu;

        [SetUp]
        public void Setup()
        {
            bus = new BigRAM();
            cpu = new CPU(bus);
        }

        [Test]
        public void CPU()
        {
            // Write rom
            byte[] bytes = File.ReadAllBytes("../../../roms/nestest.nes");
            for (int i = 0; i + 0xC000 < 65536 && i + 16 < bytes.Length; i++)
            {
                bus.Write((ushort) (i + 0x8000), bytes[i + 16]);
                bus.Write((ushort) (i + 0xC000), bytes[i + 16]);
            }

            bus.Write(0xFFFC, 0x00);
            bus.Write(0xFFFD, 0xC0);

            // RESET
            cpu.CycleInstruction();

            // Run
            Console.WriteLine();
            while (cpu.PC != 0xC66E)
            {
                cpu.Cycle();
                Console.WriteLine(cpu.DumpCycle());
            }
        }

        [Test]
        public void Jam()
        {
            bus.Write(0xFFFC, 0x69);
            bus.Write(0xFFFD, 0x42);
            bus.Write(0x4269, 0x02);

            // RESET instruction
            cpu.CycleInstruction();
            
            // JAM instruction
            cpu.Cycle();

            try {
                // Should crash due to a jam
                cpu.Cycle();
                Console.WriteLine(cpu.DumpCycle());
                Assert.Fail();
            } catch (CPU.OpcodeException) {
                Assert.Pass();
            }
        }
    }
}