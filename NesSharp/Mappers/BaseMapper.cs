using System;
using System.Collections.Generic;
using System.Text;

namespace NesSharp.Mappers
{
    abstract class BaseMapper
    {
        protected byte[] prgROM;
        protected byte[] prgRAM;
        protected byte[] chrROM;

        public void SetPRGRom(byte[] PRGDump)
        {
            for (int i = 0; i < PRGDump.Length; i++)
            {
                prgROM[i] = PRGDump[i];
            }
        }

        public void SetCHRRom(byte[] CHRDump)
        {
            for (int i = 0; i < CHRDump.Length; i++)
            {
                chrROM[i] = CHRDump[i];
            }
        }

        public abstract byte ReadPrgROM(ushort address);
        public abstract byte ReadPrgRAM(ushort address);
        public abstract byte ReadChrROM(ushort address);

        public abstract void WritePrgROM(ushort address, byte data);
        public abstract void WritePrgRAM(ushort address, byte data);
        public abstract void WriteChrROM(ushort address, byte data);
    }


}
