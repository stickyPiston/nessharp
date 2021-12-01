namespace NesSharp {
    class Sweeper {
        public bool enabled = true;
        public byte divider = 0x00;
        public bool negate  = false;
        public byte shift   = 0x00;
        private byte timer  = 0x00;

        public void ppuClock(ushort reload) {

        }

        public void apuClock(ushort reload, uint channel) {

        }
    };
};
