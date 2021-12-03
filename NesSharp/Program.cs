using System;
using System.Threading;
using NesSharp.PPU;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Sprite = SFML.Graphics.Sprite;

namespace NesSharp
{
    class RandomRam : IAddressable
    {
        public byte[] Bytes;

        public RandomRam()
        {
            Random rand = new Random();
            Bytes = new byte[0x10000];
            rand.NextBytes(Bytes);
        }

        public byte Read(ushort addr)
        {
            return Bytes[addr];
        }

        public void Write(ushort addr, byte data)
        {
            Bytes[addr] = data;
        }
    }

    class Program
    {
        private static PPU.PPU ppu;
        private static RenderWindow rw;

        static void Main(string[] args)
        {
            rw = new RenderWindow(new VideoMode(256, 240), "Nes#");
            rw.Size = new Vector2u(256 * 2, 240 * 2);
            Texture im = new Texture(256, 240);

            rw.Draw(new Sprite(im));

            ppu = new PPU.PPU(im);
            PPUMemoryBus bus = ppu.bus;
            bus.Palettes = new PPU_Palettes();
            bus.Palettes.Backgrounds = new[]
            {
                new Palette(new[] {Color.Red, Color.White, Color.Yellow}),
                new Palette(new[] {Color.Magenta, Color.Cyan, Color.Red,}),
                new Palette(new[] {Color.Green, Color.Red, Color.Blue,}), Palette.BasicColors,
            };
            bus.Nametables = new RandomRam();
            bus.Patterntables = new RandomRam();

            rw.SetActive();

            Sprite s = new Sprite(im);
            s.TextureRect = new IntRect(0, 0, 256, 240);
            // s.Scale = new Vector2f(VideoMode.DesktopMode.Width, VideoMode.DesktopMode.Height);
            s.Scale = new Vector2f(1, 1);

            byte x = 0;
            Clock c = new Clock();
            ppu.Write(0x2001, 0x08);
            while (true)
            {
                
                ppu.Cycle();
                if ((ppu.Read(0x2002) & 0x80) != 0)
                {
                    rw.DispatchEvents();
                    rw.Clear();

                    rw.Draw(s);
                    rw.Display();
                    // ppu.Write(0x2000, (byte)(x >> 7));
                    ppu.Write(0x2005, x);
                    unchecked
                    {
                        x++;
                    }
                    // Thread.Sleep(2000);
                    // while(true)
                    //     rw.DispatchEvents();

                    // Console.WriteLine(1f / c.ElapsedTime.AsSeconds());
                    // c.Restart();
                }
            }

            Console.WriteLine("Hello World!");
        }
    }
}