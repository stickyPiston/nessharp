using System;

namespace NesSharp
{
    public class ControllerPort : IAddressable
    {
        private InputDevice[] Cs = new InputDevice[]{null, null};
        private byte LastWritten = 0;
        
        public (byte, byte) Read(ushort addr) => Cs[addr - 0x4016].Read();

        public void Write(ushort addr, byte data)
        {
           if(data == 0 && LastWritten == 1)
           {
               Cs[0].latchInput();
               Cs[1].latchInput();
           }
           LastWritten = data;
        }

        public void register(InputDevice D)
        {
            if (Cs[0] == null)
            { 
                Cs[0] = D; 
            }
            else
            {
                Cs[1] = D;
            }
        }
    }
}
