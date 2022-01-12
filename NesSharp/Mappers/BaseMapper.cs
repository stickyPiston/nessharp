﻿using System;
using System.IO;

namespace NesSharp.Mappers
{
    public abstract class BaseMapper
    {
        public IAddressable PRG;
        public IAddressable CHR;
        public IAddressable PRGRAM;
        public Nametables Nametables;
        public bool IRQ;
    }

    public class SaveRAM : IAddressable {
        
        private byte[] RAM = new byte[0x2000];
        private string saveFile;

        public SaveRAM() {}

        public SaveRAM(string file) {
            this.saveFile = file;
            if (file != null && File.Exists(file)) {
                byte[] save = File.ReadAllBytes(file);
                if (save.Length == RAM.Length) RAM = save;
            }
        }

        public (byte, byte) Read(ushort addr) {
            return (RAM[addr - 0x6000], 0xFF);
        }

        public void Write(ushort addr, byte data) {
            RAM[addr - 0x6000] = data;
            if (saveFile != null) {
                File.WriteAllBytes(saveFile, RAM);
            }
        }

    }

    public class Nametables : IAddressable {

        private byte[] RAM = new byte[0x1000];
        public MirrorType mirror;

        public Nametables(MirrorType mirror) {
            this.mirror = mirror;
        }

        public (byte, byte) Read(ushort addr) {
            switch (mirror) {
                case MirrorType.horizontal:
                    return (RAM[(ushort) (addr & 0b101111111111)], 0xFF);
                case MirrorType.vertical:
                    return (RAM[(ushort) (addr & 0b011111111111)], 0xFF);
                case MirrorType.fourScreen:
                    return (RAM[addr], 0xFF);
            }
            throw new Exception();
        }

        public void Write(ushort addr, byte data) {
            switch (mirror) {
                case MirrorType.horizontal:
                    RAM[(ushort) (addr & 0b101111111111)] = data;
                    return;
                case MirrorType.vertical:
                    RAM[(ushort) (addr & 0b011111111111)] = data;
                    return;
                case MirrorType.fourScreen:
                    RAM[addr] = data;
                    return;
            }
            throw new Exception();
        }

    }

}
