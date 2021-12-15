using SFML.Audio;
using SFML.Window;
using System;
using System.Threading;
using SFML.Window;
using SFML.System;
using SFML.Graphics;
using NesSharp.PPU;
using Sprite = SFML.Graphics.Sprite;
using Eto.Forms;
using Eto.Drawing;
using Drawable = Eto.Forms.Drawable;

namespace NesSharp {
    public class RandomRam : IAddressable
    {
        public byte[] Bytes;

        public RandomRam()
        {
            Random rand = new Random();
            Bytes = new byte[0x10000];
            rand.NextBytes(Bytes);
        }

        public (byte, byte) Read(ushort addr)
        {
            return (Bytes[addr], 0xFF);
        }

        public void Write(ushort addr, byte data)
        {
            Bytes[addr] = data;
        }
    }

    public class MainForm : Form
    {
        public Drawable panel; 

        public MainForm() {
            Title = "NES#";
            ClientSize = new Size(256 * 2, 240 * 2);
            Resizable = false;
            Content = panel = new Drawable();
        }

        public void Loop(IntPtr handle) {
            Emulator emulator = new Emulator();
            emulator.Setup(handle);

            Clock c = new Clock();
            // Run Emulator
            while (Visible)
            {
                Application.Instance.RunIteration();

                emulator.bus.RunFrame();

                emulator.rw.DispatchEvents();
                emulator.rw.Clear();

                emulator.rw.Draw(emulator.s);
                emulator.rw.Display();
                
                Console.WriteLine(1/c.ElapsedTime.AsSeconds());
                c.Restart();
            }
        }
    }

    public class Emulator
    {
        public RenderWindow rw;
        public Sprite s;
        public Bus bus;

        public void Setup(IntPtr handle) {
            // Create Bus, CPU, and ControllerPort
            bus = new Bus();
            var cpu = new CPU(bus);
            var controllerPort = new ControllerPort();

            var controller1 = new Controller(1);
            var controller2 = new Controller(2);
            controllerPort.register(controller1);
            controllerPort.register(controller2);
            bus.Register(cpu);
            bus.Register(controllerPort, new Range[] {new Range(0x4016, 0x4017)});
           
            // Create window
            if (handle == IntPtr.Zero) {
                rw = new RenderWindow(new VideoMode(256, 240), "NES#", Styles.Default ^ Styles.Resize);
                rw.Size = new Vector2u(256 * 2, 240 * 2);
            } else {
                rw = new RenderWindow(handle);
                rw.SetView(new View(new FloatRect(0, 0, 256, 240)));
            }

            // Create render texture
            Texture im = new Texture(256, 240);
            s = new Sprite(im);
            s.TextureRect = new IntRect(0, 0, 256, 240);

            // Create PPU
            PPU.PPU ppu = new PPU.PPU(im, bus);
            PPUMemoryBus ppubus = ppu.bus;
            ppubus.Palettes = new PPUPalettes();
            ppubus.Nametables = new RandomRam();
            ppubus.Patterntables = new RandomRam();

            bus.Register(ppu);
            bus.Register(new Repeater(ppu, 0x2000, 8), new Range[] { new Range(0x2000, 0x3fff)});
            bus.Register(ppu, new []{new Range(0x4014, 0x4014)});
            RAM ram = new RAM(0x10000);
            bus.Register(ram, new []{ new Range(0x8000, 0xffff), new Range(0, 0x800), new Range(0x6000, 0x7fff), new Range(0x4000, 0x7fff)});
            bus.Register(new Repeater(ram, 0, 0x800), new []{new Range(0x800, 0x1fff)});
            
            // Enable rendering
            // ppu.Write(0x2001, 0x18);
            
            byte x = 0; // Scrolling test

            // Cartridge cart = RomParser.Parse("C:\\Users\\maxva\\OneDrive - Universiteit Utrecht\\Uni\\nessharp\\NesSharpTests\\roms\\ppu_vbl_nmi\\rom_singles\\10-even_odd_timing.nes");
            // Cartridge cart = RomParser.Parse("C:\\Users\\maxva\\Downloads\\Donkey Kong (World) (Rev A).nes");
            Cartridge cart = RomParser.Parse("/home/astavie/Downloads/dk.nes");
            // Cartridge cart = RomParser.Parse("C:\\Users\\maxva\\Downloads\\color_test.nes");
            // Cartridge cart = RomParser.Parse("C:\\Users\\maxva\\Downloads\\blargg_ppu_tests_2005.09.15b\\palette_ram.nes");
            Console.WriteLine(cart.rombytes.Length);
            for (int i = 0; i < cart.rombytes.Length; i++)
            {
                bus.Write((ushort)(0x8000 + i), cart.rombytes[i]);
                if (cart.rombytes.Length == 0x4000)
                {
                    bus.Write((ushort)(0xc000 + i), cart.rombytes[i]);

                }
            }
            for (int i = 0; i < cart.vrombytes.Length; i++)
            {
                ppubus.Write((ushort)i, cart.vrombytes[i]);                
            }
        }

        public static void Main(string[] args)
        {
            Emulator emulator = new Emulator();
            emulator.Setup(IntPtr.Zero);

            Clock c = new Clock();
            // Run Emulator
            while (true)
            {
                // Application.Instance.RunIteration();

                emulator.bus.RunFrame();

                emulator.rw.DispatchEvents();
                emulator.rw.Clear();

                emulator.rw.Draw(emulator.s);
                emulator.rw.Display();
                
                Console.WriteLine(1/c.ElapsedTime.AsSeconds());
                c.Restart();
            }
        }
    }
}
