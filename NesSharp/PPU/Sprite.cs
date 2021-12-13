using System;

namespace NesSharp.PPU
{
    enum SpriteFlip : byte
    {
        NotFlipped = 0,
        Flipped = 1
    }

    enum SpritePriority : byte
    {
        InFrontBackground = 0,
        BehindBackground = 1
    }
    
    struct SpriteAttribute
    {
        public byte Palette;
        public SpritePriority Priority;
        public SpriteFlip HorizontalFlip;
        public SpriteFlip VerticalFlip;

        public byte ToByte()
        {
            throw new NotImplementedException();
        }

        public void FromByte(byte data)
        {
            Palette = (byte) (data & 0b11);
            Priority =       (data & 0b100000) == 0 ? SpritePriority.InFrontBackground : SpritePriority.BehindBackground;
            HorizontalFlip = (data & 0b1000000) == 0 ? SpriteFlip.NotFlipped : SpriteFlip.Flipped;
            VerticalFlip   = (data & 0b10000000) == 0 ? SpriteFlip.NotFlipped : SpriteFlip.Flipped;
        }
    }
    
    struct Sprite : IAddressable
    {
        public byte Y;
        public byte index;
        public SpriteAttribute Attribute;
        public byte X;
        
        
        public byte Read(ushort addr)
        {
            switch (addr)
            {
                case 0:
                    return Y;
                case 1:
                    return index;
                case 2:
                    return Attribute.ToByte();
                case 3:
                    return X;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public void Write(ushort addr, byte data)
        {
            switch (addr)
            {
                case 0:
                    Y = data;
                    break;
                case 1:
                    index = data;
                    break;
                case 2:
                    Attribute.FromByte(data);
                    break;
                case 3:
                    X = data;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}