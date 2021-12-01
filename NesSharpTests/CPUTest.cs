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
            // if (addr == 0 && data != 0) throw new Exception(string.Format("Wrong CPU implementation! Error code {0:X2}", data));
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
        public void BitLogic()
        {
            Assert.AreEqual(1, CPU.Flags.NonZero(42));
            Assert.AreEqual(0, CPU.Flags.NonZero(0));
            Assert.AreEqual(0, CPU.Flags.Zero(42));
            Assert.AreEqual(1, CPU.Flags.Zero(0));
        }

        [Test]
        public void OverflowFlag()
        {
            bus.Write(0xFFFC, 0x00);
            bus.Write(0xFFFD, 0xC0);

            bus.Write(0xC000, 0x38); // SEC
            bus.Write(0xC001, 0xA9); // LDA
            bus.Write(0xC002, 0b10000000); // -128
            bus.Write(0xC003, 0x69); // ADC
            bus.Write(0xC004, 0b11111111); // -1

            bus.Write(0xC005, 0x18); // CLC
            bus.Write(0xC006, 0xA9); // LDA
            bus.Write(0xC007, 0b10000000); // -128
            bus.Write(0xC008, 0x69); // ADC
            bus.Write(0xC009, 0b11111111); // -1

            cpu.CycleInstruction(); // RESET

            cpu.CycleInstruction(); // SEC
            cpu.CycleInstruction(); // LDA
            cpu.CycleInstruction(); // ADC

            Assert.AreEqual(0, cpu.P.V);

            cpu.CycleInstruction(); // CLC
            cpu.CycleInstruction(); // LDA
            cpu.CycleInstruction(); // ADC

            Assert.AreEqual(1, cpu.P.V);
        }

        [Test]
        public void Instructions()
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
            int cycle = cpu.CycleInstruction();

            // Run
            Console.WriteLine();
            while (cycle < 14581) // Run until illegal opcode tests
            {
                cpu.Cycle();
                Console.WriteLine(cycle.ToString().PadLeft(5, '0') + " | " + cpu.DumpCycle());
                cycle += 1;
            }

            Assert.AreEqual(0x04, cpu.val); // The next should be an illegal opcode
        }

        [Test]
        public void LegalInstructions()
        {
            Assert.AreEqual(151, CPU.CountLegalInstructions());
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