using System;
using System.Collections.Generic;
using System.Text;

namespace NesSharp.Mappers
{
    class NRom : BaseMapper
    {
        public NRom()
        {
            this.prgROM = new byte[0x8000];
            this.prgRAM = new byte[0x2000];
            this.chrROM = new byte[0x2000];
        }

        public override byte ReadChrROM(ushort address)
        {
            return chrROM[address];
        }

        public override byte ReadPrgRAM(ushort address)
        {
            return prgRAM[address];
        }

        public override byte ReadPrgROM(ushort address)
        {
            return prgROM[address];
        }

        public override void WriteChrROM(ushort address, byte data)
        {
            
        }

        public override void WritePrgRAM(ushort address, byte data)
        {
            prgRAM[address] = data;
        }

        public override void WritePrgROM(ushort address, byte data)
        {
            
        }
    }
}
