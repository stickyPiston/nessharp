using System.Threading;
using System.Collections.Generic;
using System;

namespace NesSharp {
    public struct Range {
        public uint start;
        public uint end;

        public Range(uint start, uint end) {
            this.start = start;
            this.end = end;
        }
    };

    public class Bus : IAddressable {
        private List<IAddressable> chips = new List<IAddressable>();
        private Dictionary<Range, IAddressable> ranges = new Dictionary<Range, IAddressable>();

        //public run(string romFilepath) { when the rom accepts roms
        public void run() {
            throw new NotImplementedException();
        }

        public void Register(IAddressable chip, Range[] ranges) {
            chips.Add(chip);
            foreach(var range in ranges)
            {
                this.ranges.Add(range, chip);
            }
        }

        public byte Read(ushort addr) {
            foreach(KeyValuePair<Range, IAddressable> range in ranges)
            {
                if(addr >= range.Key.start && addr <= range.Key.end)
                {
                    return range.Value.Read(addr);
                }
            }
            return 0;
        }

        public void Write(ushort addr, byte data) {
           foreach(KeyValuePair<Range, IAddressable> range in ranges)
           {
                if(addr >= range.Key.start && addr <= range.Key.end)
                {
                    range.Value.Write(addr, data);
                }
           }
           
        }
    };
}
