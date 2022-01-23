using System;

namespace NesSharp
{   

    //sequencer influences the frequency of our soundwaves
    //resulting in different pitches
    //the sequencer saves the 'shape' of our soundwaves
    //it is part of the 'timer' in the nesdev wiki
    public class Sequencer
    {
        public readonly byte[] triangle_table ={
                                                 15, 14, 13, 12, 11, 10,  9,  8,
                                                 7,  6,  5,  4,  3,  2,  1,  0,
                                                 0,  1,  2,  3,  4,  5,  6,  7,
                                                 8,  9, 10, 11, 12, 13, 14, 15
                                                  };
        public uint sequence = 0x00000000;
        public uint new_sequence = 0x00000000;
        public ushort counter = 0x0000;
        public ushort tri_counter = 0x0000;
        public ushort reload = 0x0000;
        public byte output = 0x00;
        public int tri_Index = 0;

        public Sequencer(uint z, ushort x)
        {
            reload = x;
            counter = x;
            sequence = z;
            new_sequence = z;
        }
        public byte Clock(bool active, Func<UInt32, UInt32> funcTimer) {
          if (active) {   
            counter--;
            if (counter <= 0x00) {   
                counter = reload;
                sequence = funcTimer(sequence);
                output = (byte)(sequence & 0x00000001);
            }
          }
          return output;
        }

        public byte triClock(bool active)
        {
            if (active)
            {
                tri_counter--;
                if (tri_counter == 0x00)
                {
                    tri_counter = reload;
                    tri_Index = (tri_Index + 1) & 0x1F;
                    if (reload >= 2 && counter <= 0x7ff)
                    {
                        sequence = triangle_table[tri_Index];
                    }
                    output = (byte)(sequence);
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
        public void Clock(bool loop)
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
        public bool negate = false;
        public byte shift = 0x00;
        private byte timer = 0x00;
        public byte divider = 0x00;
        private ushort change = 0;
        public bool mute = false;
        //public bool channel; //1 = p1, 0 = p2

        public Sweeper(bool x, byte y)
        {
            enabled = x;
            down = x;
            reload = x;
            divider = y;
            shift = y;
            //channel = z;

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
        public bool SweepClock(ushort target, ushort channel)
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
        public byte counter = 0x00;
        public Lengthcounter(byte z)
        {
            counter = z;
        }
        public byte Clock(bool enable, bool pause)
        {
            if (!enable)
                counter = 0;
            else if (counter > 0 && !pause)
                    counter--;
            return counter;
        }
    }

    public class Linearcounter
    {
        //TODO
        //https://wiki.nesdev.org/w/index.php?title=APU_Triangle
        public bool reset = false;
        public bool start = false;
        ushort linear_counter = 0;
        public ushort reload = 0;
        public ushort output = 0;
        public Linearcounter(bool x, bool y, ushort z)
        {
            reset = x;
            start = y;
            reload = z;
        }
        public void Clock(bool start)
        {
            if (reset)
                linear_counter = reload;
            else if (linear_counter > 0)
                linear_counter--;

            if (!start)
            {
                reset = false;
            }
            //return output;
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
