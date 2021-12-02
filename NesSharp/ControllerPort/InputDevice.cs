namespace NesSharp {
    abstract class InputDevice {
        private byte register = 0x0;
        public void latchInput();
        public byte Read();
    };
};
