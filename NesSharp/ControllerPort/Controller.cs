using SFML.Window;
using static SFML.Window.Keyboard;

namespace NesSharp
{
    public class Controller : InputDevice
    {
        Keyboard.Key[] Keymap1;
        Keyboard.Key[] Keymap2;
        uint number, counter;


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
                register = (byte)((register << 1) | (InputManager.keysPressed.Contains(key) ? 1 : 0));
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
