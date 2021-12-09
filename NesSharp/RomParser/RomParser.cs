using System;
using System.IO;
using System.Linq;

namespace NesSharp
{
    public class RomParser
    {
        public static Cartridge Parse(string filename)
        {
            byte[] bytes = File.ReadAllBytes(filename);
            if (bytes[0] == 'N' && bytes[1] == 'E' && bytes[2] == 'S' && bytes[3] == 0x1a)
            {
                // return ((bytes[7] & 2) == 0 && (bytes[7] & 4) == 4) ? ParseNes2(bytes) : ParseINes(bytes);
                return ParseINes(bytes);
            }
            else
            {
                // Other Formats
                throw new Exception("Wrong file format");
            }
            return null;
        }

        public static Cartridge ParseINes(byte[] bytes)
        {
            var cartridge = new Cartridge();

            cartridge.rombanks = bytes[4];
            cartridge.vrombanks = bytes[5];
            cartridge.mirroring = (bytes[6] & 1) > 0 ? MirrorType.vertical : MirrorType.horizontal;
            cartridge.batteryRam = (bytes[6] & 2) > 0;
            cartridge.trainer = (bytes[6] & 4) > 0;
            cartridge.fourScreen = (bytes[6] & 8) > 0;
            cartridge.mapperType = (bytes[6] & 0xF0) >> 4;

            cartridge.consoleType = (bytes[7] & 1) > 0 ? ConsoleType.VSYS : ConsoleType.NES;
            if ((bytes[7] & 0xE) != 0) { throw new Exception(); };

            cartridge.mapperType |= bytes[7] & 0xF0;
            cartridge.rambanks = bytes[8] > 0 ? bytes[8] : 1;

            cartridge.timingType = (bytes[9] & 1) > 0 ? TimingType.PAL : TimingType.NTSC;

            if ((bytes[9] & 0xFE) != 0) { throw new Exception(); };

            if (bytes.Skip(10).Take(5).Aggregate(0, (a, x) => a + x) > 0) { throw new Exception(); };

            int romsize = 16 * 1024 * cartridge.rombanks, vromsize = 8 * 1024 * cartridge.vrombanks;

            cartridge.trainerbytes = new byte[512];
            cartridge.rombytes     = new byte[romsize];
            cartridge.vrombytes    = new byte[vromsize];

            if (cartridge.trainer)
            {
                Array.Copy(bytes, 16, cartridge.trainerbytes, 0, 512);
                Array.Copy(bytes, 528, cartridge.rombytes, 0, romsize);
                Array.Copy(bytes, romsize + 528, cartridge.vrombytes, 0, vromsize);
            }
            else
            {
                Array.Copy(bytes, 16, cartridge.rombytes, 0, romsize);
                Array.Copy(bytes, 16 + romsize, cartridge.vrombytes, 0, vromsize);
            }
            // TODO: Bind ROM and VROM to Mapper
            return cartridge;
        }

        /* If we find a ROM that needs NES 2.0 support, we will fix this class.
        
        public static Cartridge ParseNes2(byte[] bytes)
        {
            var cartridge = new Cartridge();

            cartridge.mirroring = (bytes[6] & 1) > 0 ? MirrorType.vertical : MirrorType.horizontal;
            cartridge.rombanks = bytes[4];
            cartridge.vrombanks = bytes[5];

            cartridge.batteryRam = (bytes[6] & 2) > 0;
            cartridge.trainer = (bytes[6] & 4) > 0;
            cartridge.fourScreen = (bytes[6] & 8) > 0;
            cartridge.mapperType = (bytes[6] & 0xF0) >> 4;

            switch (bytes[7] & 3)
            {
                case 0: cartridge.consoleType = ConsoleType.NES; break;
                case 1: cartridge.consoleType = ConsoleType.VSYS; break;
                case 2: cartridge.consoleType = ConsoleType.PC10; break;
                case 3: cartridge.consoleType = ConsoleType.ECT; break;
            }

            cartridge.mapperType |= bytes[7] & 0xF0;
            cartridge.mapperType |= (bytes[8] & 0xF) << 8;

            cartridge.submapper = (bytes[8] & 0xF0) >> 4;

            cartridge.rombanks |= (bytes[9] & 0xF) << 8;
            cartridge.vrombanks |= (bytes[9] & 0xF0) << 4;

            cartridge.prgramSize = 64 << (bytes[10] & 0xF);
            cartridge.eepromSize = 64 << (bytes[10] & (0xF0 >> 4));
            cartridge.chrramSize = 64 << (bytes[11] & 0xF);
            cartridge.chrnvramSize = 64 << (bytes[11] & (0xF0 >> 4));

            switch (bytes[12] & 3)
            {
                case 0: cartridge.timingType = TimingType.NTSC; break;
                case 1: cartridge.timingType = TimingType.PAL; break;
                case 2: cartridge.timingType = TimingType.MR; break;
                case 3: cartridge.timingType = TimingType.DENDY; break;
            }

            if (cartridge.consoleType == ConsoleType.VSYS)
            {
                var specifics = new VsysSpecifics();
                specifics.ppuType = bytes[13] & 0xF;
                specifics.hardwareType = (bytes[13] & 0xF0) >> 4;
                cartridge.consoleTypeSpecifics = specifics;
            }

            if (cartridge.consoleType == ConsoleType.ECT)
            {
                var specifics = new EctSpecifics();
                specifics.consoleType = bytes[13] & 0xF;
                cartridge.consoleTypeSpecifics = specifics;
            }

            cartridge.misrom = bytes[14] & 3;
            cartridge.expansionDevice = bytes[15] & 0x3F;

            int offset = 16;
            if (cartridge.trainer)
            {
                Array.Copy(cartridge.trainerbytes, offset, bytes, 0, 512);
                offset += 512;
            }

            if ((cartridge.rombanks & 0xF00) == 0xF00)
            {
                int T = (int)(Math.Pow(2, ((cartridge.rombanks & 0xFC) >> 2))) * ((cartridge.rombanks & 3) * 2 + 1);
                Array.Copy(cartridge.rombytes, offset, bytes, 0, T);
                offset += T;
            }
            else
            {
                int U = cartridge.rombanks * 16 * 1024;
                Array.Copy(cartridge.rombytes, offset, bytes, 0, U);
                offset += U;
            }

            if ((cartridge.vrombanks & 0xF00) == 0xF00)
            {
                int V = (int)(Math.Pow(2, ((cartridge.vrombanks & 0xFC) >> 2))) * ((cartridge.vrombanks & 3) * 2 + 1);
                Array.Copy(cartridge.vrombytes, offset, bytes, 0, V);
                offset += V;
            }
            else
            {
                int W = cartridge.vrombanks * 16 * 1024;
                Array.Copy(cartridge.vrombytes, offset, bytes, 0, W);
                offset += W;
            }

            if (cartridge.misrom > 0)
            {
                Array.Copy(cartridge.misrombytes, offset, bytes, 0, bytes.Length - offset);
            }

            return cartridge;
        }*/
    }
}