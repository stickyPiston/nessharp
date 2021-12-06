using NUnit.Framework;
using NesSharp;
using System;

namespace NesSharpTests
{
    class EmulatedKeyboard : InputDevice
    {
        public override void latchInput()
        {
            // SFML would get input from the keyboard here
            // Instead, we return a fixed sequence of bits
            register = 0b10000101; // A, Down, Right
        }

        public override byte Read()
        {
            byte bit = (byte)(register & 1);
            ror(ref register);
            return bit;
        }
    };

    public class ControllerPortTests
    {
        private ControllerPort cp = new ControllerPort();

        [SetUp]
        public void Setup() {
            cp.register(new EmulatedKeyboard());
            cp.register(new EmulatedKeyboard());
        }

        [Test(Description = "Test whether a single controller works")]
        public void singleControllerTest()
        {
            for (ushort addr = 0x4016; addr <= 0x4017; addr++)
            {
                // We haven't allowed the controller to gather input so
                // bit should be 0 (the default value) here.

                if (addr == 0x4016)
                {
                    var b = cp.Read(addr);
                    Assert.AreEqual(b, 0x0);
                }

                cp.Write(addr, 0x1); // Allow the gathering of input
                cp.Write(addr, 0x0); // Stop the gathering of input

                byte output = 0x0;
                for (uint i = 0; i < 8; i++)
                    output |= (byte)(cp.Read(addr) << (byte)i);

                Assert.AreEqual(output, 0b10000101);

                var bit = cp.Read(addr);
                Assert.AreEqual(output & 1, bit);

                // The register should reset after the 1->0 writing
                cp.Write(addr, 0x1); // Allow the gathering of input
                cp.Write(addr, 0x0); // Stop the gathering of input

                bit = cp.Read(addr);
                Assert.AreEqual(output & 1, bit);
            }
        }
    };
};
