namespace NesSharp
{
	public class RAM : IAddressable
	{
        private byte[] Data = new byte [2048];

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