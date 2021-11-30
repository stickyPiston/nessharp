using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace NesSharp.PPU
{
    public class PPU : IClockable, IAddressable
    {
        private Image currentFrame;
        private Texture frameBuffer;

        private OAM oam;
        private SecondaryOAM secondaryOam;
        public PPUMemoryBus bus;

        private PPUCTRL control;
        private PPUSTATUS status;
        private PPUMASK mask;

        private ushort v;
        private ushort t;
        private byte x;
        private bool w;

        private ushort PatternTableShift1;
        private ushort PatternTableShift2;

        private byte PaletteShift1;
        private byte PaletteShift2;

        private bool ODDFRAME;
        private uint pixel;
        private uint scanline;

        public PPU(Texture frameBuffer)
        {
            control = new PPUCTRL();
            mask = new PPUMASK();
            status = new PPUSTATUS
            {
                VblankStarted = true,
                SpriteOverflow = true
            };
            ODDFRAME = false;

            bus = new PPUMemoryBus();

            this.currentFrame = new Image(256, 240);
            this.frameBuffer = frameBuffer;
        }

        private byte tempBackgroundByte;
        private byte nametableByte;
        private byte attrtableByte;
        private ushort patterntableWord;
        public void Cycle()
        {
            if (mask.ShowBackground)
            {
                if (scanline >= 0 && scanline <= 239)
                {
                    if (pixel == 0)
                    {
                    }
                    else if (pixel >= 1 && pixel <= 256)
                    {
                        int colorIndex = ((PatternTableShift1 >> (8 + x)) & 1) | ((PatternTableShift2 >> (7 + x)) & 2);
                        int PaletteIndex = ((PaletteShift1 >> x) & 1) | ((PaletteShift2 >> (x - 1)) & 2);
                        // currentFrame.SetPixel(pixel-1, scanline, new Color((byte)pixel, (byte)scanline, 1));
                        currentFrame.SetPixel(pixel-1, scanline, bus.Palettes.Backgrounds[PaletteIndex].Colors[colorIndex * 3 + 5+ PaletteIndex]);
                        ShiftRegs();
                        switch (pixel % 8)
                        {
                            case 1:
                                tempBackgroundByte = getTile();
                                break;
                            case 2:
                                nametableByte = tempBackgroundByte;
                                break;
                            case 3:
                                tempBackgroundByte = getTileAttr();
                                break;
                            case 4:
                                attrtableByte = tempBackgroundByte;
                                break;
                            case 5:
                                tempBackgroundByte = bus.Read((ushort)(control.BackgroundPatterntableAddress + nametableByte));
                                break;
                            case 6:
                                patterntableWord = (ushort)(tempBackgroundByte << 8);
                                break;
                            case 7:
                                tempBackgroundByte = bus.Read((ushort)(control.BackgroundPatterntableAddress + nametableByte + 8));
                                break;
                            case 0:
                                patterntableWord |= tempBackgroundByte;
                                // PaletteShift1 = attrtableByte;
                                // PaletteShift2 = attrtableByte;
                                PatternTableShift1 |= (ushort) (patterntableWord & 0x00ff);
                                PatternTableShift2 |= (ushort) ((patterntableWord >> 8) & 0x00ff);
                                if (pixel == 256)
                                    YIncrement();
                                else 
                                    XIncrement();
                                break;
                            
                        }
                    }
                    else if (pixel == 257)
                    {
                        v = (ushort) ((v & ~0x041f) | (t & 0x041f));
                    }
                    //throw new System.NotImplementedException();
                }
            }

            IncrementPixel();
        }

        private void ShiftRegs()
        {
            PatternTableShift1 <<= 1;
            PatternTableShift2 <<= 1;
            PaletteShift1 <<= 1;
            PaletteShift2 <<= 1;
        }

        private void IncrementPixel()
        {
            pixel = (pixel + 1) % 262;
            if (pixel == 0)
            {
                scanline = (scanline + 1) % 341;

                if (scanline == 0)
                {
                    ODDFRAME = !ODDFRAME;
                    frameBuffer.Update(currentFrame);
                }
            }

            if (scanline == 241 && pixel == 1)
            {
                status.VblankStarted = true;
                frameBuffer.Update(currentFrame);
            }

            if (scanline == 261 && pixel == 1)
            {
                status.VblankStarted = false;
                status.Sprite0Hit = false;
                status.SpriteOverflow = false;
            }

            if (mask.ShowBackground || mask.ShowSprites)
            {
                if (scanline == 0 && pixel == 0 && ODDFRAME)
                {
                    pixel++;
                }
            }
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }

        //source: https://wiki.nesdev.org/w/index.php?title=PPU_scrolling
        void XIncrement()
        {
            if ((v & 0x001f) == 31)
            {
                v &= 0xffe0;
                v ^= 0x0400;
            }
            else
            {
                v += 1;
            }
        }

        void YIncrement()
        {
            if ((v & 0x7000) != 0x7000)
            {
                v += 0x1000;
            }
            else
            {
                v &= 0x8fff;
                int y = (v & 0x03e0) >> 5;
                if (y == 29)
                {
                    y = 0;
                    v ^= 0x0800;
                }
                else if (y == 31)
                {
                    y = 0;
                }
                else
                {
                    y += 1;
                }

                v = (ushort) ((v & 0xfb1f) | (y << 5));
            }
        }

        byte getTile()
        {
            ushort addr = (ushort) (0x2000 | (v & 0x0fff));
            return bus.Read(addr);
        }

        byte getTileAttr()
        {
            ushort addr = (ushort) (0x23c0 | (v & 0x0c00) | ((v >> 4) & 0x38) | ((v >> 2) & 0x07));
            return bus.Read(addr);
        }

        public byte Read(ushort addr)
        {
            if (addr == 0x2002)
            {
                byte val = status.ToByte();
                status.VblankStarted = false;
                return val;
            }

            throw new NotImplementedException();
        }

        public void Write(ushort addr, byte data)
        {
            switch (addr)
            {
                case 0x2000:
                    t = (ushort) ((t & 0x73ff) | ((data & 0x03) << 10));
                    control.FromByte(data);
                    break;
                case 0x2001:
                    mask.FromByte(data);
                    break;
                case 0x2002:
                    break;
                case 0x2003:
                    throw new NotImplementedException();
                case 0x2004:
                    throw new NotImplementedException();
                case 0x2005:
                    if (!w)
                    {
                        x = (byte) (data & 0x07);
                        t = (ushort) ((t & ~0x801f) | (data >> 3));
                    }
                    else
                    {
                        t = (ushort) ((t & ~0xf3e0) | ((data & 0xf8) << 5) | ((data & 0x07) << 12));
                    }

                    w = !w;
                    break;
                case 0x2006:
                    if (!w)
                    {
                        t = (ushort) ((t & 0x00ff) | ((data & 0x3f) << 8));
                    }
                    else
                    {
                        t = (ushort) ((t & 0xff00) | data);
                        v = t;
                    }

                    w = !w;
                    break;
                default:
                    throw new NotImplementedException($"Writing to address 0x{addr:X4} is not implemented");
            }

            if (addr == 0x2001)
            {
                mask.FromByte(data);
                return;
            }

            throw new NotImplementedException();
        }
    }
}

enum SpriteSize
{
    _8x8 = 0,
    _8x16 = 1
}

struct PPUCTRL
{
    public ushort BaseNametableAddress;
    public ushort VramAddrInc;
    public ushort SpritePatterntableAddress8x8;
    public ushort BackgroundPatterntableAddress;
    public SpriteSize SpriteSize;
    public bool GenNMI_VBL;

    public byte ToByte()
    {
        throw new NotImplementedException();
    }

    public void FromByte(byte data)
    {
        throw new NotImplementedException();
    }
}

struct PPUMASK
{
    public bool greyscale;
    public bool BackgroundOnLeft8;
    public bool SpritesOnLeft8;
    public bool ShowBackground;
    public bool ShowSprites;
    public bool EmphasizeRed;
    public bool EmphasizeGreen;
    public bool EmphasizeBlue;


    public byte ToByte()
    {
        throw new NotImplementedException();
    }

    public void FromByte(byte data)
    {
        greyscale = (data & 0x01) != 0;
        BackgroundOnLeft8 = (data & 0x02) != 0;
        SpritesOnLeft8 = (data & 0x04) != 0;
        ShowBackground = (data & 0x08) != 0;
        ShowSprites = (data & 0x10) != 0;
        EmphasizeRed = (data & 0x20) != 0;
        EmphasizeGreen = (data & 0x40) != 0;
        EmphasizeBlue = (data & 0x80) != 0;
    }
}

struct PPUSTATUS
{
    public byte lastRegWrite;
    public bool SpriteOverflow;
    public bool Sprite0Hit;
    public bool VblankStarted;

    public byte ToByte()
    {
        byte val = (byte) (lastRegWrite & 0x1f);

        if (SpriteOverflow)
            val |= 0x20;
        if (Sprite0Hit)
            val |= 0x40;
        if (VblankStarted)
            val |= 0x80;

        return val;
    }

    public void FromByte(byte data)
    {
        throw new NotImplementedException();
    }
}