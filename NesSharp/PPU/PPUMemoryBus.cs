using NesSharp.Mappers;

namespace NesSharp.PPU
{
    public class PPUMemoryBus
    {
        public IAddressable Patterntables;
        public IAddressable Nametables;
        public PPUPalettes Palettes;
        private PPU ppu;

        private byte buffer = 0;

        public PPUMemoryBus(PPU ppu)
        {
            this.ppu = ppu;
        }

        public byte Read(ushort addr)
        {
            ppu.MainBus.mapper.NotifyVramAddrChange(addr);
            addr = (ushort)(addr % 0x4000);
            if (addr < 0x2000)
            {
                return Patterntables.Read(addr).Item1;
            }
            if (addr < 0x3f00)
            {
                return Nametables.Read((ushort)((addr - 0x2000) % 0x1000)).Item1;
            }
            if (addr <= 0x3fff)
            {
                return Palettes.Read((ushort)(0x3f00 + ((addr - 0x3f00) % 0xe0)));
            }
            throw new System.NotImplementedException();
        }

        public byte BufferedRead(ushort addr)
        {
            ppu.MainBus.mapper.NotifyVramAddrChange(addr);

            if (addr < 0x2000)
            {
                byte b = buffer;
                buffer = Patterntables.Read(addr).Item1;
                return b;
            }
            if (addr < 0x3f00)
            {
                byte b = buffer;
                buffer = Nametables.Read((ushort)((addr - 0x2000) % 0x1000)).Item1;
                return b;
            }
            if (addr <= 0x3fff)
            {
                buffer = Nametables.Read((ushort)((addr - 0x2000) % 0x1000)).Item1;
                return Palettes.Read((ushort)(0x3f00 + ((addr - 0x3f00) % 0xe0)));
            }
            throw new System.NotImplementedException();
        }

        public void Write(ushort addr, byte data)
        {
            ppu.MainBus.mapper.NotifyVramAddrChange(addr);


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
