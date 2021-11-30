namespace NesSharp.PPU
{
    class OAM : IAddressable
    {
        public Sprite[] Sprites;

        public OAM()
        {
            Sprites = new Sprite[64];
        }


        public byte Read(ushort addr)
        {
            int index = addr / 4;
            ushort offset = (ushort)(addr % 4);

            return Sprites[index].Read(offset);
        }

        public void Write(ushort addr, byte data)
        {
            int index = addr / 4;
            ushort offset = (ushort)(addr % 4);

            Sprites[index].Write(offset, data);
        }
    }

    class SecondaryOAM : OAM
    {
        public SecondaryOAM()
        {
            Sprites = new Sprite[8];
        }
    }
}