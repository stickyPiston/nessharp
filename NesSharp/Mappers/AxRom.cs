using System;
using System.IO;

namespace NesSharp.Mappers
{
    class AxRomPRG : IAddressable {

        private byte[] ROM;
        internal byte bank;

        private Nametables nametables;

        public AxRomPRG(byte[] RomData, Nametables nametables)
        {
            this.ROM = RomData;
            this.nametables = nametables;
        }
        
        public (byte, byte) Read(ushort addr)
        {
            return (ROM[addr - 0x8000 + bank * 0x8000], 0xFF);
        }

        public void Write(ushort addr, byte data)
        {
            bank = (byte) (data & 0b111);
            nametables.mirror = (MirrorType) ((data >> 4) & 1);
        }

    }

    public class AxRom : BaseMapper
    {
        public AxRom(byte[] PRGData)
        {
            Nametables = new Nametables(MirrorType.lower);
            PRG = new AxRomPRG(PRGData, Nametables);
            CHR = new UxRamCHR(); // exactly the same
        }

        public override void SaveState(BinaryWriter writer){
            base.SaveState(writer);
            writer.Write(((AxRomPRG) PRG).bank);
        }

        public override void LoadState(BinaryReader reader) {
            base.LoadState(reader);
            ((AxRomPRG) PRG).bank = reader.ReadByte();
        }
    }

}
