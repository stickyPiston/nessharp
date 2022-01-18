using System;
using System.IO;

namespace NesSharp.Mappers
{
    public class MMC1PRG : IAddressable
    {
        private byte[] ROM;

        internal byte shift = 0b10000;
        internal byte PRGbank;
        internal byte PRGmode = 3;
        internal bool ignore;

        private Nametables nametables;
        private MMC1CHR CHR;

        public MMC1PRG(byte[] RomData, MMC1CHR CHR, Nametables nametables)
        {
            this.ROM = RomData;
            this.CHR = CHR;
            this.nametables = nametables;
        }
        
        public (byte, byte) Read(ushort addr)
        {
            int offset;

            ignore = false;

            if ((addr & 0x4000) == 0) {
                switch (PRGmode) {
                    case 0:
                    case 1:
                        offset = (PRGbank & 0b11110) * 16 * 1024;
                        break;
                    case 2:
                        offset = 0;
                        break;
                    case 3:
                        offset = PRGbank * 16 * 1024;
                        break;
                    default:
                        throw new Exception();
                }
            } else {
                switch (PRGmode) {
                    case 0:
                    case 1:
                        offset = (PRGbank | 1) * 16 * 1024;
                        break;
                    case 2:
                        offset = PRGbank * 16 * 1024;
                        break;
                    case 3:
                        offset = ROM.Length - 16 * 1024;
                        break;
                    default:
                        throw new Exception();
                }
            }

            return (ROM[((addr & 0x3FFF) + offset) & (ROM.Length - 1)], 0xFF);
        }

        public void Write(ushort addr, byte data)
        {
            if (ignore) {
                return;
            }

            ignore = true;

            if (data > 127) {
                shift = 0b10000;
                PRGmode = 3;
                return;
            }

            bool one = (shift & 1) == 1;
            shift = (byte) ((shift >> 1) | ((data & 1) << 4));

            if (!one) return;

            switch ((addr & 0x6000) >> 13) {
                case 0:
                    CHR.CHRmode = shift > 15;

                    PRGmode = (byte)((shift >> 2) & 3);

                    if (nametables.mirror == MirrorType.fourScreen) break;
                    nametables.mirror = (MirrorType) (shift & 3);
                    break;
                case 1:
                    CHR.CHRbank[0] = shift;
                    break;
                case 2:
                    CHR.CHRbank[1] = shift;
                    break;
                case 3:
                    PRGbank = shift;
                    break;
            }

            shift = 0b10000;
        }
    }

    public class MMC1CHR : IAddressable
    {
        internal byte[] ROM;
        internal byte[] CHRbank = new byte[2];
        internal bool CHRmode;
        internal bool RAM;

        public MMC1CHR(byte[] RomData)
        {
            this.ROM = RomData.Length == 0 ? new byte[8 * 1024] : RomData;
            this.RAM = RomData.Length == 0;
        }

        public (byte, byte) Read(ushort addr)
        {
            int offset;

            if ((addr & 0x1000) == 0) {
                offset = (!CHRmode ? (CHRbank[0] & 0b11110) : CHRbank[0]) * 4 * 1024;
            } else {
                offset = (!CHRmode ? (CHRbank[0] | 1) : CHRbank[1]) * 4 * 1024;
            }

            return (ROM[((addr & 0xFFF) + offset) & (ROM.Length - 1)], 0xFF);
        }

        public void Write(ushort addr, byte data)
        {
            if (RAM) {
                int offset;

                if ((addr & 0x1000) == 0) {
                    offset = (!CHRmode ? (CHRbank[0] & 0b11110) : CHRbank[0]) * 4 * 1024;
                } else {
                    offset = (!CHRmode ? (CHRbank[0] | 1) : CHRbank[1]) * 4 * 1024;
                }

                ROM[((addr & 0xFFF) + offset) & (ROM.Length - 1)] = data;
            }
        }

    }

    public class MMC1 : BaseMapper
    {
        public MMC1(byte[] PRGData, byte[] CHRData, MirrorType mirror, string filename)
        {
            Nametables = new Nametables(mirror);
            CHR = new MMC1CHR(CHRData);
            PRG = new MMC1PRG(PRGData, (MMC1CHR) CHR, Nametables);
            PRGRAM = new SaveRAM(filename);
        }

        public override void SaveState(BinaryWriter writer){
            base.SaveState(writer);

            writer.Write(((MMC1PRG) PRG).shift);
            writer.Write(((MMC1PRG) PRG).PRGbank);
            writer.Write(((MMC1PRG) PRG).PRGmode);
            writer.Write(((MMC1PRG) PRG).ignore);

            writer.Write(((MMC1CHR) CHR).CHRbank[0]);
            writer.Write(((MMC1CHR) CHR).CHRbank[1]);
            writer.Write(((MMC1CHR) CHR).CHRmode);

            if (((MMC1CHR) CHR).RAM) {
                foreach (byte b in ((MMC1CHR) CHR).ROM) writer.Write(b);
            }

            foreach (byte b in ((SaveRAM) PRGRAM).RAM) writer.Write(b);
        }

        public override void LoadState(BinaryReader reader) {
            base.LoadState(reader);

            ((MMC1PRG) PRG).shift = reader.ReadByte();
            ((MMC1PRG) PRG).PRGbank = reader.ReadByte();
            ((MMC1PRG) PRG).PRGmode = reader.ReadByte();
            ((MMC1PRG) PRG).ignore = reader.ReadBoolean();

            ((MMC1CHR) CHR).CHRbank[0] = reader.ReadByte();
            ((MMC1CHR) CHR).CHRbank[1] = reader.ReadByte();
            ((MMC1CHR) CHR).CHRmode = reader.ReadBoolean();

            if (((MMC1CHR) CHR).RAM) {
                for (int i = 0; i < 8 * 1024; i++) ((MMC1CHR) CHR).ROM[i] = reader.ReadByte();
            }

            for (int i = 0; i < 8 * 1024; i++) ((SaveRAM) PRGRAM).RAM[i] = reader.ReadByte();
        }
    }
}
