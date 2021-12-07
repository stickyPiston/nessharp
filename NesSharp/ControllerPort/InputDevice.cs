namespace NesSharp
{
    public abstract class InputDevice
    {
        public byte register = 0x0;

        public abstract void latchInput();
        public abstract byte Read();
        public byte ror(ref byte b) => b = (byte)((b >> 1) | ((b & 1) << 7));
    };
};
