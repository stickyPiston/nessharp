using System;

namespace NesSharp
{

    //sequencer influences the frequency of our soundwaves
    //resulting in different pitches
    public class Sequencer
    {

        public uint sequence = 0x00000000;
        public uint new_sequence = 0x00000000;
        public ushort counter = 0x0000;
        public ushort reload = 0x0000;
        public sbyte output = 0x00;

        public Sequencer(uint z, ushort x)
        {
            reload = x;
            counter = x;
            sequence = z;
            new_sequence = z;
        }
        public int Clock(bool active, Func<UInt32, UInt32> funcTimer) {
          if (active) {   
            counter--;
            if (counter == 0xFFFF) {   
                counter = reload;
                sequence = funcTimer(sequence);
                output = (sbyte)(sequence & 0x00000001);
            }
          }
          return output;
        }
    }

    public class Envelope
    {
        public bool start = false;
        public bool disable = false;
        UInt16 divider_counter = 0;
        UInt16 decay_counter = 0;
        public UInt16 volume = 0;
        public UInt16 output = 0;
        public Envelope(bool x, ushort z)
        {
            start = x;
            disable = x;
            volume = z;
        }
        public void ApuClock(bool loop)
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
            //return output;
        }
    }

    public class Sweeper
    {
        public bool enabled = true;
        public bool down = false;
        public bool reload = false;
        public sbyte divider = 0x00;
        public bool negate = false;
        public sbyte shift = 0x00;
        private sbyte timer = 0x00;
        private ushort change = 0;
        public bool mute = false;
        public bool channel; //1 = p1, 0 = p2

        public Sweeper(bool x, sbyte y, bool z)
        {
            enabled = x;
            down = x;
            reload = x;
            divider = y;
            shift = y;
            channel = z;

        }

        //tracks
        public void TrackClock(ushort target)
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
        public sbyte counter = 0x00;
        public Lengthcounter(sbyte z)
        {
            counter = z;
        }
        public sbyte Clock(bool enable, bool pause)
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
        public double dutycycle = 0;
        public double frequency = 0;
        public double amplitude = 1;

        public Oscillator(uint z)
        {
            dutycycle = z;
        }
        
        public double Sample(double t)
        {
            t = frequency * t;
            if (t - (int)t < dutycycle)
                return amplitude;
            else
                return -amplitude;

        }
    }
}
