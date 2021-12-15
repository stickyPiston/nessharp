namespace NesSharp
{
    public class RAM : IAddressable
    {
        private byte[] Data;

        public RAM (int Capacity) // int because 65536 (the maximum storage amount) doesn't fit in a ushort
        {
            Data = new byte[Capacity];
        }

        public (byte, byte) Read(ushort addr)
        {
            return (Data[addr], 0xFF);
        }

        public void Write(ushort addr, byte data)
        {
            Data[addr] = data;
        }
    }
}
