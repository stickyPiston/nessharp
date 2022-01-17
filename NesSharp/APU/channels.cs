using System;

namespace NesSharp
{
    public class X2A03 : IAddressable, IClockable
    {
        bool mode;// 0 = 4step, 1 = 5step
        bool inhibit4017;
        public bool irqset;
        
        Bus bus;
        //public double globalTime = 0.0;
        //https://wiki.nesdev.com/w/index.php/APU_Length_Counter
        private readonly short[] lc_table = {10, 254, 20,  2, 40,  4, 80,  6, 160,
                                    8, 60, 10, 14, 12, 26, 14, 12,  16,
                                    24, 18, 48, 20, 96, 22, 192,  24, 72,
                                    26, 16, 28, 32, 30};

        //https://www.nesdev.org/2A03%20technical%20reference.txt
        public Pulse pulse1 = new Pulse(false, false, 0.0, 0.0, 0x0000, 0x0000, 0x0000, 0x0000, false, 1, false);
        public Pulse pulse2 = new Pulse(false, false, 0.0, 0.0, 0x0000, 0x0000, 0x0000, 0x0000, false, 1, false);
        public Noise noise = new Noise(false, false, 0.0, 0.0, 0x0000, 0xDBDB, 0x0000, false, 1);

        //public double globalTime = 0.0;
        public X2A03(Bus bus)
        {
            this.bus = bus;
            noise.n_seq.sequence = 0xDBDB;
        }
        //public (byte, byte) Read(UInt16 addr) {
        public (byte, byte) Read(UInt16 addr) {
            byte value = 0x00;
            if(addr == 0x4015)
            {
                value |= (byte)((pulse1.p_lc.counter > 0) ? 0x01 : 0x00);
                value |= (byte)((pulse2.p_lc.counter > 0) ? 0x02 : 0x00);		
                value |= (byte)((noise.n_lc.counter > 0) ? 0x04 : 0x00);
                //clears inhibit
                //inhibit4017 = false;
                //return ((byte)(irqset ? 1 << 6 : 0), 0xFF);
            }
            //return (0, 0xFF);
            return (value,0xFF);
        }

        public void Write(ushort addr, byte value)
        {
            /* Console.WriteLine($"APU write! {addr} => {value}"); */
            switch (addr)
            {
                //pulse 1 channel
                case 0x4000:
                    /* Console.WriteLine($"Duty cycle becomes {(value & 0xC0) >> 6}"); */
                    switch ((value & 0xC0) >> 6)
                    {
                        case 0x00: pulse1.p_seq.sequence = 0b01000000; pulse1.p_osc.dutycycle = 0.125; break;
                        case 0x01: pulse1.p_seq.new_sequence = 0b01100000; pulse1.p_osc.dutycycle = 0.250; break;
                        case 0x02: pulse1.p_seq.new_sequence = 0b01111000; pulse1.p_osc.dutycycle = 0.500; break;
                        case 0x03: pulse1.p_seq.new_sequence = 0b10011111; pulse1.p_osc.dutycycle = 0.750; break;

                    }
                    pulse1.p_seq.sequence = pulse1.p_seq.new_sequence;
                    pulse1.p_halt = Convert.ToBoolean(value & 0x20);//Convert.ToBoolean
                    pulse1.p_env.volume = Convert.ToUInt16(value & 0x0F);
                    pulse1.p_env.disable = Convert.ToBoolean(value & 0x10); //Convert.ToBoolean
                    break;

                case 0x4001:
                    pulse1.p_swp.enabled = Convert.ToBoolean(value & 0x80);//Convert.ToBoolean
                    pulse1.p_swp.down = Convert.ToBoolean(value & 0x08);//Convert.ToBoolean
                    pulse1.p_swp.divider = (sbyte)(Convert.ToByte(value & 0x70) >> 4);
                    pulse1.p_swp.shift = (sbyte)(value & 0x07);
                    pulse1.p_swp.reload = true;
                    pulse1.p_swp.channel = true;
                    break;

                case 0x4002:
                    pulse1.p_seq.reload = Convert.ToUInt16((pulse1.p_seq.reload & 0xFF00) | Convert.ToUInt16(value));
                    break;

                case 0x4003:
                    pulse1.p_seq.reload = (ushort)((value & 0x07) << 8 | (pulse1.p_seq.reload & 0x00FF));
                    pulse1.p_seq.counter = pulse1.p_seq.reload;
                    pulse1.p_seq.sequence = pulse1.p_seq.new_sequence;
                    pulse1.p_lc.counter = (sbyte)(lc_table[(value & 0xF8) >> 3]);
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
                    pulse2.p_halt = Convert.ToBoolean(value & 0x20); //Convert.ToBoolean
                    pulse2.p_env.volume = Convert.ToUInt16(value & 0x0F);
                    pulse2.p_env.disable = Convert.ToBoolean(value & 0x10); //Convert.ToBoolean
                    break;

                case 0x4005:
                    pulse2.p_swp.enabled = Convert.ToBoolean(value & 0x80); //Convert.ToBoolean
                    pulse2.p_swp.down = Convert.ToBoolean(value & 0x08); //Convert.ToBoolean
                    pulse2.p_swp.divider = (sbyte)(Convert.ToByte(value & 0x70) >> 4);
                    pulse2.p_swp.shift = (sbyte)(value & 0x07);
                    pulse2.p_swp.reload = true;
                    pulse2.p_swp.channel = false;
                    break;

                case 0x4006:
                    pulse2.p_seq.reload = Convert.ToUInt16((pulse2.p_seq.reload & 0xFF00) | Convert.ToUInt16(value));
                    break;

                case 0x4007:
                    pulse2.p_seq.reload = (ushort)((value & 0x07) << 8 | (pulse2.p_seq.reload & 0x00FF));
                    pulse2.p_seq.counter = pulse2.p_seq.reload;
                    pulse2.p_seq.sequence = pulse2.p_seq.new_sequence;
                    pulse2.p_lc.counter = (sbyte)(lc_table[(value & 0xF8) >> 3]);
                    pulse2.p_env.start = true;
                    break;

                case 0x4008:
                    break;

                case 0x4015:
                    pulse1.p_status = Convert.ToBoolean(value & 0x01); //Convert.ToBoolean
                    pulse2.p_status = Convert.ToBoolean(value & 0x02); //Convert.ToBoolean
                    noise.n_status = Convert.ToBoolean(value & 0x04);
                    //return length counter status

                    break;

                case 0x4017:
                    //msb mode
                    mode = (value & 0x80) > 0;

                    //inhibit
                    inhibit4017 = (value & 0x40) > 0;
                    Console.WriteLine("inhibit" + inhibit4017);
                    break;

                case 0x400C:
                    noise.n_env.volume = ((ushort)(value & 0x0F));
                    noise.n_env.disable = Convert.ToBoolean(value & 0x10);
                    noise.n_halt = Convert.ToBoolean(value & 0x20);
                    break;

                case 0x400E:
                    switch (value & 0x0F)
                    {
                        case 0x00: noise.n_seq.reload = 4; break;
                        case 0x01: noise.n_seq.reload = 8; break;
                        case 0x02: noise.n_seq.reload = 16; break;
                        case 0x03: noise.n_seq.reload = 32; break;
                        case 0x04: noise.n_seq.reload = 64; break;
                        case 0x05: noise.n_seq.reload = 96; break;
                        case 0x06: noise.n_seq.reload = 128; break;
                        case 0x07: noise.n_seq.reload = 160; break;
                        case 0x08: noise.n_seq.reload = 202; break;
                        case 0x09: noise.n_seq.reload = 254; break;
                        case 0x0A: noise.n_seq.reload = 380; break;
                        case 0x0B: noise.n_seq.reload = 508; break;
                        case 0x0C: noise.n_seq.reload = 762; break;
                        case 0x0D: noise.n_seq.reload = 1016; break;
                        case 0x0E: noise.n_seq.reload = 2034; break;
                        case 0x0F: noise.n_seq.reload = 4068; break;
                    }
                    break;

                case 0x400F:
                    pulse1.p_env.start = true;
                    pulse2.p_env.start = true;
                    noise.n_env.start = true;
                    noise.n_lc.counter = (sbyte)lc_table[(value & 0xF8) >> 3];
                    break;
            }
        }

        //the pulse object, used in both pulse channels
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
            
            public double globalTime;


            public Pulse(bool x, bool y, double a, double b, uint c, uint z, ushort g, ushort h, bool i, sbyte j, bool k)
            {
                p_status = x;
                p_halt = y;
                p_sample = a;
                p_output = b;

                p_seq = new Sequencer(c, g);
                p_osc = new Oscillator(z);
                p_env = new Envelope(i, h);
                p_swp = new Sweeper(i, j, k);
                p_lc = new Lengthcounter(j);
            }
        }

        // TODO
        public class Noise
        {
            public bool n_status;
            public bool n_halt;
            public double n_sample;
            public double n_output;
            public Sequencer n_seq;
            public Envelope n_env;
            public Lengthcounter n_lc;
            

            public Noise(bool x, bool y, double a, double b, uint z, ushort g, ushort h, bool i, sbyte j)
            {
                n_status = x;
                n_halt = y;
                n_sample = a;
                n_output = b;

                n_seq = new Sequencer(z, g);
                n_env = new Envelope(i, h);
                n_lc = new Lengthcounter(j);
            }

        }

        bool quarterFrame = false;
        bool halfFrame = false;
        UInt16 clock_counter = 0;
        UInt16 clock_counter2 = 0;
        readonly double third = 0.3333333333;
        public double globalTime = 0.0;
        /*
        public double GlobalTime
        {
            get { return globalTime; }
            set { globalTime = value; }
        }*/


        public void Reset() {
        }

        public void Cycle()
        {

            //should be a third of the ppu frequency
            globalTime += third / 1789773;
            /* Console.WriteLine($"globalTime: {globalTime}"); */
            /* Console.WriteLine(clock_counter); */

            //frame_counter
            //89493
            //59,660
            //if (clock_counter == 59660)
            //    irqset = true;
            if (clock_counter % 6 == 0)
            {
                clock_counter2++;

                if (!mode)
                {
                    // 4-Step Sequence Mode
                    if (clock_counter2 == 3728)
                    {
                        //clock_counter2 += 0.5;
                        quarterFrame = true;
                    }

                    if (clock_counter2 == 7457)
                    {
                        //clock_counter2 += 0.5;
                        quarterFrame = true;
                        halfFrame = true;
                    }

                    if (clock_counter2 == 11186)
                    {
                        //clock_counter2 += 0.5;
                        quarterFrame = true;
                    }

                    if (clock_counter2 == 14915)
                    {
                        //Console.WriteLine("clock counter" + clock_counter2);
                        if (inhibit4017 == false)
                        {

                            //Console.WriteLine("HighIRQ sent");
                            //TODO Fix IRQ
                            //bus.HighIRQ(this);
                        }
                        //clock_counter2 += 0.5;
                        quarterFrame = true;
                        halfFrame = true;
                        clock_counter2 = 0;
                    }
                    if (clock_counter2 == 0)
                        irqset = true;
                    if (clock_counter2 == 1)
                        irqset = false;



                    if (quarterFrame == true)
                    {
                        pulse1.p_env.ApuClock(pulse1.p_halt);
                        pulse2.p_env.ApuClock(pulse2.p_halt);
                        noise.n_env.ApuClock(noise.n_halt);
                    }

                    if (halfFrame == true)
                    {
                        pulse1.p_lc.Clock(pulse1.p_status, pulse1.p_halt);
                        pulse2.p_lc.Clock(pulse2.p_status, pulse2.p_halt);
                        pulse1.p_swp.ApuClock(pulse1.p_seq.reload, 0);
                        pulse2.p_swp.ApuClock(pulse2.p_seq.reload, 1);
                        noise.n_lc.Clock(noise.n_status, noise.n_halt);

                    }
                }
                else
                {
                    //5-step sequencer mode
                    if (clock_counter2 == 3729)
                    {
                        quarterFrame = true;
                    }

                    if (clock_counter2 == 7457)
                    {
                        quarterFrame = true;
                        halfFrame = true;
                    }

                    if (clock_counter2 == 11186)
                    {
                        quarterFrame = true;
                    }

                    //step 4 is empty

                    if (clock_counter2 == 18641)
                    {
                        //Console.WriteLine("clock counter" + clock_counter2);
                        quarterFrame = true;
                        halfFrame = true;
                        clock_counter2 = 0;

                    }

                    if (quarterFrame == true)
                    {
                        pulse1.p_env.ApuClock(pulse1.p_halt);
                        pulse2.p_env.ApuClock(pulse2.p_halt);
                        noise.n_env.ApuClock(noise.n_halt);
                    }

                    if (halfFrame == true)
                    {
                        pulse1.p_lc.Clock(pulse1.p_status, pulse1.p_halt);
                        pulse2.p_lc.Clock(pulse2.p_status, pulse2.p_halt);
                        pulse1.p_swp.ApuClock(pulse1.p_seq.reload, 0);
                        pulse2.p_swp.ApuClock(pulse2.p_seq.reload, 1);
                        noise.n_lc.Clock(noise.n_status, noise.n_halt);

                    }
                }

                //pulse1.p_seq.Clock(
                //    pulse1.p_status,
                //    (s) => ((s & 0x0001) << 7) | ((s & 0x00FE) >> 1)
                //);
                //pulse1.p_sample = pulse1.p_seq.output;
            pulse1.p_osc.frequency = 1789773.0 / (16.0 * (pulse1.p_seq.reload + 1));
            pulse1.p_osc.amplitude = (double)(pulse1.p_env.output - 1) / 16.0;
            pulse1.p_sample = pulse1.p_osc.Sample(globalTime);
            pulse1.p_output += (pulse1.p_sample - pulse1.p_output) /* 0.5*/;


            //pulse1.p_osc.frequency = 1789773.0 / (16.0 * (pulse1.p_seq.reload + 1));
            //pulse1.p_osc.amplitude = (double)(pulse1.p_env.output - 1) / 16.0;
            //pulse1.p_sample = pulse1.p_osc.Sample(globalTime);
            //if (pulse1.p_lc.counter > 0 && pulse1.p_seq.counter >= 8 && !pulse1.p_swp.mute && pulse1.p_env.output > 2)
            //    pulse1.p_output += (pulse1.p_sample - pulse1.p_output) /** 0.5*/;
            //else
            //    pulse1.p_output = 0;

            pulse2.p_osc.frequency = 1789773.0 / (16.0 * (pulse2.p_seq.reload + 1));
            pulse2.p_osc.amplitude = (double)(pulse2.p_env.output - 1) / 16.0;
            pulse2.p_sample = pulse2.p_osc.Sample(globalTime);
            pulse2.p_output += (pulse2.p_sample - pulse2.p_output) /** 0.5*/;

            //pulse2.p_osc.frequency = 1789773.0 / (16.0 * (pulse2.p_seq.reload + 1));
            //pulse2.p_osc.amplitude = (double)(pulse2.p_env.output - 1) / 16.0;
            //pulse2.p_sample = pulse2.p_osc.Sample(globalTime);
            //if (pulse2.p_lc.counter > 0 && pulse2.p_seq.counter >= 8 && !pulse2.p_swp.mute && pulse2.p_env.output > 2)
            //    pulse2.p_output += (pulse2.p_sample - pulse2.p_output) /** 0.5*/;
            //else
            //    pulse2.p_output = 0;

            //if (pulse1.p_lc.counter > 0 && pulse1.p_seq.counter >= 8 && !pulse1.p_swp.mute && pulse1.p_env.output > 2)
            //    pulse1.p_output += (pulse1.p_sample - pulse1.p_output) * 0.5;
            //else
            //    pulse1.p_output = 0;
            //pulse1.p_seq.Clock(
            //    pulse1.p_status,
            //    (s) => ((s & 0x0001) << 7) | ((s & 0x00FE) >> 1)
            //);
            //pulse1.p_sample = pulse1.p_seq.output;

            noise.n_seq.Clock(noise.n_status,
                s => (((s & 0x0001) ^ ((s & 0x0002) >> 1)) << 14) | ((s & 0x7FFF) >> 1)
            );
            if (noise.n_lc.counter > 0 && noise.n_seq.counter >= 8)
            {
                noise.n_output = noise.n_seq.output * ((noise.n_env.output - 1) / 16.0);
            }

                if (!pulse1.p_status) pulse1.p_output = 0;
                if (!pulse2.p_status) pulse2.p_output = 0;
                if (!noise.n_status) noise.n_output = 0;
            }
        pulse1.p_swp.TrackClock(pulse1.p_seq.reload);
        pulse2.p_swp.TrackClock(pulse2.p_seq.reload);

        clock_counter++;
}

        public double noiseOutput()
        {
            //Console.WriteLine($"Pulse 1 output: {(((1.0 * pulse1.p_sample) - 0.8) * 0.5 + ((1.0 * pulse2.p_sample) - 0.8) * 0.5) * Int16.MaxValue}");
            //Console.WriteLine($"Pulse 1 output: {((pulse1.p_sample - 0.5) * 0.5 + (pulse2.p_sample - 0.5) * 0.5) * Int16.MaxValue}");
            //Console.WriteLine($"Pulse 1 sample: {(pulse1.p_sample / 2) + (pulse2.p_sample /2) * Int16.MaxValue}");
            //Console.WriteLine($"Noise sample: {(2.0 *(noise.n_output - 0.5) * 0.1) * 150000}");
            //Console.WriteLine($"Pulse 1 output: {(((1.0 * pulse1.p_output) - 0.8) * 0.5) * 100000}");

            //return (double)(((1.0 * pulse1.p_output) - 0.8) * 0.05 + ((1.0 * pulse2.p_output) - 0.8) *0.05);

            //return (double)((pulse1.p_output - 0.5) * 0.05 + (pulse2.p_output - 0.5) * 0.05);
            //return (short)((pulse1.p_sample / 2) + (pulse2.p_sample /2));

            return (double)((((1.0 * pulse1.p_output) - 0.8) * 0.05 + ((1.0 * pulse2.p_output) - 0.8) * 0.05) + (2.0 * (noise.n_output - 0.5)* 0.05)  );

            //return (double)((((1.0 * pulse1.p_output) - 0.8)*0.5 ));
            //return (double)((2.0 * (noise.n_output - 0.5))*0.05);
        }
    }
}

