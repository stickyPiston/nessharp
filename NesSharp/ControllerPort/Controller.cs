using System;
using SFML.Window;
using static SFML.Window.Keyboard;

namespace NesSharp
{
    public class Controller : InputDevice
    {
        Keyboard.Key[] Keymap1 = new Keyboard.Key []{ Key.Z, Key.X, Key.RShift, Key.Enter, Key.Up, Key.Down, Key.Left, Key.Right };
        Keyboard.Key[] Keymap2 = new Keyboard.Key []{ Key.U, Key.O, Key.Escape, Key.Space, Key.I, Key.K, Key.J, Key.L };
        uint number, counter;

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
            counter = 8;
        }

        public override (byte, byte) Read()
        {
            if (counter == 0) return (1, 1);
            register = rol(register);
            byte Bit = (byte)(register & 0x1);
            counter--;
            return (Bit, 0x1);
        }
    }
}
