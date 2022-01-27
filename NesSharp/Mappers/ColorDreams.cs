using System;

namespace NesSharp.Mappers
{
    class ColorDreamsPRG : IAddressable
    {
        byte[,] ROMBanks;
        int currentBank = 0;
        private ColorDreamsCHR chr;
        
        //No RAM because we don't emulate family BASIC

        public ColorDreamsPRG(byte[] RomData, ColorDreamsCHR chr)
        {
            int amountBanks = RomData.Length / 0x8000;
            ROMBanks = new byte[amountBanks,0x8000];
            this.chr = chr;
            for(int bank = 0; bank < amountBanks; bank++)
            {
                for(int i = 0; i < 0x8000; i++)
                {
                    ROMBanks[bank, i] = RomData[bank* 0x8000 + i];
                }
            }

        }

        public (byte, byte) Read(ushort addr)
        {
            return (ROMBanks[currentBank, addr-0x8000], 0xff);
        }

        public void Write(ushort addr, byte data)
        {
            currentBank = data & 0b0000_0011;
            chr.currentBank = data >> 4;
        }
    }

    class ColorDreamsCHR : IAddressable
    {
        byte[,] ROMBanks = new byte[16,0x2000];
        internal int currentBank = 0;


        public ColorDreamsCHR(byte[] RomData)
        {
            int amountBanks = RomData.Length / 0x2000;
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

    public class ColorDreams : BaseMapper
    {
        public ColorDreams(byte[] PRGData, byte[] CHRData, MirrorType mirror)
        {
            CHR = new ColorDreamsCHR(CHRData);
            PRG = new ColorDreamsPRG(PRGData, (ColorDreamsCHR)CHR);
            
            Nametables = new Nametables(mirror);
        }
    }
}