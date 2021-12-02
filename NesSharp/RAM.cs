namespace NesSharp
{
    public class RAM : IAddressable
    {
        private byte[] Data;

        public RAM (ushort Capacity)
        {
            Data = new byte[Capacity];
        }

        public byte Read(ushort addr)
        {
            return Data[addr];
        }

        public void Write(ushort addr, byte data)
        {
            Data[addr] = data;
        }
    }
}