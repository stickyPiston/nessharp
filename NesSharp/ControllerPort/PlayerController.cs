using System;

namespace NesSharp
{
    public enum Reset {
        NONE, SOFT, POWER
    }

    public interface IMovie {
        byte GetInput(uint controller);
        Reset GetReset();
        void Advance();
    }

    public class FM2 : IMovie {

        string[] lines;
        int current = 0;
        
        public FM2(string[] lines) {
            this.lines = lines;
            while (current < lines.Length && !lines[current].StartsWith('|')) current++;
        }

        public void Advance() {
            current++;
        }

        public Reset GetReset() {
            if (current >= lines.Length || !lines[current].StartsWith('|')) return Reset.NONE;
            return (Reset) (lines[current][1] - '0');
        }

        public byte GetInput(uint controller) {
            if (current >= lines.Length || !lines[current].StartsWith('|')) return 0;

            string[] split = lines[current].Split('|');
            string controls = split[2 + controller];

            if (controls.Length != 8) return 0;

            byte b = 0;

            for (int i = 0; i < 8; i++) {
                if (controls[i] != '.') {
                    b |= (byte) (1 << i);
                }
            }

            return b;
        }

    }

    public class PlayerController : InputDevice
    {
        IMovie movie;
        uint number, counter;

        public PlayerController(uint number, IMovie movie)
        {
            this.movie = movie;
            this.number = number;
        }

        public override void latchInput()
        {
            register = movie.GetInput(number);
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
