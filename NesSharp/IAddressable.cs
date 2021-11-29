using System;

namespace NesSharp
{
    interface IAddressable
    {
        byte Read(ushort addr);
        void Write(ushort addr, byte data);
    }
}
