using System;

namespace NesSharp.Mappers
{
    class MMC3PRG : IAddressable
    {
        private byte[] ROM;
        private bool[] D;
        private byte[] R;
        private int bank;

        private Nametables nametables;

        public MMC3PRG(byte[] RomData, byte[] R, bool[] D, Nametables nametables)
        {
            this.ROM = RomData;
            this.R = R;
            this.D = D;
            this.nametables = nametables;
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
                    // TODO: IRQ
                    if ((addr & 1) == 0) {
                    } else {
                    }
                    break;
                case 3:
                    // TODO: IRQ
                    if ((addr & 1) == 0) {
                    } else {
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

        public MMC3CHR(byte[] RomData, byte[] R, bool[] D)
        {
            this.ROM = RomData;
            this.R = R;
            this.D = D;       
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
            
        }
    }

    public class MMC3 : BaseMapper
    {
        public MMC3(byte[] PRGData, byte[] CHRData, MirrorType mirror)
        {
            Nametables = new Nametables(mirror);
            bool[] D = new bool[2];
            byte[] R = new byte[8];

            PRG = new MMC3PRG(PRGData, R, D, Nametables);
            CHR = new MMC3CHR(CHRData, R, D);
            PRGRAM = new SaveRAM();
        }
    }
}
