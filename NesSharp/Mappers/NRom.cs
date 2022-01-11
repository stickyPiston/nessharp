using System;
using System.Collections.Generic;
using System.Text;

namespace NesSharp.Mappers
{
    class NRomPRG : IAddressable
    {
        byte[] ROM = new byte[0x8000];
        //No RAM because we don't emulate family BASIC

        public NRomPRG(byte[] RomData)
        {
            ROM = new byte[0x8000];
            if(RomData.Length == 0x8000)
            {
                Array.Copy(RomData, ROM, 0x8000);
            }
            else 
            {
                Array.Copy(RomData, 0, ROM, 0x0000, 0x4000);
                Array.Copy(RomData, 0, ROM, 0x4000, 0x4000);
            }
        }

        public (byte, byte) Read(ushort addr)
        {
            return (ROM[addr-0x8000], 0xff);
        }

        public void Write(ushort addr, byte data)
        {
            
        }
    }

    class NRomCHR : IAddressable
    {
        byte[] ROM = new byte[0x2000];
        //No RAM because we don't emulate family BASIC

        public NRomCHR(byte[] RomData)
        {
            Array.Copy(RomData, ROM, 0x2000);
        }
        public (byte, byte) Read(ushort addr)
        {
            return (ROM[addr], 0xff);
        }

        public void Write(ushort addr, byte data)
        {

        }
    }

    public class NRom : BaseMapper
    {
        public NRom(byte[] PRGData, byte[] CHRData, MirrorType mirror)
        {
            PRG = new NRomPRG(PRGData);
            CHR = new NRomCHR(CHRData);
            Nametables = new Nametables(mirror);
        }
    }
}
