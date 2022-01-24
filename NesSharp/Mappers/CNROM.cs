namespace NesSharp.Mappers
{
    using System;

namespace NesSharp.Mappers
{
    class CNROMPRG : IAddressable
    {
        byte[] ROM = new byte[0x8000];
        private CNROMCHR chr;
        
        //No RAM because we don't emulate family BASIC

        public CNROMPRG(byte[] RomData, CNROMCHR chr)
        {
            this.chr = chr;

            if (RomData.Length == 0x4000)
            {
                Array.Copy(RomData, ROM, 0x4000);
                Array.Copy(RomData, 0, ROM, 0x4000, 0x4000);
            }
            else
            {
                Array.Copy(RomData, ROM, 0x8000);
            }
            
            
        }

        public (byte, byte) Read(ushort addr)
        {
            return (ROM[addr-0x8000], 0xff);
        }

        public void Write(ushort addr, byte data)
        {
            chr.currentBank = data % chr.amountBanks;
        }
    }

    class CNROMCHR : IAddressable
    {
        byte[,] ROMBanks;
        internal int currentBank = 0;
        internal int amountBanks;


        public CNROMCHR(byte[] RomData)
        {
            amountBanks = RomData.Length / 0x2000;
            ROMBanks = new byte[amountBanks,0x2000];

            for(int bank = 0; bank < amountBanks; bank++)
            {
                for(int i = 0; i < 0x2000; i++)
                {
                    ROMBanks[bank, i] = RomData[bank* 0x2000 + i];
                }
            }
        }
        public (byte, byte) Read(ushort addr)
        {
            return (ROMBanks[currentBank, addr], 0xff);
        }

        public void Write(ushort addr, byte data)
        {

        }
    }

    public class CNROM : BaseMapper
    {
        public CNROM(byte[] PRGData, byte[] CHRData, MirrorType mirror)
        {
            CHR = new CNROMCHR(CHRData);
            PRG = new CNROMPRG(PRGData, (CNROMCHR)CHR);
            
            Nametables = new Nametables(mirror);
        }
    }
}
}