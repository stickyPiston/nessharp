using System;
using SFML.Graphics;

namespace NesSharp.PPU
{
    public class PPUPalettes
    {
        public byte background;
        public Palette[] Backgrounds = new Palette[4];
        public Palette[] Sprites = new Palette[4];

        public PPUPalettes()
        {
            for (int i = 0; i < 4; i++)
            {
                Backgrounds[i] = new Palette();
                Sprites[i] = new Palette();
            }
        }
        
        public byte Read(ushort addr)
        {
            addr = (ushort)(addr - 0x3f00);
            int palNumber = (addr >> 2) & 0b11;
            ushort palSubAddr = (ushort)((addr-1) & 0b11);
            if (addr == 0 || addr == 0x10)
            {
                return background;
            }
            else if ((addr & 0x10) == 0)
            {
                return Backgrounds[palNumber].Read((ushort)(palSubAddr & 0xe7));
            }
            else
            {
                return Sprites[palNumber].Read(palSubAddr);
            }
        }

        public void Write(ushort addr, byte data)
        {
            addr = (ushort)(addr - 0x3f00);
            int palNumber = (addr >> 2) & 0b11;
            ushort palSubAddr = (ushort)((addr-1) & 0b11);
            if (addr == 0 || addr == 0x10)
            {
                background = data;
                Console.WriteLine($"background = ");
            }
            else if ((addr & 0x10) == 0)
            {
                Backgrounds[palNumber].Write((ushort)(palSubAddr & 0xe7), data);
            }
            else
            {
                Sprites[palNumber].Write(palSubAddr, data);
            }
        }
    }
}
