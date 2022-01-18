﻿using System;
using System.IO;

namespace NesSharp.Mappers
{
    class UxRomPRG : IAddressable
    {
        byte[,] ROMBanks;
        int bankAmount;
        internal byte currentBank = 0;
        //No RAM because we don't emulate family BASIC

        public UxRomPRG(byte[] RomData)
        {
            bankAmount = RomData.Length / 0x4000;
            
            if(RomData.Length % 0x4000 != 0)
                throw new Exception("Rom size was not a multiple of 0x4000");
            //not sure if possible but voor de zekerheid
            
            ROMBanks = new byte[bankAmount, 0x4000];
            for(int bank = 0; bank < bankAmount; bank++)
            {
                for(int i = 0; i < 0x4000; i++)
                {
                    ROMBanks[bank, i] = RomData[bank* 0x4000 + i];
                }
            }

        }

        public (byte, byte) Read(ushort addr)
        {
            if((addr & 0x4000) == 0)
                return (ROMBanks[currentBank % bankAmount, addr-0x8000], 0xff);
            else
                return (ROMBanks[bankAmount - 1, addr-0xC000], 0xff);

        }

        public void Write(ushort addr, byte data)
        {
            currentBank = (byte) (data & 0b0000_1111);
        }
    }

    class UxRomCHR : IAddressable
    {
        byte[] ROM = new byte[0x2000];
        

        public UxRomCHR(byte[] RomData)
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
    class UxRamCHR : IAddressable
    {
        internal byte[] RAM = new byte[0x2000];
        
        public (byte, byte) Read(ushort addr)
        {
            return (RAM[addr], 0xff);
        }

        public void Write(ushort addr, byte data)
        {
            RAM[addr] = data;
        }
    }

    public class UxRom : BaseMapper
    {
        public UxRom(byte[] PRGData, byte[] CHRData, MirrorType mirror)
        {
            PRG = new UxRomPRG(PRGData);
            if(CHRData.Length == 0)
                CHR = new UxRamCHR();
            else
                CHR = new UxRomCHR(CHRData);
            Nametables = new Nametables(mirror);
        }

        public override void SaveState(BinaryWriter writer){
            base.SaveState(writer);
            writer.Write(((UxRomPRG) PRG).currentBank);
            if (CHR is UxRamCHR ram) {
                foreach (byte b in ram.RAM) writer.Write(b);
            }
        }

        public override void LoadState(BinaryReader reader) {
            base.LoadState(reader);
            ((UxRomPRG) PRG).currentBank = reader.ReadByte();
            if (CHR is UxRamCHR ram) {
                for (int i = 0; i < ram.RAM.Length; i++) ram.RAM[i] = reader.ReadByte();
            }
        }
    }
}
