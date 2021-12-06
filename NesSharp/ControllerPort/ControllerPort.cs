using System;

namespace NesSharp
{
    public class ControllerPort : IAddressable
    {
        private InputDevice[] Cs = new InputDevice[]{null, null};
        private byte LastWritten = 0;
        
        public byte Read(ushort addr) => (byte)((addr == 0x4016 || addr == 0x4017) ? Cs[addr - 0x4016].Read() : 0x0);

        public void Write(ushort addr, byte data)
        {
           if(data == 0 && LastWritten == 1)
           {
               Cs[addr - 0x4016].latchInput();
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
