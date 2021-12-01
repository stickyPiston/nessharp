using System;
using System.Collections.Generic;
using System.Text;

namespace NesSharp
{
    abstract class BaseMapper
    {
        private byte[] prgROM;
        private byte[] prgRAM = new byte[0x200];
        private byte[] chrROM;
    }

    class NRom : BaseMapper
    {

    }

    class UxRom : BaseMapper
    {

    }
    
    class MMC1 : BaseMapper
    {

    }

    class MMC3 : BaseMapper
    {

    }
}
