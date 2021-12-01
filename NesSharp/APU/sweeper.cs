using System;

namespace NesSharp
{
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
    };
};