using System;

namespace NesSharp
{
    public class CPU2A03
    {       
            public void CpuWrite(UInt16 addr, sbyte data)
            {

            }
            
            
            //cpuread method
            /*(public int CpuRead(ushort addr)
            {
            }*/
    }

    public class ApuClock
    {

    }

    //sequencer influences the frequency of our soundwaves
    //resulting in different pitches
    public class Sequencer
    {
        public uint sequence = 0x00000000;
        public uint new_sequence = 0x00000000;
        private UInt16 counter = 0x0000;
        private UInt16 reload = 0x0000;
        public sbyte output = 0x00;
        public int Clock(bool active, Func<uint, uint> funcTimer)
		{
			if (active)
			{   
				counter--;
				if (counter == 0xFFFF)
				{   
                    counter = reload;
					funcTimer(sequence);
                    output = (sbyte)(sequence & 0x00000001);
				}
            }
        return output;
		}
    }

    public class Envelope
    {
        void Apuclock(bool loop)
        {
            if (!start)
            {
                if (divider_counter == 0)
                {
                    divider_counter = volume;
                    if (decay_counter == 0)
                    {
                        if (loop)
                        {
                            decay_counter = 15;
                        }
                    }
                    else
                        decay_counter--;
                }
                else
                    divider_counter--;
            }
            else
            {
                start = false;
                divider_counter = volume;
                decay_counter = 15;
            }

            if (disable)
                output = volume;
            else
                output = decay_counter;
        }

        bool start = false;
        bool disable = false;
        UInt16 divider_counter = 0;
        UInt16 decay_counter = 0;
        UInt16 volume = 0;
        UInt16 output = 0;
    }

    public class Sweeper
    {
        public bool enabled = true;
        private bool down = false;
        private bool reload = false;
        public sbyte divider = 0x00;
        public bool negate = false;
        public sbyte shift = 0x00;
        private sbyte timer = 0x00;
        private ushort change = 0;
        public bool mute = false;

        //tracks
        public void PpuClock(ushort target)
        {
            if (enabled)
            {
                change = (ushort)(target >> shift);
                mute = (target < 8) || (target > 0x7FF);
            }
        }

        //clock
        public bool ApuClock(ushort target, ushort channel)
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
    public class Lengthcounter
    {
        sbyte counter = 0x00;
        sbyte Clock(bool enable, bool pause)
        {
            if (!enable)
                counter = 0;
            else
                if (counter > 0 && !pause)
                counter--;
            return counter;
        }
    }

    //oscillates the soundwaves resulting in smoother sounds
    public class Oscillator
    {
        double frequency = 0;
        double dutycycle = 0;
        double amplitude = 1;
        double pi = 3.141592653;
        double harmonics = 20;

        double Sample(double t)
        {
            double a = 0;
            double b = 0;
            double p = dutycycle * 2.0 * pi;

            for (double n = 1; n < harmonics; n++)
            {
                double c = n * frequency * 2.0 * pi * t;
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