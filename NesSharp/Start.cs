﻿using SFML.Audio;
using SFML.Window;
using System;
using System.Threading;
using SFML.Window;
using SFML.System;
using SFML.Graphics;
using NesSharp.PPU;
using Sprite = SFML.Graphics.Sprite;

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

        public byte Read(ushort addr)
        {
            return Bytes[addr];
        }

        public void Write(ushort addr, byte data)
        {
            Bytes[addr] = data;
        }
    }

    class Emulator
    {
        static void Main(string[] args)
        {
            // Create Bus, CPU, and ControllerPort
            var bus = new Bus();
            var cpu = new CPU(bus);
            var controllerPort = new ControllerPort();

            var controller = new Controller(1);
            controllerPort.register(controller);
            bus.Register(cpu);
            bus.Register(controllerPort, new Range[] {new Range(0x4016, 0x4017)});
           
            // Create window
            RenderWindow rw = new RenderWindow(new VideoMode(256, 240), "NES#", Styles.Default ^ Styles.Resize);
            rw.Size = new Vector2u(256 * 2, 240 * 2);

            // Create render texture
            Texture im = new Texture(256, 240);
            Sprite s = new Sprite(im);
            s.TextureRect = new IntRect(0, 0, 256, 240);

            // Create PPU
            PPU.PPU ppu = new PPU.PPU(im, bus);
            PPUMemoryBus ppubus = ppu.bus;
            ppubus.Palettes = new PPUPalettes();
            ppubus.Nametables = new RandomRam();
            ppubus.Patterntables = new RandomRam();

            bus.Register(ppu); 
            
            // Enable rendering
            ppu.Write(0x2001, 0x08);
            
            byte x = 0; // Scrolling test


            Clock c = new Clock();
            // Run Emulator
            while (true)
            {
                int frames = ppu.FrameCycleCount();
                for (int i = 0; i < 250 * 341; i++) bus.Tick();
                
                // Scrolling test
                ppu.Read(0x2002);
                ppu.Write(0x2005, x);
                unchecked
                {
                    x++;
                }

                for (int i = 250 * 341; i < frames; i++) bus.Tick();

                rw.DispatchEvents();
                rw.Clear();

                rw.Draw(s);
                rw.Display();
                
                Console.WriteLine(1/c.ElapsedTime.AsSeconds());
                c.Restart();
            }
        }
    }
}
