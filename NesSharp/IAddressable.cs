using System;

namespace NesSharp
{
    public interface IAddressable
    {
        (byte, byte) Read(ushort addr);
        void Write(ushort addr, byte data);
    }
}
