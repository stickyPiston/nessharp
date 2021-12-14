namespace NesSharp.PPU
{
    public class PPUMemoryBus : IAddressable
    {
        public IAddressable Patterntables;
        public IAddressable Nametables;
        public PPUPalettes Palettes;


        public byte Read(ushort addr)
        {
            if (addr < 0x2000)
            {
                return Patterntables.Read(addr);
            }
            if (addr < 0x3f00)
            {
                return Nametables.Read((ushort)((addr - 0x2000) % 0x1000));
            }
            if (addr <= 0x3fff)
            {
                return Palettes.Read((ushort)(0x3f00 + ((addr - 0x3f00) % 0xe0)));
            }
            throw new System.NotImplementedException();
        }

        public void Write(ushort addr, byte data)
        {
            addr = (ushort) (addr % 0x4000);
            if (addr < 0x2000)
            {
                Patterntables.Write(addr, data);
            }
            else if (addr < 0x3eff)
            {
                Nametables.Write((ushort) ((addr - 0x2000) % 0x1000), data);
            }
            else if (addr >= 0x3f00 && addr <= 0x3fff)
            {
                Palettes.Write((ushort)(0x3f00 + ((addr - 0x3f00) % 0xe0)), data);
            }
            else
                throw new System.NotImplementedException($"Can't write to {addr:x4}");
        }
    }
}
