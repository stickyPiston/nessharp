using System;

namespace NesSharp
{
    interface IClockable
    {
        void Cycle();
        void Reset();
    }
}
