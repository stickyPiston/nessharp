using System;

namespace NesSharp
{
    public class RAM : IAddressable
    {
        private byte[] Data;

        public RAM (int Capacity, bool init = false) // int because 65536 (the maximum storage amount) doesn't fit in a ushort
        {
            Data = new byte[Capacity];
            if (!init) return;
            for (int i = 0; i < 0x800; i++)
            {
                if ((i & 4) != 0)
                {
                    Data[i] = 0xFF;
                }
                else
                {
                    Data[i] = 0x00;
                }
            }
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
