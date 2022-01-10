namespace NesSharp
{
    public abstract class InputDevice
    {
        public byte register = 0x0;

        public abstract void latchInput();
        public abstract (byte, byte) Read();
        public byte rol(byte b) => (byte)((b << 1) | ((b & 0x80) >> 7));
    };
};
