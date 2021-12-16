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
        private Control panel;
        private Emulator emulator;
        private Thread emuThread;
        private bool running;
        private Func<IntPtr, IntPtr> handleGetter;
        private IntPtr handle;

        public MainForm(Func<IntPtr, IntPtr> handleGetter) {
            this.handleGetter = handleGetter;

            Title = "NES#";
            ClientSize = new Size(256 * 2, 240 * 2);
            Resizable = false;
            Content = panel = new Panel();

            this.Menu = new MenuBar();
            ButtonMenuItem item = new ButtonMenuItem { Text = "File" };
            item.Items.Add(new ButtonMenuItem(Open) { Text = "Open..." });
            item.Items.Add(new ButtonMenuItem(Close) { Text = "Close" });
            this.Menu.Items.Add(item);
            
            emulator = new Emulator();

            Shown += WhenShown;
            Closed += WhenClosed;
        }

        public static void Start(string platform, Func<IntPtr, IntPtr> handleGetter) {
            new Application(platform).Run(new MainForm(handleGetter));

                /* MainForm form = new MainForm(handleGetter); */
                /* form.Owner = a.MainForm; */
                /* form.Show(); */

                /* while (form.Visible) { */
                /*     a.RunIteration(); */
                /*     lock(form.emulator) { form.emulator.Render(); } */
                /* } */
        }

        public void WhenShown(object o, EventArgs e) {
            handle = handleGetter(panel.NativeHandle);
            emulator.SetupScreen(handle);
        }

        public void WhenClosed(object o, EventArgs e) {
            running = false;
        }

        public void Open(object o, EventArgs e) {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog(this) == DialogResult.Ok) {
                lock (emulator) {
                    if (!running) {
                        emulator.SetupCartridge(dialog.FileName);
                        running = true;
                        emuThread = new Thread(Run);
                        emuThread.Start();
                    } else {
                        emulator.SetupCartridge(dialog.FileName);
                    }
                }
            }
        }

        public void Close(object o, EventArgs e) {
            running = false;
        }

        public void Run() {
            Clock c = new Clock();
            // Run Emulator
            while (running)
            {
                lock (emulator) {
                    emulator.RunFrame();
                    Application.Instance.Invoke(emulator.Render);
                }

                Console.WriteLine(1/c.ElapsedTime.AsSeconds());
                c.Restart();
            }
        }
    }

    public class Emulator
    {
        private Texture im;
        private RenderWindow rw;
        private Sprite s;
        private Bus bus;

        public void Render() {
            rw.DispatchEvents();
            rw.Clear();
            rw.Draw(s);
            rw.Display();
        }

        public void RunFrame() {
            bus.RunFrame();
        }

        public void SetupScreen(IntPtr handle) {
            // Create window
            if (handle == IntPtr.Zero) {
                rw = new RenderWindow(new VideoMode(256, 240), "NES#", Styles.Default ^ Styles.Resize);
                rw.Size = new Vector2u(256 * 2, 240 * 2);
            } else {
                rw = new RenderWindow(handle);
                rw.SetView(new View(new FloatRect(0, 0, 256, 240)));
            }

            // Create render texture
            im = new Texture(256, 240);
            s = new Sprite(im);
            s.TextureRect = new IntRect(0, 0, 256, 240);
        }

        public void SetupCartridge(string file) {
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
            
            Cartridge cart = RomParser.Parse(file);
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
            emulator.SetupScreen(IntPtr.Zero);
            emulator.SetupCartridge("/home/astavie/Downloads/dk.nes");

            Clock c = new Clock();
            // Run Emulator
            while (true)
            {
                emulator.RunFrame();                
                Console.WriteLine(1/c.ElapsedTime.AsSeconds());
                c.Restart();
            }
        }
    }
}
