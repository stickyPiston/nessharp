using System.Threading;
using System.Collections.Generic;

namespace NesSharp {
    struct Range {
        public uint start;
        public uint end;

        public Range(uint start, uint end) {
            this.start = start;
            this.end = end;
        }
    };

    class Bus : IAddressable {
        private List<IAddressable> chips = new();
        private Dictionary<Range, IAddressable> ranges = new();

        public Bus() {
            throw new NotImplementedException();
        }

        //public run(string romFilepath) { when the rom accepts roms
        public run() {
            throw new NotImplementedException();
        }

        public Register(IAddressable chip, Range[] ranges) {
            throw new NotImplementedException();
        }

        public byte Read(ushort addr) {
            throw new NotImplementedException();
        }

        public void Write(ushort addr, byte data) {
            throw new NotImplementedException();
        }
    };
}
