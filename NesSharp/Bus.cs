using System;
using System.Collections.Generic;
using System.Text;

namespace NesSharp
{
    class Bus : IAddressable
    {
        public byte Read(ushort addr)
        {
            throw new NotImplementedException();
        }

        public void Write(ushort addr, byte data)
        {
            throw new NotImplementedException();
        }
    }
}
