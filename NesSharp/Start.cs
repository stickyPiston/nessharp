using SFML.Window;
using System;
using System.IO;
using System.Threading;
using SFML.System;
using SFML.Graphics;
using NesSharp.PPU;
using Sprite = SFML.Graphics.Sprite;
using Eto.Forms;
using Eto.Drawing;
using Keyboard = SFML.Window.Keyboard;
using KeyEventArgs = SFML.Window.KeyEventArgs;
using System.Collections.Generic;

namespace NesSharp {

    public static class InputManager {
        public static HashSet<Keyboard.Key> keysPressed = new HashSet<Keyboard.Key>();

    }

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

        private readonly Object m_lock = new Object();

        public MainForm() {
            Title = "NES#";
            // ClientSize = new Size(256 * 2, 240 * 2);
            Resizable = false;
            // Content = panel = new Panel();

            this.Menu = new MenuBar();
            ButtonMenuItem item = new ButtonMenuItem { Text = "File" };
            item.Items.Add(new ButtonMenuItem(Open) { Text = "Open..." });
            item.Items.Add(new ButtonMenuItem(Close) { Text = "Close" });
            this.Menu.Items.Add(item);
            
            Closed += Close;
        }

        public static void Start(string platform) {
            new Application(platform).Run(new MainForm());
        }

        public void Open(object o, EventArgs e) {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog(this) == DialogResult.Ok) {
                if (running) {
                    Monitor.Enter(m_lock);
                        emulator.SetupCartridge(dialog.FileName);
                    Monitor.Exit(m_lock);
                } else {
                    emulator = new Emulator();
                    emulator.Closed += Close;
                    emulator.SetupScreen(IntPtr.Zero);
                    emulator.SetupCartridge(dialog.FileName);
                    running = true;
                    emuThread = new Thread(Run);
                    emuThread.Start();
                }
            }
        }

        public void Close(object o, EventArgs e) {
            if (running) {
                running = false;
                emuThread.Join();
                emulator.Close();
                emulator = null;
            }
        }

        public void Run() {
            Clock c = new Clock();
            // Run Emulator
            while (running)
            {
                Monitor.Enter(m_lock);
                emulator.RunFrame();
                Monitor.Exit(m_lock);

                Application.Instance.InvokeAsync(() => { if (running) emulator.Render(); });
                
                /* Console.WriteLine(1/c.ElapsedTime.AsSeconds()); */
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
            Update();
            rw.DispatchEvents();
            rw.Clear();
            rw.Draw(s);
            rw.Display();
        }

        public void Update() {
            void updateKey(Keyboard.Key key) {
                if (Keyboard.IsKeyPressed(key) && rw.HasFocus()) {
                    InputManager.keysPressed.Add(key);
                } else {
                    InputManager.keysPressed.Remove(key);
                }
            }

            foreach (var key in ConfigurationManager.getConfig().Keymap1) {
                updateKey(key);
            }

            foreach (var key in ConfigurationManager.getConfig().Keymap2) {
                updateKey(key);
            }
        }

        public void RunFrame() {
            bus.RunFrame();
        }

        public void Close() {
            rw.Close();
        }

        public event EventHandler Closed;

        public void SetupScreen(IntPtr handle) {
            // Create window
            if (handle == IntPtr.Zero) {
                rw = new RenderWindow(new VideoMode(256, 240), "NES#", Styles.Default ^ Styles.Resize);
                rw.Size = new Vector2u(256 * 2, 240 * 2);
                rw.Closed += Closed;
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
            // Load config
            // TODO maybe other place
            ConfigurationManager.LoadConfiguration();
            
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
           
            Cartridge cart = RomParser.Parse(file);
            
            // Create PPU
            PPU.PPU ppu = new PPU.PPU(im, bus);
            PPUMemoryBus ppubus = ppu.bus;
            ppubus.Palettes = new PPUPalettes();

            RandomRam nametables = new RandomRam();
            ppubus.Nametables = cart.mapper.Nametables;
            ppubus.Patterntables = cart.mapper.CHR;

            bus.Register(ppu);
            bus.Register(new Repeater(ppu, 0x2000, 8), new Range[] { new Range(0x2000, 0x3fff)});
            bus.Register(ppu, new []{new Range(0x4014, 0x4014)});
            RAM ram = new RAM(0x10000);
            bus.Register(ram, new []{ new Range(0, 0x800), new Range(0x6000, 0x7fff), new Range(0x4000, 0x7fff)});
            bus.Register(new Repeater(ram, 0, 0x800), new []{new Range(0x800, 0x1fff)});

            bus.Register(cart.mapper.PRG, new[] { new Range(0x8000, 0xffff) });
        }

        public static void Main() { }
    }
}
