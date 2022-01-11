using System;
using System.Collections.Generic;
using System.Text;

namespace NesSharp.Mappers
{
    public abstract class BaseMapper
    {
        public IAddressable PRG;
        public IAddressable CHR;
        public IAddressable Nametables;
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
            }
            throw new Exception();
        }

    }

}
