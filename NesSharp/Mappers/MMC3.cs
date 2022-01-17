using System;
using System.IO;

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
        internal byte[] ROM;
        private bool[] D;
        private byte[] R;
        private MMC3 mapper;
        internal bool RAM;

        public MMC3CHR(MMC3 mapper, byte[] RomData, byte[] R, bool[] D)
        {
            this.ROM = RomData;
            this.R = R;
            this.D = D;
            this.mapper = mapper;
            this.RAM = RomData.Length == 0;
            if (this.RAM) ROM = new byte[0x2000];
        }

        public (byte, byte) Read(ushort addr)
        {
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
            if (!RAM) return;

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

            ROM[((addr & 0x3FF) + offset) & (ROM.Length - 1)] = data;
        }
    }
    
    public class MMC3 : BaseMapper
    {
        internal byte IRQLatch;
        internal byte IRQCounter;
        internal bool ResetCounterNext;
        internal bool IRQEnable = false;
        private bool[] D;
        private byte[] R;
        
        private bool prevA12Set = false;
        public MMC3(byte[] PRGData, byte[] CHRData, MirrorType mirror)
        {
            Nametables = new Nametables(mirror);
            D = new bool[2];
            R = new byte[8];

            PRG = new MMC3PRG(this, PRGData, R, D, Nametables);
            CHR = new MMC3CHR(this, CHRData, R, D);
            PRGRAM = new SaveRAM();
        }

        public override void SaveState(BinaryWriter writer){
            base.SaveState(writer);

            writer.Write(IRQLatch);
            writer.Write(IRQCounter);
            writer.Write(ResetCounterNext);
            writer.Write(IRQEnable);
            writer.Write(IRQ);
            writer.Write(prevA12Set);

            writer.Write(D[0]);
            writer.Write(D[1]);
            writer.Write(R[0]);
            writer.Write(R[1]);
            writer.Write(R[2]);
            writer.Write(R[3]);
            writer.Write(R[4]);
            writer.Write(R[5]);
            writer.Write(R[6]);
            writer.Write(R[7]);

            if (((MMC3CHR) CHR).RAM) {
                foreach (byte b in ((MMC3CHR) CHR).ROM) writer.Write(b);
            }

            foreach (byte b in ((SaveRAM) PRGRAM).RAM) writer.Write(b);
        }

        public override void LoadState(BinaryReader reader) {
            base.LoadState(reader);

            IRQLatch = reader.ReadByte();
            IRQCounter = reader.ReadByte();
            ResetCounterNext = reader.ReadBoolean();
            IRQEnable = reader.ReadBoolean();
            IRQ = reader.ReadBoolean();
            prevA12Set = reader.ReadBoolean();

            D[0] = reader.ReadBoolean();
            D[1] = reader.ReadBoolean();
            R[0] = reader.ReadByte();
            R[1] = reader.ReadByte();
            R[2] = reader.ReadByte();
            R[3] = reader.ReadByte();
            R[4] = reader.ReadByte();
            R[5] = reader.ReadByte();
            R[6] = reader.ReadByte();
            R[7] = reader.ReadByte();

            if (((MMC3CHR) CHR).RAM) {
                for (int i = 0; i < 8 * 1024; i++) ((MMC3CHR) CHR).ROM[i] = reader.ReadByte();
            }

            for (int i = 0; i < 8 * 1024; i++) ((SaveRAM) PRGRAM).RAM[i] = reader.ReadByte();
        }

        public override void NotifyVramAddrChange(ushort v)
        {
            bool newA12Set = (v & 0x1000) == 0x1000;
            
            
            
            if (newA12Set && !prevA12Set)
            {
                bool isZero = IRQCounter == 0;
                
                if (isZero || ResetCounterNext)
                {
                    IRQCounter = IRQLatch;
                    ResetCounterNext = false;
                }
                else
                {
                    IRQCounter--;
                }
                
                if (IRQCounter == 0 && IRQEnable)
                {
                    IRQ = true;
                }
            }

            prevA12Set = newA12Set;
        }
    }
}
