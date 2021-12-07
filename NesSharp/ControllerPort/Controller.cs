using System;
using SFML.Window;
using static SFML.Window.Keyboard;

namespace NesSharp
{
    public class Controller : InputDevice
    {
        Keyboard.Key[] Keymap1 = new Keyboard.Key []{ Key.Q, Key.E, Key.Escape, Key.Space, Key.W, Key.S, Key.A, Key.D };
        Keyboard.Key[] Keymap2 = new Keyboard.Key []{ Key.U, Key.O, Key.Escape, Key.Space, Key.I, Key.K, Key.J, Key.L };
        uint number;

        public Controller(uint number)
        {
            this.number = number;
        }

        public override void latchInput()
        {
            register = 0x0;
            var Keymap = (this.number == 1) ? Keymap1 : Keymap2;
            foreach (var key in Keymap)
            {
                register = (byte)((register << 1) | (IsKeyPressed(key) ? 1 : 0));
            }
        }

        public override byte Read()
        {
            byte Bit = (byte)(register & 0x1);
            ror(ref register);
            return Bit;
        }
    }
}
