using System;
using SFML.Graphics;
using System.IO;

namespace NesSharp.PPU
{
    public class PPU : IClockable, IAddressable
    {
        internal Bus MainBus;

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
        public ushort pixel;
        public ushort scanline;

        private (byte, byte)[] spritePatternShiftRegs = new (byte, byte)[8];
        private SpriteAttribute[] spriteAttributeLatches = new SpriteAttribute[8];
        private byte[] spriteXCounters = new byte[8];
        private bool isRenderingSpriteZero;
        private bool secondaryOamHasSpriteZero;
        private bool rendering;

        public void SaveState(BinaryWriter writer) {
            writer.Write(control.ToByte());
            writer.Write(status.ToByte());
            writer.Write(mask.ToByte());
            writer.Write(v);
            writer.Write(t);
            writer.Write(x);
            writer.Write(w);
            writer.Write(PatternTableShift1);
            writer.Write(PatternTableShift2);
            writer.Write(PaletteShift1);
            writer.Write(PaletteShift2);
            writer.Write(paletteLatch1);
            writer.Write(paletteLatch2);
            writer.Write(ODDFRAME);
            writer.Write(pixel);
            writer.Write(scanline);
            foreach (var bb in spritePatternShiftRegs) {
                writer.Write(bb.Item1);
                writer.Write(bb.Item2);
            }
            foreach (var attrib in spriteAttributeLatches) {
                writer.Write(attrib.ToByte());
            }
            foreach (var counter in spriteXCounters) {
                writer.Write(counter);
            }
            writer.Write(isRenderingSpriteZero);
            writer.Write(secondaryOamHasSpriteZero);
            writer.Write(rendering);
            writer.Write(tempBackgroundByte);
            writer.Write(nametableByte);
            writer.Write(attrtableByte);
            writer.Write(patterntableWord);
            writer.Write(tempSpriteByte);
            writer.Write(secOamAddr);
            writer.Write(secOamFull);
            writer.Write(oamAddrOverflow);
            writer.Write(SpriteIndex);
            writer.Write(preventVbl);
            foreach (var counter in SpriteRenderingCounters) {
                writer.Write(counter);
            }

            // OAM
            for (ushort i = 0; i < 64 * 4; i++) {
                writer.Write(oam.Read(i));
            }
            for (ushort i = 0; i < 8 * 4; i++) {
                writer.Write(secondaryOam.Read(i));
            }

            // Palettes
            writer.Write(bus.Palettes.background);
            for (int i = 0; i < 4 * 4; i++) {
                writer.Write(bus.Palettes.Backgrounds[i / 4].Read((ushort) (i % 4)));
            }
            for (int i = 0; i < 4 * 4; i++) {
                writer.Write(bus.Palettes.Sprites[i / 4].Read((ushort) (i % 4)));
            }
        }

        public void LoadState(BinaryReader reader) {
            control.FromByte(reader.ReadByte());
            status.FromByte(reader.ReadByte());
            mask.FromByte(reader.ReadByte());
            v = reader.ReadUInt16();
            t = reader.ReadUInt16();
            x = reader.ReadByte();
            w = reader.ReadBoolean();
            PatternTableShift1 = reader.ReadUInt16();
            PatternTableShift2 = reader.ReadUInt16();
            PaletteShift1 = reader.ReadByte();
            PaletteShift2 = reader.ReadByte();
            paletteLatch1 = reader.ReadByte();
            paletteLatch2 = reader.ReadByte();
            ODDFRAME = reader.ReadBoolean();
            pixel = reader.ReadUInt16();
            scanline = reader.ReadUInt16();
            for (int i = 0; i < 8; i++) {
                spritePatternShiftRegs[i] = (reader.ReadByte(), reader.ReadByte());
            }
            for (int i = 0; i < 8; i++) {
                spriteAttributeLatches[i].FromByte(reader.ReadByte());
            }
            for (int i = 0; i < 8; i++) {
                spriteXCounters[i] = reader.ReadByte();
            }
            isRenderingSpriteZero = reader.ReadBoolean();
            secondaryOamHasSpriteZero = reader.ReadBoolean();
            rendering = reader.ReadBoolean();
            tempBackgroundByte = reader.ReadByte();
            nametableByte = reader.ReadByte();
            attrtableByte = reader.ReadByte();
            patterntableWord = reader.ReadUInt16();
            tempSpriteByte = reader.ReadByte();
            secOamAddr = reader.ReadByte();
            secOamFull = reader.ReadBoolean();
            oamAddrOverflow = reader.ReadBoolean();
            SpriteIndex = reader.ReadByte();
            preventVbl = reader.ReadBoolean();
            for (int i = 0; i < 8; i++) {
                SpriteRenderingCounters[i] = reader.ReadByte();
            }

            // OAM
            for (ushort i = 0; i < 64 * 4; i++) {
                oam.Write(i, reader.ReadByte());
            }
            for (ushort i = 0; i < 8 * 4; i++) {
                secondaryOam.Write(i, reader.ReadByte());
            }

            // Palettes
            bus.Palettes.background = reader.ReadByte();
            for (int i = 0; i < 4 * 4; i++) {
                bus.Palettes.Backgrounds[i / 4].Write((ushort) (i % 4), reader.ReadByte());
            }
            for (int i = 0; i < 4 * 4; i++) {
                bus.Palettes.Sprites[i / 4].Write((ushort) (i % 4), reader.ReadByte());
            }
        }

        public PPU(Texture frameBuffer, Bus mainBus)
        {
            MainBus = mainBus;
            control = new PPUCTRL();
            mask = new PPUMASK();
            status = new PPUSTATUS
            {
                // VblankStarted = true,
                // SpriteOverflow = true
            };
            ODDFRAME = true;
            scanline = 261;
            pixel = 340;

            bus = new PPUMemoryBus(this);

            this.currentFrame = new Image(256, 240);
            this.frameBuffer = frameBuffer;

            oam = new OAM();
            secondaryOam = new SecondaryOAM();
        }

        private byte tempBackgroundByte;
        private byte nametableByte;
        private byte attrtableByte;
        private ushort patterntableWord;


        private byte tempSpriteByte;

        private byte secOamAddr = 0;
        private bool secOamFull;
        private bool oamAddrOverflow;
        private byte SpriteIndex;
        internal bool preventVbl = false;

        private byte[] SpriteRenderingCounters = new byte[8];

        public void DrawPixel(uint pixel) {
            if (scanline <= 239 && 0 <= pixel && pixel <= 255)
            {
                bool renderBack = mask.ShowBackground && (mask.BackgroundOnLeft8 || pixel > 7);
                bool renderSprite = mask.ShowSprites && (mask.SpritesOnLeft8 || pixel > 7);

                if (renderBack && renderSprite)
                {
                    int backgroundColorIndex = (((PatternTableShift1 << x) & 0x8000) >> 15) |
                                               (((PatternTableShift2 << x) & 0x8000) >> 14);
                    int backgroundPaletteIndex =
                        (((PaletteShift1 << x) & 0x80) >> 7) | (((PaletteShift2 << x) & 0x80) >> 6);


                    Color color = backgroundColorIndex == 0
                        ? Palette.BasicColors[bus.Palettes.background]
                        : bus.Palettes.Backgrounds[backgroundPaletteIndex][backgroundColorIndex - 1];

                    for (int i = 0; i < 8; i++)
                    {
                        if (spriteXCounters[i] == 0 && SpriteRenderingCounters[i] < 8)
                        {
                            int spriteColorIndex;
                            if (spriteAttributeLatches[i].HorizontalFlip == SpriteFlip.NotFlipped)
                            {
                                var (b1, b2) = spritePatternShiftRegs[i];
                                spriteColorIndex = (((b1 << SpriteRenderingCounters[i]) & 0x80) >> 7) |
                                                   (((b2 << SpriteRenderingCounters[i]) & 0x80) >> 6);
                            }
                            else
                            {
                                var (b1, b2) = spritePatternShiftRegs[i];
                                spriteColorIndex = (((b1 >> SpriteRenderingCounters[i]) & 1)) |
                                                   (((b2 >> SpriteRenderingCounters[i]) & 1) << 1);
                            }

                            if (spriteColorIndex == 0 ||
                                (spriteAttributeLatches[i].Priority == SpritePriority.BehindBackground &&
                                 backgroundColorIndex != 0))
                            {
                                color = backgroundColorIndex == 0
                                    ? Palette.BasicColors[bus.Palettes.background]
                                    : bus.Palettes.Backgrounds[backgroundPaletteIndex][backgroundColorIndex - 1];
                                // color = Color.Blue;
                            }
                            else
                            {
                                color = bus.Palettes.Sprites[spriteAttributeLatches[i].Palette][spriteColorIndex - 1];
                                // color = Color.Red;
                            }

                            if (i == 0 && spriteColorIndex != 0 && backgroundColorIndex != 0 && isRenderingSpriteZero &&
                                pixel != 255)
                            {
                                status.Sprite0Hit = true;
                            }

                            if (spriteColorIndex != 0)
                                break;
                        }
                    }


                    currentFrame.SetPixel(pixel, scanline, color);
                }
                else if (renderBack)
                {
                    int colorIndex = (((PatternTableShift1 << x) & 0x8000) >> 15) |
                                     (((PatternTableShift2 << x) & 0x8000) >> 14);
                    int paletteIndex = (((PaletteShift1 << x) & 0x80) >> 7) | (((PaletteShift2 << x) & 0x80) >> 6);

                    // currentFrame.SetPixel(pixel-1, scanline, new Color((byte)pixel, (byte)scanline, 1));
                    currentFrame.SetPixel(pixel, scanline,
                        colorIndex == 0
                            ? Palette.BasicColors[bus.Palettes.background]
                            : bus.Palettes.Backgrounds[paletteIndex][colorIndex - 1]);
                }
                else if (renderSprite)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if (spriteXCounters[i] == 0 && SpriteRenderingCounters[i] < 8)
                        {
                            int spriteColorIndex;
                            Color color;
                            if (spriteAttributeLatches[i].HorizontalFlip == SpriteFlip.NotFlipped)
                            {
                                var (b1, b2) = spritePatternShiftRegs[i];
                                spriteColorIndex = (((b1 << SpriteRenderingCounters[i]) & 0x80) >> 7) |
                                                   (((b2 << SpriteRenderingCounters[i]) & 0x80) >> 6);
                            }
                            else
                            {
                                var (b1, b2) = spritePatternShiftRegs[i];
                                spriteColorIndex = (((b1 >> SpriteRenderingCounters[i]) & 1)) |
                                                   (((b2 >> SpriteRenderingCounters[i]) & 1) << 1);
                            }

                            if (spriteColorIndex == 0)
                            {
                                color = Palette.BasicColors[bus.Palettes.background];
                            }
                            else
                            {
                                color = bus.Palettes.Sprites[spriteAttributeLatches[i].Palette][spriteColorIndex - 1];
                            }

                            currentFrame.SetPixel(pixel, scanline, color);
                            break;
                        }
                    }
                }
                else
                {
                    currentFrame.SetPixel(pixel, scanline, Palette.BasicColors[bus.Palettes.background]);
                }

                if (mask.ShowSprites)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if (spriteXCounters[i] == 0 && SpriteRenderingCounters[i] < 8)
                        {
                            SpriteRenderingCounters[i]++;
                        }

                        if (spriteXCounters[i] > 0)
                        {
                            spriteXCounters[i]--;
                        }
                    }
                }
            }
        }

        public void Cycle()
        {
            IncrementPixel();

            DrawPixel((uint) (pixel - 1));

            if (mask.ShowBackground || mask.ShowSprites) {
                DoSpriteFetches();

                if (scanline <= 239)
                {
                    // ShiftRegs();

                    if (pixel == 0)
                    {
                    }
                    else if (pixel >= 1 && pixel <= 256)
                    {
                        ShiftRegs();
                        DoBackgroundFetches();
                    }
                    else if (pixel == 257)
                    {
                        // v = (ushort) ((v & ~0x081f) | (t & 0x081f));
                        v = (ushort) ((v & ~0x041f) | (t & 0x041f));
                        MainBus.mapper.NotifyVramAddrChange(v);

                    }
                    else if (pixel >= 321 && pixel <= 336)
                    {
                        ShiftRegs();
                        DoBackgroundFetches();
                    }
                    else if (pixel >= 337 && pixel <= 340)
                    {
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
                        ShiftRegs();
                        DoBackgroundFetches();
                    }
                    else if (pixel == 257)
                    {
                        //hori(v) = hori(t)
                        v = (ushort) ((v & ~0x041f) | (t & 0x041f));
                        MainBus.mapper.NotifyVramAddrChange(v);
                    }
                    else if (pixel >= 280 && pixel <= 304)
                    {
                        //vert(v) = vert(t)
                        v = (ushort) ((v & ~0x7be0) | (t & 0x7be0));
                        MainBus.mapper.NotifyVramAddrChange(v);
                    }
                    else if (pixel >= 321 && pixel <= 336)
                    {
                        ShiftRegs();
                        DoBackgroundFetches();
                    }
                    else if (pixel >= 337 && pixel <= 340)
                    {
                        DoBackgroundFetches();
                    }
                }
            }

            if (scanline == 241 && pixel == 1 && !preventVbl)
            {
                StartVBlank();
            }


            // In theory this should be pixel 1
            if (scanline == 261 && pixel == 0)
            {
                status.Sprite0Hit = false;
                status.SpriteOverflow = false;
            }

            if (scanline == 261 && pixel == 1)
            {
                EndVBlank();
            }

            if (rendering)
            {
                if (scanline == 261 && pixel == 339 && ODDFRAME)
                {
                    pixel += 1;
                }
            }

            rendering = mask.ShowSprites || mask.ShowBackground;
        }

        private void DoSpriteFetches()
        {
            if (scanline <= 239)
            {
                if (pixel == 0)
                {
                    SpriteIndex = 0;
                    secOamAddr = 0;
                    secOamFull = false;
                    oamAddrOverflow = false;
                    secondaryOamHasSpriteZero = false;
                    for (int i = 0; i < 8; i++)
                    {
                        SpriteRenderingCounters[i] = 0;
                    }
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
                        tempSpriteByte = oam.Read(OAMADDR);
                        return;
                    }

                    if (oamAddrOverflow)
                    {
                        return;
                    }

                    if (!secOamFull)
                    {
                        secondaryOam.Write(secOamAddr, tempSpriteByte);
                        int y = secondaryOam.Read((ushort) (secOamAddr & 0xfc));
                        bool inRange = scanline >= y &&
                                       (scanline - y) < (control.SpriteSize == SpriteSize._8x8 ? 8 : 16);

                        if (inRange)
                        {
                            if (OAMADDR == 0)
                            {
                                secondaryOamHasSpriteZero = true;
                            }

                            secOamAddr++;
                            OAMADDR++;
                        }
                        else
                        {
                            OAMADDR += 4;
                        }
                    }


                    if (OAMADDR == 0)
                    {
                        oamAddrOverflow = true;
                    }

                    if (secOamAddr >= 8 * 4)
                    {
                        secOamFull = true;
                    }
                }
                else if (pixel <= 320)
                {
                    OAMADDR = 0;
                    switch (pixel % 8)
                    {
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                            break;
                        case 5:
                            spriteXCounters[SpriteIndex] = secondaryOam.Sprites[SpriteIndex].X;
                            break;
                        case 6:
                            spriteAttributeLatches[SpriteIndex] =
                                secondaryOam.Sprites[SpriteIndex].Attribute;
                            break;
                        case 7:
                            ushort spritePatternAddress;
                            uint y = (uint) (spriteAttributeLatches[SpriteIndex].VerticalFlip == SpriteFlip.Flipped
                                ? 7 - (scanline - secondaryOam.Sprites[SpriteIndex].Y)
                                : (scanline - secondaryOam.Sprites[SpriteIndex].Y));

                            if (control.SpriteSize == SpriteSize._8x8)
                            {
                                spritePatternAddress = control.SpritePatterntableAddress8x8;
                                spritePatternAddress |= (ushort) (secondaryOam.Sprites[SpriteIndex].index << 4);
                                spritePatternAddress |= (ushort) (y & 0b111);
                            }
                            else
                            {
                                spritePatternAddress = (ushort) ((secondaryOam.Sprites[SpriteIndex].index & 1) << 12);
                                spritePatternAddress |=
                                    (ushort) ((secondaryOam.Sprites[SpriteIndex].index & 0xfe) << 4);
                                if ((scanline - secondaryOam.Sprites[SpriteIndex].Y >= 8) ^ spriteAttributeLatches[SpriteIndex].VerticalFlip == SpriteFlip.Flipped)
                                {
                                    spritePatternAddress |= 1 << 4;
                                }

                                spritePatternAddress |= (ushort) (y & 0b111);
                            }

                            spritePatternShiftRegs[SpriteIndex] = (bus.Read(spritePatternAddress), bus.Read(
                                (ushort) (spritePatternAddress | 8)));
                            break;
                        case 0:
                            isRenderingSpriteZero = secondaryOamHasSpriteZero;
                            SpriteIndex = (byte) ((SpriteIndex + 1) % 8);
                            break;
                    }
                }
            }

            if (scanline == 261 && pixel == 0)
            {
                SpriteIndex = 0;
                secOamAddr = 0;
                secOamFull = false;
                oamAddrOverflow = false;
                secondaryOamHasSpriteZero = false;
                for (int i = 0; i < 8; i++)
                {
                    SpriteRenderingCounters[i] = 0;
                }
            }

            return;
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
                        bus.Read((ushort) (control.BackgroundPatterntableAddress | (nametableByte << 4) | (v >> 12)));
                    break;
                case 6:
                    patterntableWord = (ushort) (tempBackgroundByte << 8);
                    break;
                case 7:
                    tempBackgroundByte =
                        bus.Read((ushort) ((control.BackgroundPatterntableAddress | ((nametableByte) << 4) |
                                            ((v >> 12) + 8))));
                    break;
                case 0:
                    patterntableWord |= tempBackgroundByte;
                    paletteLatch1 = (byte) ((attrtableByte >> (
                        ((v & 0x40) >> 4) |
                        ((v & 0b10) << 0)
                    )) & 1);
                    paletteLatch2 = (byte) ((attrtableByte >> (
                        (((v & 0x40) >> 4) |
                         ((v & 0b10) << 0)) + 1
                    )) & 1);
                    // paletteLatch1 = (byte) ((attrtableByte >> (((v & 0x4000) >> 12) | ((x & 0b100)))) & 2);
                    // paletteLatch2 = (byte) (((attrtableByte >> (((v & 0x4000) >> 12) | ((x & 0b100)))) >> 2) & 1);
                    PatternTableShift1 |= (ushort) ((patterntableWord >> 8) & 0x00ff);
                    PatternTableShift2 |= (ushort) (patterntableWord & 0x00ff);
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

        private void StartVBlank()
        {
            status.VblankStarted = true;
            if (control.GenNMI_VBL) MainBus.LowNMI();
            else MainBus.HighNMI();
            if (frameBuffer != null) frameBuffer.Update(currentFrame);
            // Console.WriteLine("VBLANK!");
        }

        private void EndVBlank()
        {
            status.VblankStarted = false;
            isRenderingSpriteZero = false;
            MainBus.HighNMI();
        }

        private void IncrementPixel()
        {
            pixel = (ushort) ((pixel + 1) % 341);
            if (pixel == 0)
            {
                scanline = (ushort) ((scanline + 1) % 262);

                if (scanline == 0)
                {
                    preventVbl = false;
                    ODDFRAME = !ODDFRAME;
                }
            }
        }

        public void Reset()
        {
            ODDFRAME = false;
            control.FromByte(0);
            mask.FromByte(0);
            Write(0x2005, 0);
            Write(0x2005, 0);
            w = false;
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
            //MainBus.mapper.NotifyVramAddrChange(v);
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
            //MainBus.mapper.NotifyVramAddrChange(v);
        }

        byte getTile()
        {
            ushort addr = (ushort) (0x2000 | (v & 0x0fff));
            return bus.Read(addr);
        }

        byte getTileAttr()
        {
            ushort addr = (ushort) (0x23c0 | (v & 0x0c00) | ((v >> 4) & 0x38) | ((v >> 2) & 0x07));
            // ushort addr = (ushort) ((control.BaseNametableAddress + 0x23c0 + (v & 0x0c00)) |  ((v >> 4) & 0x38) | ((v >> 2) & 0x07));
            return bus.Read(addr);
        }

        public (byte, byte) Read(ushort addr)
        {
            // Console.WriteLine($"PPU Read: ${addr:x4}");

            switch (addr)
            {
                case 0x2002:
                {
                    if (scanline == 241 && pixel == 0) {
                        preventVbl = true;
                        Console.WriteLine("VBL skip!");
                    }

                    byte val = status.ToByte();

                    status.VblankStarted = false;
                    MainBus.HighNMI();
                    w = false;
                    return (val, 0xFF);
                }
                case 0x2007:
                {
                    byte val = bus.BufferedRead(v);
                    v += control.VramAddrInc;
                    MainBus.mapper.NotifyVramAddrChange(v);
                    return (val, 0xFF);
                }
                default:
                    throw new NotImplementedException($"Could not read {addr:x4}");
            }
        }

        public byte OAMADDR;

        public void Write(ushort addr, byte data)
        {
            // Console.WriteLine($"PPU: ${addr:x4} = {data:x2}");
            switch (addr)
            {
                case 0x2000:
                    t = (ushort) ((t & 0x73ff) | ((data & 0x03) << 10));

                    control.FromByte(data);

                    if (control.GenNMI_VBL && status.VblankStarted)
                        MainBus.LowNMI();
                    else MainBus.HighNMI();

                    break;
                case 0x2001:
                    mask.FromByte(data);
                    break;
                case 0x2002:
                    break;
                case 0x2003:
                    OAMADDR = data;
                    break;
                case 0x2004:
                    if (scanline < 240 || scanline == 261)
                        break;
                    oam.Write(OAMADDR, data);
                    unchecked
                    {
                        OAMADDR++;
                    }

                    break;
                case 0x2005:
                    if (!w)
                    {
                        x = (byte) (data & 0x07);
                        t = (ushort) ((t & ~0x801f) | (data >> 3));
                    }
                    else
                    {
                        t = (ushort) ((t & ~0xf3e0) | ((data & 0xf8) << 2) | ((data & 0x07) << 12));
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
                        MainBus.mapper.NotifyVramAddrChange(v);
                    }
    
                    w = !w;
                    break;
                case 0x2007:
                    bus.Write(v, data);
                    v += control.VramAddrInc;
                    MainBus.mapper.NotifyVramAddrChange(v);

                    break;
                case 0x4014:
                    MainBus.BeginOAM((ushort) (data << 8));
                    break;
                default:
                    throw new NotImplementedException($"Writing to address 0x{addr:X4} is not implemented");
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

        public byte backup;

        public byte ToByte()
        {
            return backup;
        }

        public void FromByte(byte data)
        {
            BaseNametableAddress = (ushort) (0x2000 + 0x400 * (data & 0b11));
            VramAddrInc = (ushort) (1 + ((data & 0b100) >> 2) * 31);
            SpritePatterntableAddress8x8 = (ushort) ((data & 0b1000) * 0x0200);
            BackgroundPatterntableAddress = (ushort) ((data & 0b10000) * 0x0100);
            SpriteSize = (data & 0b100000) == 0 ? SpriteSize._8x8 : SpriteSize._8x16;
            //PPU master/slave select not implemented
            GenNMI_VBL = (data & 0x80) != 0;

            backup = data;
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

        public byte backup;

        public byte ToByte()
        {
            return backup;
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

            backup = data;

            // Console.WriteLine($"Show Background = {ShowBackground}");
            // Console.WriteLine($"Show Sprites = {ShowSprites}");
        }
    }

    struct PPUSTATUS
    {
        public bool SpriteOverflow;
        public bool Sprite0Hit;
        public bool VblankStarted;

        public byte ToByte()
        {
            byte val = 0;

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
            SpriteOverflow = (data & 0x20) != 0;
            Sprite0Hit = (data & 0x40) != 0;
            VblankStarted = (data & 0x80) != 0;
        }
    }
}
