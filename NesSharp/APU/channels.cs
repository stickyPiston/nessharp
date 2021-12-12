﻿using System;

namespace NesSharp
{
    public class X2A03
    {
        //https://wiki.nesdev.com/w/index.php/APU_Length_Counter
        private readonly short[] lc_table = {10, 254, 20,  2, 40,  4, 80,  6, 160,   
                                    8, 60, 10, 14, 12, 26, 14, 12,  16, 
                                    24, 18, 48, 20, 96, 22, 192,  24, 72, 
                                    26, 16, 28, 32, 30};
        public void CpuWrite(UInt16 addr, sbyte value)
        {
            Pulse pulse1 = new Pulse(false, false, 0.0, 0.0, 00000000, 0000000, false, 1);
            Pulse pulse2 = new Pulse(false, false, 0.0, 0.0, 00000000, 0000000, false, 1);
            switch (addr)
            {
                //pulse 1 channel
                case 0x4000:
                    switch ((value & 0xC0) >> 6)
                    {  
                        case 0x00: pulse1.p_seq.sequence = 0b01000000; pulse1.p_osc.dutycycle = 0.125; break;
                        case 0x01: pulse1.p_seq.new_sequence = 0b01100000; pulse1.p_osc.dutycycle = 0.250; break;
                        case 0x02: pulse1.p_seq.new_sequence = 0b01111000; pulse1.p_osc.dutycycle = 0.500; break;
                        case 0x03: pulse1.p_seq.new_sequence = 0b10011111; pulse1.p_osc.dutycycle = 0.750; break;
                        
                    }
                    pulse1.p_seq.sequence = pulse1.p_seq.new_sequence;
                    pulse1.p_halt = Convert.ToBoolean(value & 0x20);
                    pulse1.p_env.volume = Convert.ToUInt16(value & 0x0F);
                    pulse1.p_env.disable = Convert.ToBoolean(value & 0x10);
                    break;

                case 0x4001:
                    pulse1.p_swp.enabled = Convert.ToBoolean(value & 0x80);
                    pulse1.p_swp.down = Convert.ToBoolean(value & 0x08);
                    pulse1.p_swp.divider = (sbyte)(Convert.ToSByte(value & 0x70) >> 4);
                    pulse1.p_swp.shift = Convert.ToSByte(value & 0x07);
                    pulse1.p_swp.reload = true;
                    break;

                case 0x4002:
                    pulse1.p_seq.reload = Convert.ToUInt16((pulse1.p_seq.reload & 0xFF00) | Convert.ToUInt16(value));
                    break;

                case 0x4003:
                    pulse1.p_seq.reload = (ushort)((value & 0x07) << 8 | (pulse1.p_seq.reload & 0x00FF));
                    pulse1.p_seq.counter = pulse1.p_seq.reload;
                    pulse1.p_seq.sequence = pulse1.p_seq.new_sequence;
                    pulse1.p_lc.counter = Convert.ToSByte(lc_table[(value & 0xF8) >> 3]);
                    pulse1.p_env.start = true;
                    break;

                    //pulse 2 channel
                case 0x4004:
                    switch ((value & 0xC0) >> 6)
                    {
                        case 0x00: pulse2.p_seq.new_sequence = 0b01000000; pulse2.p_osc.dutycycle = 0.125; break;
                        case 0x01: pulse2.p_seq.new_sequence = 0b01100000; pulse2.p_osc.dutycycle = 0.250; break;
                        case 0x02: pulse2.p_seq.new_sequence = 0b01111000; pulse2.p_osc.dutycycle = 0.500; break;
                        case 0x03: pulse2.p_seq.new_sequence = 0b10011111; pulse2.p_osc.dutycycle = 0.750; break;
                    }
                    pulse2.p_seq.sequence = pulse2.p_seq.new_sequence;
                    pulse2.p_halt = Convert.ToBoolean(value & 0x20);
                    pulse2.p_env.volume = Convert.ToUInt16(value & 0x0F);
                    pulse2.p_env.disable = Convert.ToBoolean(value & 0x10);
                    break;

                case 0x4005:
                    pulse2.p_swp.enabled = Convert.ToBoolean(value & 0x80);
                    pulse2.p_swp.down = Convert.ToBoolean(value & 0x08);
                    pulse2.p_swp.divider = (sbyte)(Convert.ToSByte(value & 0x70) >> 4);
                    pulse2.p_swp.shift = Convert.ToSByte(value & 0x07);
                    pulse2.p_swp.reload = true;
                    break;

                case 0x4006:
                    pulse2.p_seq.reload = Convert.ToUInt16((pulse2.p_seq.reload & 0xFF00) | Convert.ToUInt16(value));
                    break;

                case 0x4007:
                    pulse2.p_seq.reload = (ushort)((value & 0x07) << 8 | (pulse2.p_seq.reload & 0x00FF));
                    pulse2.p_seq.counter = pulse2.p_seq.reload;
                    pulse2.p_seq.sequence = pulse2.p_seq.new_sequence;
                    pulse2.p_lc.counter = Convert.ToSByte(lc_table[(value & 0xF8) >> 3]);
                    pulse2.p_env.start = true;
                    break;
            }
        }
        
        public class Pulse
        {
            public bool p_status;
            public bool p_halt;
            public double p_sample;
            public double p_output;
            public Sequencer p_seq;
            public Oscillator p_osc;
            public Envelope p_env;
            public Sweeper p_swp;
            public Lengthcounter p_lc;

            public Pulse(bool x, bool y, double a, double b, uint z, ushort h, bool i, sbyte j)
            {
                p_status = x;
                p_halt = y;
                p_sample = a;
                p_output = b;

                p_seq =  new Sequencer(z, h);
                p_osc = new Oscillator(z);
                p_env = new Envelope(i, h);
                p_swp = new Sweeper(i, j);
                p_lc = new Lengthcounter(j);
            }
        }
        //cpuread method
        /*(public int CpuRead(ushort addr)
        {
        }*/
    }


    public class ApuClock
    {

    }

}

