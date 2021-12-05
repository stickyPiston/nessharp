using System;

namespace NesSharp
{
    class sequencer
    {
        public byte sequence = 0x00000000;
        public byte new_sequence = 0x00000000;
        private UInt16 timer = 0x0000;
        private byte reload = 0x0000;
        public byte output = 0x00;
        public int Clock(bool bEnable, Func<uint, uint> funcManip)
		{
			if (bEnable)
			{
				timer--;
				if (timer == 0xFFFF)
				{
					timer = reload;
					funcManip(sequence);
                    output = (byte)(sequence & 0x00000001);
				}
            }
        return output;
		}
    }

    class envelope
    {
        void apuclock(bool bLoop)
        {
            if (!start)
            {
                if (divider_count == 0)
                {
                    divider_count = volume;

                    if (decay_count == 0)
                    {
                        if (bLoop)
                        {
                            decay_count = 15;
                        }

                    }
                    else
                        decay_count--;
                }
                else
                    divider_count--;
            }
            else
            {
                start = false;
                decay_count = 15;
                divider_count = volume;
            }

            if (disable)
            {
                output = volume;
            }
            else
            {
                output = decay_count;
            }
        }

        bool start = false;
        bool disable = false;
        UInt16 divider_count = 0;
        UInt16 volume = 0;
        UInt16 output = 0;
        UInt16 decay_count = 0;
    }
    class Sweeper
    {
        public bool enabled = true;
        private bool down = false;
        private bool reload = false;
        //divider = period
        public byte divider = 0x00;
        public bool negate = false;
        public byte shift = 0x00;
        private byte timer = 0x00;
        private ushort change = 0;
        public bool mute = false;

        //tracks
        public void ppuClock(ushort target)
        {
            if (enabled)
            {
                change = (ushort)(target >> shift);
                mute = (target < 8) || (target > 0x7FF);
            }
        }

        //clock
        public bool apuClock(ushort target, ushort channel)
        {
            bool changed = false;
            if (timer == 0 && enabled && shift > 0 && !mute)
            {
                if (target >= 8 && change < 0x07FF)
                {
                    if (down)
                    {
                        target -= Convert.ToUInt16(change - channel);
                    }
                    else
                    {
                        target += change;
                    }
                    changed = true;
                }
            }
            if (timer == 0 || reload)
            {
                timer = divider;
                reload = false;
            }
            else
                timer--;

            mute = (target < 8) || (target > 0x7FF);
            return changed;
        }
    }
    class Lengthcounter
    {
        UInt16 counter = 0x00;
        UInt16 clock(bool bEnable, bool bHalt)
        {
            if (!bEnable)
                counter = 0;
            else
                if (counter > 0 && !bHalt)
                counter--;
            return counter;
        }
    }

    class Oscpulse
    {
        double frequency = 0;
        double dutycycle = 0;
        double amplitude = 1;
        double pi = 3.14159;
        double harmonics = 20;

        double Sample(double t)
        {
            double a = 0;
            double b = 0;
            double p = dutycycle * 2.0 * pi;

            for (double n = 1; n < harmonics; n++)
            {
                double c = n * frequency * 2.0 * pi * t;
                double c2 = Sinus(c);
                a += -Sinus(c) / n;
                b += -Sinus(c - p * n) / n;
            }

            return (2.0 * amplitude / pi) * (a - b);
        }

        static double Sinus(double t)
        {
            double j = t * 0.15915;
            j = j - (int)j;
            return 20.785 * j * (j - 0.5) * (j - 1.0f);
        }
    }
}