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

        private readonly short[] noise_table = {
                                                4, 8, 16, 32, 64, 96, 128, 
                                                160, 202, 254, 380, 508, 
                                                762, 1016, 2034, 4068
                                                };
        public readonly short[] triangle_table ={
                                                 15, 14, 13, 12, 11, 10,  9,  8,
                                                 7,  6,  5,  4,  3,  2,  1,  0,
                                                 0,  1,  2,  3,  4,  5,  6,  7,
                                                 8,  9, 10, 11, 12, 13, 14, 15
                                                  };
        private readonly short[] dmc_table = {428, 380, 340, 320, 286, 
                                              254, 226, 214, 190, 160, 
                                              142, 128, 106,  84,  72,  54
                                                            };

        //https://www.nesdev.org/2A03%20technical%20reference.txt
        public Pulse pulse1 = new Pulse(false, false, 0.0, 0.0, 0x0000, 0x0000, 0x0000, 0x0000, false, 1, false);
        public Pulse pulse2 = new Pulse(false, false, 0.0, 0.0, 0x0000, 0x0000, 0x0000, 0x0000, false, 1, false);
        public Triangle triangle = new Triangle(false, false, false, 0.0, 0.0, 0.0, 0x0000, 0x0000, 0x0000, 0x0000, false, 1, 0x0000, false);
        public Noise noise = new Noise(1, false, false, false, 0.0, 0.0, 0xDBDB, 0x0000, 0x0000, false, 1);

        //public double globalTime = 0.0;
        public X2A03(Bus bus)
        {
            this.bus = bus;
            //noise.n_seq.sequence = 0xDBDB;
        }
        //public (byte, byte) Read(UInt16 addr) {
        public (byte, byte) Read(UInt16 addr) {
            byte value = 0x00;
            if(addr == 0x4015)
            {
                //value |= (byte)((pulse1.p_lc.counter > 0) ? 0x01 : 0x00);
                //value |= (byte)((pulse2.p_lc.counter > 0) ? 0x02 : 0x00);		
                //value |= (byte)((noise.n_lc.counter > 0) ? 0x04 : 0x00);
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
                    //https://wiki.nesdev.org/w/index.php?title=APU_Pulse
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
                    pulse1.p_swp.divider = (byte)((value & 0x70) >> 4);
                    pulse1.p_swp.shift = (byte)(value & 0x07);
                    pulse1.p_swp.reload = true;
                    //pulse1.p_swp.channel = true;
                    break;

                case 0x4002:
                    pulse1.p_seq.reload = Convert.ToUInt16((pulse1.p_seq.reload & 0xFF00) | Convert.ToUInt16(value));
                    break;

                case 0x4003:
                    pulse1.p_seq.reload = (ushort)((value & 0x07) << 8 | (pulse1.p_seq.reload & 0x00FF));
                    pulse1.p_seq.counter = pulse1.p_seq.reload;
                    pulse1.p_seq.sequence = pulse1.p_seq.new_sequence;
                    pulse1.p_lc.counter = (byte)(lc_table[(value & 0xF8) >> 3]);
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
                    pulse2.p_swp.divider = (byte)((value & 0x70) >> 4);
                    pulse2.p_swp.shift = (byte)(value & 0x07);
                    pulse2.p_swp.reload = true;
                    //pulse2.p_swp.channel = false;
                    break;

                case 0x4006:
                    pulse2.p_seq.reload = Convert.ToUInt16((pulse2.p_seq.reload & 0xFF00) | Convert.ToUInt16(value));
                    break;

                case 0x4007:
                    pulse2.p_seq.reload = (ushort)((value & 0x07) << 8 | (pulse2.p_seq.reload & 0x00FF));
                    pulse2.p_seq.counter = pulse2.p_seq.reload;
                    pulse2.p_seq.sequence = pulse2.p_seq.new_sequence;
                    pulse2.p_lc.counter = (byte)(lc_table[(value & 0xF8) >> 3]);
                    pulse2.p_env.start = true;
                    break;

                //triangle channel
                case 0x4008:
                    //stop deze zo in de frame counter en link met linc clock
                    triangle.t_halt = Convert.ToBoolean(value & 0x80);

                    //control flag?
                    triangle.t_linc.start = Convert.ToBoolean(value & 0x80);
                    triangle.t_linc.reload = ((ushort)(value & 0x7F));
                    break;

                case 0x400A:
                    triangle.t_seq.reload = Convert.ToUInt16((triangle.t_seq.reload & 0xFF00) | (value));
                    break;

                case 0x400B:
                    
                    triangle.t_seq.reload = (ushort)((value & 0x07) << 8 | (triangle.t_seq.reload & 0x00FF));
                    //timer period
                    triangle.t_seq.tri_counter = triangle.t_seq.reload;
                    //timer value
                    triangle.t_seq.sequence = triangle.t_seq.new_sequence;
                    //lc value
                    triangle.t_lc.counter = (byte)(lc_table[(value & 0xF8) >> 3]);
                    triangle.t_linc.reset = true;
                    break;

                case 0x4009:
                    //empty
                    break;

                //DMC channel
                case 0x4010:
                    break;

                case 0x4011:
                    break;

                case 0x4012:
                    break;

                case 0x4013:
                    break;

                //status
                case 0x4015:
                    pulse1.p_status = Convert.ToBoolean(value & 0x01); //Convert.ToBoolean
                    pulse2.p_status = Convert.ToBoolean(value & 0x02); //Convert.ToBoolean
                    triangle.t_status = Convert.ToBoolean(value & 0x04);
                    noise.n_status = Convert.ToBoolean(value & 0x08);

                    //return length counter status

                    break;

                case 0x4017:
                    //msb mode
                    mode = (value & 0x80) > 0;

                    //inhibit
                    inhibit4017 = (value & 0x40) > 0;
                    Console.WriteLine("inhibit" + inhibit4017);
                    break;

                //noise channel
                case 0x400C:
                    noise.n_env.volume = ((ushort)(value & 0x0F));
                    noise.n_env.disable = Convert.ToBoolean(value & 0x10);
                    noise.n_halt = Convert.ToBoolean(value & 0x20);
                    break;

                case 0x400E:
                    noise.n_mode = Convert.ToBoolean(value & 0x80);
                    noise.n_seq.reload = (ushort)noise_table[value & 0x0F];
                    break;

                case 0x400F:
                    pulse1.p_env.start = true;
                    pulse2.p_env.start = true;
                    noise.n_env.start = true;
                    noise.n_lc.counter = (byte)lc_table[(value & 0xF8) >> 3];
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


            public Pulse(bool x, bool y, double a, double b, uint c, uint z, ushort g, ushort h, bool i, byte j, bool k)
            {
                p_status = x;
                p_halt = y;
                p_sample = a;
                p_output = b;

                p_seq = new Sequencer(c, g);
                p_osc = new Oscillator(z);
                p_env = new Envelope(i, h);
                p_swp = new Sweeper(i, j);
                p_lc = new Lengthcounter(j);
            }
        }

        public class Triangle
        {
            public bool t_status;
            public bool t_halt;
            public double t_sample;
            public double t_output;
            public double frequency;
            public Sequencer t_seq;
            public Oscillator t_osc;
            //public Envelope t_env;
            //public Sweeper t_swp;
            public Lengthcounter t_lc;
            public Linearcounter t_linc;
            public Triangle(bool x, bool y, bool z, double a, double b, double c, uint d, uint e, ushort g, ushort h, bool i, byte j, ushort k, bool l)
            {
                t_status = x;
                t_halt = y;
                t_sample = a;
                t_output = b;
                frequency = c;

                t_seq = new Sequencer(d, g);
                t_osc = new Oscillator(e);
                //t_env = new Envelope(i, h);
                //t_swp = new Sweeper(i, j);
                t_lc = new Lengthcounter(j);
                t_linc = new Linearcounter(z, l, k);

            }
        }
        // TODO
        public class Noise
        {
            public int n_sr;//shift register
            public bool n_status;
            public bool n_halt;
            public bool n_mode;
            public double n_sample;
            public double n_output;
            public Sequencer n_seq;
            public Envelope n_env;
            public Lengthcounter n_lc;

            public Noise(int u, bool x, bool y, bool z, double a, double b, uint c, ushort g, ushort h, bool i, byte j)
            {
                n_sr = u;
                n_status = x;
                n_halt = y;
                n_mode = z;
                n_sample = a;
                n_output = b;

                n_seq = new Sequencer(c, g);
                n_env = new Envelope(i, h);
                n_lc = new Lengthcounter(j);
            }

        }

        UInt16 fc_counter = 0;
        UInt16 fc_counter2 = 0;
        readonly double third = 0.3333333333;
        public double globalTime = 0.0;

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
            if (fc_counter % 6 == 0)
            {
                fc_counter2++;

                if (!mode)
                {
                    // 4-Step Sequence Mode
                    if (fc_counter2 == 3728)
                    {
                        //clock_counter2 += 0.5;
                        EnvClock();
                    }

                    if (fc_counter2 == 7457)
                    {
                        //clock_counter2 += 0.5;
                        EnvClock();
                        LcClock();
                    }

                    if (fc_counter2 == 11186)
                    {
                        //clock_counter2 += 0.5;
                        EnvClock();
                    }

                    if (fc_counter2 == 14915)
                    {
                        //Console.WriteLine("clock counter" + clock_counter2);
                        if (inhibit4017 == false)
                        {

                            //Console.WriteLine("HighIRQ sent");
                            //TODO Fix IRQ
                            //bus.HighIRQ(this);
                        }
                        //clock_counter2 += 0.5;
                        EnvClock();
                        LcClock();
                        fc_counter2 = 0;
                    }
                    if (fc_counter2 == 0)
                        irqset = true;
                    if (fc_counter2 == 1)
                        irqset = false;



                    //if (quarterFrame == true)
                    //{
                    //    pulse1.p_env.ApuClock(pulse1.p_halt);
                    //    pulse2.p_env.ApuClock(pulse2.p_halt);
                    //    noise.n_env.ApuClock(noise.n_halt);
                    //    triangle.t_linc.ApuClock(triangle.t_halt);
                    //}

                    //if (halfFrame == true)
                    //{
                    //    pulse1.p_lc.Clock(pulse1.p_status, pulse1.p_halt);
                    //    pulse2.p_lc.Clock(pulse2.p_status, pulse2.p_halt);
                    //    triangle.t_lc.Clock(triangle.t_status, triangle.t_halt);
                    //    pulse1.p_swp.SweepClock(pulse1.p_seq.reload, 0);
                    //    pulse2.p_swp.SweepClock(pulse2.p_seq.reload, 1);
                    //    noise.n_lc.Clock(noise.n_status, noise.n_halt);

                    //}
                }
                else
                {
                    //5-step sequencer mode
                    if (fc_counter2 == 3729)
                    {
                        EnvClock();
                    }

                    if (fc_counter2 == 7457)
                    {
                        EnvClock();
                        LcClock();
                    }

                    if (fc_counter2 == 11186)
                    {
                        EnvClock();
                    }

                    //step 4 is empty

                    if (fc_counter2 == 18641)
                    {
                        //Console.WriteLine("clock counter" + clock_counter2);
                        EnvClock();
                        LcClock();
                        fc_counter2 = 0;

                    }

                    //if (quarterFrame == true)
                    //{
                    //    pulse1.p_env.ApuClock(pulse1.p_halt);
                    //    pulse2.p_env.ApuClock(pulse2.p_halt);
                    //    noise.n_env.ApuClock(noise.n_halt);
                    //    triangle.t_linc.ApuClock( triangle.t_halt);
                    //}

                    //if (halfFrame == true)
                    //{
                    //    pulse1.p_lc.Clock(pulse1.p_status, pulse1.p_halt);
                    //    pulse2.p_lc.Clock(pulse2.p_status, pulse2.p_halt);
                    //    triangle.t_lc.Clock(triangle.t_status, triangle.t_halt);
                    //    pulse1.p_swp.SweepClock(pulse1.p_seq.reload, 0);
                    //    pulse2.p_swp.SweepClock(pulse2.p_seq.reload, 1);
                    //    noise.n_lc.Clock(noise.n_status, noise.n_halt);

                    //}
                }

                //pulse1.p_seq.Clock(
                //    pulse1.p_status,
                //    (s) => ((s & 0x0001) << 7) | ((s & 0x00FE) >> 1)
                //);
                //pulse1.p_sample = pulse1.p_seq.output;

            //https://wiki.nesdev.org/w/index.php/APU#Pulse_.28.244000-4007.29
            //The frequency of the pulse channels is a division of the CPU Clock (1.789773MHz NTSC, 1.662607MHz PAL). The output frequency (f) of the generator can be determined by the 11-bit period value (t) written to $4002-4003/$4006-4007. If t < 8, the corresponding pulse channel is silenced.
            //f = CPU / (16 * (t + 1))
            pulse1.p_osc.frequency = 1789773.0 / (16.0 * (pulse1.p_seq.reload + 1));
            pulse1.p_osc.amplitude = (double)(pulse1.p_env.output - 1) / 16.0;
            pulse1.p_sample = pulse1.p_osc.Sample(globalTime);
            if (pulse1.p_lc.counter > 0 && pulse1.p_seq.counter >= 8 && !pulse1.p_swp.mute && pulse1.p_env.output > 2)
                pulse1.p_output += (pulse1.p_sample - pulse1.p_output) /** 0.5*/;
            else
                pulse1.p_output = 0;
            //pulse1.p_output += (pulse1.p_sample - pulse1.p_output) /* 0.5*/;


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
            if (pulse2.p_lc.counter > 0 && pulse2.p_seq.counter >= 8 && !pulse2.p_swp.mute && pulse2.p_env.output > 2)
                pulse2.p_output += (pulse2.p_sample - pulse2.p_output) /** 0.5*/;
            else
                pulse2.p_output = 0;
            //pulse2.p_output += (pulse2.p_sample - pulse2.p_output) /** 0.5*/;

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


                //https://wiki.nesdev.org/w/index.php?title=APU_Triangle
                //f = fCPU/(32*(tval + 1))
                //tval = fCPU/(32*f) - 1
                //waarschijnlijk geen oscillator gebruiken.


                //triangle.t_osc.frequency = 1789773.0 / (32.0 * (triangle.t_seq.reload + 1));
                //triangle.t_osc.amplitude = (double)((triangle.t_env.output - 1) / 32.0);
                //triangle.t_sample = triangle.t_osc.Sample(globalTime);
                //triangle.t_output += (triangle.t_sample - triangle.t_output);
                //triangle.t_output = triangle_table[triangle.t_seq.output];

                triangle.frequency = 1789773.0 / (32 * (triangle.t_seq.reload + 1));
                triangle.t_seq.reload = (ushort)(1789773.0 / (32 * triangle.frequency) - 1);
                if (triangle.t_linc.start)
                {
                    triangle.t_seq.triClock(triangle.t_status);
                }
                if (triangle.t_lc.counter == 0)
                    triangle.t_output = 0;
                else if (triangle.t_status)
                    triangle.t_output = triangle.t_seq.output;

                //triangle.t_output = triangle_table[globalTime];
                //triangle.t_output = triangle.t_seq.output * ((triangle.t_env.output - 1) / 32.0);

                //int feedback_bit;
                //if (noise.n_mode == true) feedback_bit = (noise.n_sr >> 6) & 1;
                //else feedback_bit = (noise.n_sr >> 1) & 1;
                //int feedback = feedback_bit ^ (noise.n_sr & 1);
                //noise.n_sr >>= 1;
                //noise.n_sr &= ~(1 << 14);
                //noise.n_sr |= (feedback << 14);
                //noise.n_status = (noise.n_sr & 1) != 0;

                noise.n_seq.Clock(noise.n_status, 
                        s => (((s & 0x0001) ^ ((s & 0x0002) >> 1)) << 14) | ((s & 0x7FFF) >> 1)
                );
                //if (noise.n_lc.counter > 0 && noise.n_seq.counter >= 8)
                //{
                if (noise.n_lc.counter == 0)
                    noise.n_output = 0;
                else if (noise.n_status)
                    noise.n_output = noise.n_seq.output * ((noise.n_env.output - 1) / 16.0);
                //else
                //    noise.n_output = 0;
                //}
                //else
                //    noise.n_output = 0;


                //if (!pulse1.p_status) pulse1.p_output = 0;
                //if (!pulse2.p_status) pulse2.p_output = 0;
                //if (!noise.n_status) noise.n_output = 0;
            }
            if (!pulse1.p_status) pulse1.p_output = 0;
            if (!pulse2.p_status) pulse2.p_output = 0;
            if (!noise.n_status) noise.n_output = 0;


        pulse1.p_swp.TrackClock(pulse1.p_seq.reload);
        pulse2.p_swp.TrackClock(pulse2.p_seq.reload);

        fc_counter++;
}
        public void EnvClock()
        {
            pulse1.p_env.Clock(pulse1.p_halt);
            pulse2.p_env.Clock(pulse2.p_halt);
            noise.n_env.Clock(noise.n_halt);
            triangle.t_linc.Clock(triangle.t_halt);
        }

        public void LcClock()
        {
            pulse1.p_lc.Clock(pulse1.p_status, pulse1.p_halt);
            pulse2.p_lc.Clock(pulse2.p_status, pulse2.p_halt);
            triangle.t_lc.Clock(triangle.t_status, triangle.t_halt);
            pulse1.p_swp.SweepClock(pulse1.p_seq.reload, 0);
            pulse2.p_swp.SweepClock(pulse2.p_seq.reload, 1);
            noise.n_lc.Clock(noise.n_status, noise.n_halt);
        }
        public double noiseOutput()
        {
            //Console.WriteLine($"Pulse 1 output: {(((1.0 * pulse1.p_sample) - 0.8) * 0.5 + ((1.0 * pulse2.p_sample) - 0.8) * 0.5) * Int16.MaxValue}");
            //Console.WriteLine($"Pulse 1 output: {((pulse1.p_sample - 0.5) * 0.5 + (pulse2.p_sample - 0.5) * 0.5) * Int16.MaxValue}");
            //Console.WriteLine($"Pulse 1 sample: {(pulse1.p_sample / 2) + (pulse2.p_sample /2) * Int16.MaxValue}");
            //Console.WriteLine($"Noise sample: {(2.0 *(noise.n_output - 0.5) * 0.1) * 150000}");
            //Console.WriteLine($"Pulse 1 output: {(((1.0 * pulse1.p_output) - 0.8) * 0.5) * 100000}");
            //Console.WriteLine($"Triangle output: {(((0.4 *triangle.t_output) - 0.5)* 0.1) * 10000}");

            //return (double)(((1.0 * pulse1.p_output) - 0.8) * 0.05 + ((1.0 * pulse2.p_output) - 0.8) *0.05);

            //return (double)((pulse1.p_output - 0.5) * 0.05 + (pulse2.p_output - 0.5) * 0.05);
            //return (short)((pulse1.p_sample / 2) + (pulse2.p_sample /2));

            //return (double)((((pulse1.p_output) - 0.8) * 0.05 + ((pulse2.p_output) - 0.8) * 0.05) + ((noise.n_output - 0.5)* 0.05)  );

            //return (double)((((pulse1.p_output) - 0.8) * 0.05 + ((pulse2.p_output) - 0.8) * 0.05) + ((noise.n_output - 0.5)* 0.05) + ((triangle.t_output - 0.3) * 0.0125));

            return (double)((((pulse1.p_output) - 0.8) * 0.1 + ((pulse2.p_output) - 0.8) * 0.1) + (2.0 * (noise.n_output - 0.5) * 0.1) + ((0.2 * triangle.t_output - 0.5) * 0.1));
            //return (double)(((triangle.t_output - 0.2) * 0.05) + ((pulse1.p_output - 0.8) * 0.05));
            //return (double)(((( 0.4 * triangle.t_output) - 0.5)*0.1 ));
            //return (double)((noise.n_output - 0.5)*0.05);
        }
    }
}

