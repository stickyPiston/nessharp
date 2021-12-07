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

        private byte paletteLatch1;
        private byte paletteLatch2;

        private byte PaletteShift1;
        private byte PaletteShift2;

        private bool ODDFRAME;
        private uint pixel;
        private uint scanline;

        private (byte, byte)[] spritePatternShiftRegs = new (byte, byte)[8];
        private byte[] spriteAttributeLatches = new byte[8];
        private byte[] spriteXCounters = new byte[8];

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


        private byte tempSpriteByte;
        
        
        
        private byte oamAddr = 0;
        private byte secOamIndex = 0;
        private int copySpriteDataCounter;

        public int FrameCycleCount()
        {
            if ((mask.ShowBackground || mask.ShowSprites) && ODDFRAME) {
                return 262 * 341 - 1;
            } else {
                return 262 * 341;
            }
        }

        public void Cycle()
        {
            if (mask.ShowBackground)
            {
                if (scanline <= 239)
                {
                    // ShiftRegs();

                    if (pixel == 0)
                    {
                    }
                    else if (pixel >= 1 && pixel <= 256)
                    {
                        int colorIndex = (((PatternTableShift1 << x) & 0x8000) >> 15) |
                                         (((PatternTableShift2 << x) & 0x8000) >> 14);
                        int paletteIndex = (((PaletteShift1 << x) & 0x80) >> 7) | (((PaletteShift2 << x) & 0x80) >> 6);

                        // currentFrame.SetPixel(pixel-1, scanline, new Color((byte)pixel, (byte)scanline, 1));
                        currentFrame.SetPixel(pixel - 1, scanline,
                            colorIndex == 0
                                ? bus.Palettes.background
                                : bus.Palettes.Backgrounds[paletteIndex].Colors[colorIndex - 1]);
                        ShiftRegs();
                        DoBackgroundFetches();
                    }
                    else if (pixel == 257)
                    {
                        // v = (ushort) ((v & ~0x081f) | (t & 0x081f));
                        v = (ushort) ((v & ~0x041f) | (t & 0x041f));
                    }
                    else if (pixel >= 321 && pixel <= 336)
                    {
                        ShiftRegs();
                        DoBackgroundFetches();
                    }
                    //throw new System.NotImplementedException();
                }
                else if (scanline == 261)
                {
                    if (pixel == 0)
                    {
                    }
                    else if (pixel >= 1 && pixel <= 256)
                    {
                        // TODO
                        ShiftRegs();
                        DoBackgroundFetches();
                    }
                    else if (pixel == 257)
                    {
                        //hori(v) = hori(t)
                        //Also probably wrong
                        // v = (ushort) ((v & ~0x081f) | (t & 0x081f));
                        v = (ushort) ((v & ~0x041f) | (t & 0x041f));
                    }
                    else if (pixel >= 280 && pixel <= 304)
                    {
                        //Also probably wrong
                        // v = (ushort) ((v & ~0x77e0) | (t & 0x77e0));
                        v = (ushort) ((v & ~0x7be0) | (t & 0x7be0));

                        // v = t;
                    }
                    else if (pixel >= 321 && pixel <= 336)
                    {
                        ShiftRegs();
                        DoBackgroundFetches();
                    }
                }
            }
            
            if (mask.ShowSprites)
            {
                if (scanline <= 239)
                {
                    if (pixel == 0)
                    {
                    }
                    else if (pixel <= 64)
                    {
                        if (pixel % 2 == 1)
                        {
                            tempSpriteByte = 0xff;
                        }
                        else
                        {
                            secondaryOam.Write((ushort) (pixel / 2 - 1), tempSpriteByte);
                        }
                    }
                    else if (pixel <= 256)
                    {
                        if (pixel % 2 == 1)
                        {
                            tempSpriteByte = oam.Read(oamAddr);
                            return;
                        }

                        if (copySpriteDataCounter > 3)
                        {
                            
                        }
                        
                        bool inRange = (scanline - tempSpriteByte) < (control.SpriteSize == SpriteSize._8x8 ? 8 : 16);

                        if (inRange && secOamIndex != 8*4 && oamAddr != 0)
                        {
                            copySpriteDataCounter = 3;
                        }

                    }
                    
                }
            }

            IncrementPixel();
        }

        private void DoBackgroundFetches()
        {
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
                    tempBackgroundByte =
                        bus.Read((ushort) (control.BackgroundPatterntableAddress + nametableByte + (v >> 12)));
                    break;
                case 6:
                    patterntableWord = (ushort) (tempBackgroundByte << 8);
                    break;
                case 7:
                    tempBackgroundByte =
                        bus.Read((ushort) (control.BackgroundPatterntableAddress + nametableByte + 8 + (v >> 12)));
                    break;
                case 0:
                    patterntableWord |= tempBackgroundByte;
                    paletteLatch1 = (byte) ((attrtableByte >> (
                        ((v & 0x40) >> 6) |
                        ((v & 0b10) << 0)
                    )) & 1);
                    paletteLatch2 = (byte) ((attrtableByte >> (
                        (((v & 0x40) >> 6) |
                         ((v & 0b10) << 0)) + 1
                    )) & 1);
                    // paletteLatch1 = (byte) ((attrtableByte >> (((v & 0x4000) >> 12) | ((x & 0b100)))) & 2);
                    // paletteLatch2 = (byte) (((attrtableByte >> (((v & 0x4000) >> 12) | ((x & 0b100)))) >> 2) & 1);
                    PatternTableShift1 |= (ushort) (patterntableWord & 0x00ff);
                    PatternTableShift2 |= (ushort) ((patterntableWord >> 8) & 0x00ff);
                    if (pixel == 256)
                        YIncrement();
                    else
                        XIncrement();
                    break;
            }
        }

        private void ShiftRegs()
        {
            PatternTableShift1 = (ushort) (PatternTableShift1 << 1);
            PatternTableShift2 = (ushort) (PatternTableShift2 << 1);

            PaletteShift1 = (byte) (PaletteShift1 << 1);
            PaletteShift2 = (byte) (PaletteShift2 << 1);

            PaletteShift1 |= (byte) (paletteLatch1 & 1);
            PaletteShift2 |= (byte) (paletteLatch2 & 1);
        }

        private void IncrementPixel()
        {
            pixel = (pixel + 1) % 341;
            if (pixel == 0)
            {
                scanline = (scanline + 1) % 262;

                if (scanline == 0)
                {
                    ODDFRAME = !ODDFRAME;
                }
            }

            if (scanline == 241 && pixel == 1)
            {
                status.VblankStarted = true;
                if (frameBuffer != null) frameBuffer.Update(currentFrame);
            }

            if (scanline == 261 && pixel == 1)
            {
                status.VblankStarted = false;
                status.Sprite0Hit = false;
                status.SpriteOverflow = false;
            }

            if (mask.ShowBackground || mask.ShowSprites)
            {
                if (scanline == 261 && pixel == 340 && ODDFRAME)
                {
                    pixel = 0;
                    scanline = 0;
                    ODDFRAME = !ODDFRAME;
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

                v = (ushort) ((v & 0xfc1f) | (y << 5));
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
                w = false;
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
