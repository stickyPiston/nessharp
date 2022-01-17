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
            ButtonMenuItem item = new ButtonMenuItem { Text = "ROM" };
            item.Items.Add(new ButtonMenuItem(Open) { Text = "Open..." });
            item.Items.Add(new ButtonMenuItem(Close) { Text = "Close" });
            this.Menu.Items.Add(item);

            item = new ButtonMenuItem { Text = "Movie" };
            item.Items.Add(new ButtonMenuItem(OpenMovie) { Text = "Play..." });
            item.Items.Add(new ButtonMenuItem(StopMovie) { Text = "Stop" });
            this.Menu.Items.Add(item);
            
            Closed += Close;
        }

        public static void Start(string platform) {
            new Application(platform).Run(new MainForm());
        }

        public void StopMovie(object o, EventArgs e) {
            Monitor.Enter(m_lock);
                emulator.StopMovie();
            Monitor.Exit(m_lock);
        }

        public void OpenMovie(object o, EventArgs e) {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog(this) == DialogResult.Ok) {
                if (running) {
                    Monitor.Enter(m_lock);
                        emulator.OpenMovie(dialog.FileName);
                    Monitor.Exit(m_lock);
                } else {
                }
            }
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

        int fps = 60;

        public void Run() {
            Clock c = new Clock();
            long frame = 0;
            long last = c.ElapsedTime.AsMilliseconds();
            // Run Emulator
            while (running)
            {
                Monitor.Enter(m_lock);
                emulator.RunFrame();
                Monitor.Exit(m_lock);

                Application.Instance.InvokeAsync(() => { if (running) emulator.Render(); });
                
                long time = c.ElapsedTime.AsMilliseconds();
                frame++;

                if (frame % fps == 0) {
                    Console.WriteLine(fps + " frames in " + (time - last) + " milliseconds");
                    last = time;
                }

                // Comment the following 3 lines out to remove frame limiter
                while (time < 1000.0 / fps * frame) {
                    time = c.ElapsedTime.AsMilliseconds();
                }
            }
        }

    }

    public class Emulator
    {
        private Texture im;
        private RenderWindow rw;
        private Sprite s;
        public Bus bus;
        private ControllerPort controllerPort;
        private IMovie movie;
        private string file;

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

        public void OpenMovie(string path) {
            movie = new FM2(File.ReadAllLines(path));
            controllerPort.register(new PlayerController(0, movie), 0);
            controllerPort.register(new PlayerController(1, movie), 1);
            File.Delete(file + ".save");
            movie.Advance();
            SetupCartridge(file);
        }

        public void StopMovie() {
            movie = null;
            controllerPort.register(new Controller(1), 0);
            controllerPort.register(new Controller(2), 1);
        }

        public void RunFrame() {
            if (movie != null) {
                Reset reset = movie.GetReset();
                switch (reset) {
                    case Reset.SOFT:
                        /* bus.Reset(); */
                        SetupCartridge(file);
                        break;
                    case Reset.POWER:
                        SetupCartridge(file);
                        break;
                }
            }

            bus.RunPreVblank();

            if (movie != null) {
                movie.Advance();
                if (movie.Ended()) StopMovie();
            }

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
            InputManager.keysPressed.Clear();
            this.file = file;
            
            // Create Bus, CPU, and ControllerPort
            bus = new Bus();
            var cpu = new CPU(bus);

            if (controllerPort == null) {
                controllerPort = new ControllerPort();

                var controller1 = new Controller(1);
                var controller2 = new Controller(2);
                controllerPort.register(controller1, 0);
                controllerPort.register(controller2, 1);
            }
            bus.Register(cpu);
            bus.Register(controllerPort, new Range[] {new Range(0x4016, 0x4017)});
           
            // Create apu
            X2A03 apu = new X2A03(bus);
            bus.Register(apu);
            bus.Register(apu, new Range[] { new Range(0x4000, 0x4013), new Range(0x4015, 0x4015), new Range(0x4017, 0x4017), new Range(0x400C, 0x400C), new Range(0x400E, 0x400E), new Range(0x400F, 0x400F) });
            Cartridge cart = RomParser.Parse(file);
            
            // Create PPU
            PPU.PPU ppu = new PPU.PPU(im, bus);
            bus.Register(ppu);

            // Create RAM
            RAM ram = new RAM(0x10000);
            bus.Register(new Repeater(ram, 0, 0x800), new []{new Range(0, 0x1fff)});

            // Create Mapper
            bus.Register(cart.mapper);
        }

        public static void Main() { }
    }
}
