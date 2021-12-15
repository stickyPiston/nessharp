using System;
using System.Linq;
using SFML.Window;
using static SFML.Window.Keyboard;

namespace NesSharp
{
    public class Controller : InputDevice
    {
        Keyboard.Key[] Keymap1;
        Keyboard.Key[] Keymap2;
        uint number;

        public Controller(uint number)
        {
            var config = ConfigurationManager.getConfig();
            Keymap1 = config.Keymap1;
            Keymap2 = config.Keymap2;
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
        private Keyboard.Key mapStringToKey(string s) {
            switch (s) {
                case "Q": return Key.Q;
                case "E": return Key.E;
                case "Esc": return Key.Escape;
                case "Spc": return Key.Space;
                case "W": return Key.W;
                case "S": return Key.S;
                case "A": return Key.A;
                case "D": return Key.D;
                case "U": return Key.U;
                case "O": return Key.O;
                case "I": return Key.I;
                case "K": return Key.K;
                case "J": return Key.J;
                case "L": return Key.L;
            }
            return Key.A;
        }
    }
}
