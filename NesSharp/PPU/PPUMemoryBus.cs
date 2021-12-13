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
            throw new System.NotImplementedException();
        }

        public void Write(ushort addr, byte data)
        {
            if (addr >= 0x3f00 && addr <= 0x3f1f)
            {
                Palettes.Write(addr, data);
            }
            else if (addr >= 0x2000 && addr < 0x3000)
            {
                Nametables.Write((ushort)(addr - 0x2000), data);
            }
            else
                throw new System.NotImplementedException($"Can't write to {addr:x4}");
        }
    }
}
