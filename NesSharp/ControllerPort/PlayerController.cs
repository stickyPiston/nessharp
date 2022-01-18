using System;

namespace NesSharp
{
    public enum Reset {
        NONE, SOFT, POWER
    }

    public interface IMovie {
        byte GetInput(uint controller);
        Reset GetReset();
        void Advance(bool vblSkip);
        bool Ended();
    }

    public class BK2 : IMovie {

        string[] lines;
        int current = 0;
        
        public BK2(string[] lines) {
            this.lines = lines;
            while (current < lines.Length && !lines[current].StartsWith('|')) current++;
            // current++;
        }

        public void Advance(bool vblSkip) {
            current++;
            while (current < lines.Length && !lines[current].StartsWith('|')) current++;
        }

        public bool Ended() {
            return current >= lines.Length;
        }

        public Reset GetReset() {
            if (current >= lines.Length || !lines[current].StartsWith('|')) return Reset.NONE;
            if (lines[current][1] != '.') return Reset.POWER;
            if (lines[current][2] != '.') return Reset.SOFT;
            return Reset.NONE;
        }

        public byte GetInput(uint controller) {
            if (current >= lines.Length || !lines[current].StartsWith('|')) return 0;

            string[] split = lines[current].Split('|');
            string controls = split[2 + controller];

            if (controls.Length != 8) return 0;

            byte b = 0;

            if (controls[3] != '.') b |= 1;
            if (controls[2] != '.') b |= 2;
            if (controls[1] != '.') b |= 4;
            if (controls[0] != '.') b |= 8;
            if (controls[4] != '.') b |= 16;
            if (controls[5] != '.') b |= 32;
            if (controls[6] != '.') b |= 64;
            if (controls[7] != '.') b |= 128;

            return b;
        }

    }

    public class FM2 : IMovie {

        string[] lines;
        int current = 0;
        
        public FM2(string[] lines) {
            this.lines = lines;
            while (current < lines.Length && !lines[current].StartsWith('|')) current++;
            current++;
        }

        public void Advance(bool vblSkip) {
            if (vblSkip) return;
            current++;
            while (current < lines.Length && !lines[current].StartsWith('|')) current++;
        }

        public bool Ended() {
            return current >= lines.Length;
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
