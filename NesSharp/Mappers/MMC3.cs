using System;

namespace NesSharp.Mappers
{
    class MMC3PRG : IAddressable
    {
        private byte[] ROM;
        private bool[] D;
        private byte[] R;
        private int bank;
        
        private MMC3 mapper;


        private Nametables nametables;

        public MMC3PRG(MMC3 mapper, byte[] RomData, byte[] R, bool[] D, Nametables nametables)
        {
            this.ROM = RomData;
            this.R = R;
            this.D = D;
            this.nametables = nametables;
            this.mapper = mapper;
        }
        
        public (byte, byte) Read(ushort addr)
        {
            int offset;

            switch ((addr & 0x6000) >> 13) {
                case 0:
                    offset = D[0] ? ROM.Length - 16 * 1024 : (R[6] & 0x3F) * 8 * 1024;
                    break;
                case 1:
                    offset = (R[7] & 0x3F) * 8 * 1024;
                    break;
                case 2:
                    offset = !D[0] ? ROM.Length - 16 * 1024 : (R[6] & 0x3F) * 8 * 1024;
                    break;
                case 3:
                    offset = ROM.Length - 8 * 1024;
                    break;
                default:
                    throw new Exception();
            }

            return (ROM[((addr & 0x1FFF) + offset) & (ROM.Length - 1)], 0xFF);
        }

        public void Write(ushort addr, byte data)
        {
            switch ((addr & 0x6000) >> 13) {
                case 0:
                    if ((addr & 1) == 0) {
                        bank = data & 0b111;
                        D[0] = ((data >> 6) & 1) == 1;
                        D[1] = (data >> 7) == 1;
                    } else {
                        R[bank] = data;
                    }
                    break;
                case 1:
                    if ((addr & 1) == 0) {
                        if (nametables.mirror == MirrorType.fourScreen) break;
                        nametables.mirror = (data & 1) == 1 ? MirrorType.horizontal : MirrorType.vertical;
                    } else {
                        // TODO: PRG RAM
                    }
                    break;
                case 2:
                    if ((addr & 1) == 0)
                    {
                        mapper.IRQLatch = data;
                    } 
                    else
                    {
                        mapper.ResetCounterNext = true;
                    }
                    break;
                case 3:
                    if ((addr & 1) == 0)
                    {
                        mapper.IRQEnable = false;
                        mapper.IRQ = false;
                    }
                    else
                    {
                        mapper.IRQEnable = true;
                    }
                    break;
                default:
                    throw new Exception();
            }
        }
    }

    class MMC3CHR : IAddressable
    {
        private byte[] ROM;
        private bool[] D;
        private byte[] R;
        private MMC3 mapper;
        
        

        private bool prevA12Set = false;

        public MMC3CHR(MMC3 mapper, byte[] RomData, byte[] R, bool[] D)
        {
            this.ROM = RomData;
            this.R = R;
            this.D = D;
            this.mapper = mapper;

        }

        public (byte, byte) Read(ushort addr)
        {
            bool newA12Set = (addr & 0x1000) != 0;

            if (newA12Set && !prevA12Set)
            {
                bool isZero = mapper.IRQCounter == 0;
                if (isZero && mapper.IRQEnable)
                {
                    mapper.IRQ = true;
                }
                
                if (isZero || mapper.ResetCounterNext)
                {
                    mapper.IRQCounter = mapper.IRQLatch;
                    mapper.ResetCounterNext = false;
                }
                else
                {
                    mapper.IRQCounter--;
                }
            }
            
            prevA12Set = newA12Set;

            
            int offset;

            switch ((addr & 0x1C00) >> 10) {
                case 0:
                    offset = (D[1] ? R[2] : (R[0] & 0xFE)) * 1024;
                    break;
                case 1:
                    offset = (D[1] ? R[3] : (R[0] | 0x01)) * 1024;
                    break;
                case 2:
                    offset = (D[1] ? R[4] : (R[1] & 0xFE)) * 1024;
                    break;
                case 3:
                    offset = (D[1] ? R[5] : (R[1] | 0x01)) * 1024;
                    break;
                case 4:
                    offset = (!D[1] ? R[2] : (R[0] & 0xFE)) * 1024;
                    break;
                case 5:
                    offset = (!D[1] ? R[3] : (R[0] | 0x01)) * 1024;
                    break;
                case 6:
                    offset = (!D[1] ? R[4] : (R[1] & 0xFE)) * 1024;
                    break;
                case 7:
                    offset = (!D[1] ? R[5] : (R[1] | 0x01)) * 1024;
                    break;
                default:
                    throw new Exception();
            }

            return (ROM[((addr & 0x3FF) + offset) & (ROM.Length - 1)], 0xFF);
        }

        public void Write(ushort addr, byte data)
        {
            
        }
    }

    class MMC3CHRRAM : IAddressable
    {
        private byte[] RAM = new byte[0x2000];
        private MMC3 mapper;
        
        

        private bool prevA12Set = false;

        public MMC3CHRRAM(MMC3 mapper)
        {
            this.mapper = mapper;

        }

        public (byte, byte) Read(ushort addr)
        {
            bool newA12Set = (addr & 0x1000) != 0;
            
            if (newA12Set && !prevA12Set)
            {
                bool isZero = mapper.IRQCounter == 0;
                if (isZero && mapper.IRQEnable)
                {
                    mapper.IRQ = true;
                }
                
                if (isZero || mapper.ResetCounterNext)
                {
                    mapper.IRQCounter = mapper.IRQLatch;
                    mapper.ResetCounterNext = false;
                }
                else
                {
                    mapper.IRQCounter--;
                }
            }

            prevA12Set = newA12Set;

            return (RAM[addr], 0xff);
        }

        public void Write(ushort addr, byte data)
        {
            RAM[addr] = data;
        }
    }
    
    public class MMC3 : BaseMapper
    {
        internal int IRQLatch;
        internal int IRQCounter;
        internal bool ResetCounterNext;
        internal bool IRQEnable = false;
        
        public MMC3(byte[] PRGData, byte[] CHRData, MirrorType mirror)
        {
            Nametables = new Nametables(mirror);
            bool[] D = new bool[2];
            byte[] R = new byte[8];

            PRG = new MMC3PRG(this, PRGData, R, D, Nametables);
            if (CHRData.Length == 0)
                CHR = new MMC3CHRRAM(this);
            else
                CHR = new MMC3CHR(this, CHRData, R, D);
            PRGRAM = new SaveRAM();
        }
    }
}
