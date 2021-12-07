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
				var cartridge = new Cartridge();

				cartridge.rombanks   = bytes[4];
				cartridge.vrombanks  = bytes[5];
				cartridge.mirroring  = bytes[6] & 1 ? MirrorType.vertical : MirrorType.horizontal;
				cartridge.batteryRam = bytes[6] & 2;
				cartridge.trainer    = bytes[6] & 4;
				cartridge.fourScreen = bytes[6] & 8;
				cartridge.mapperType = (bytes[6] & 0xF0) >> 4;
				
				cartridge.vsystem = bytes[7] & 1;
				if (bytes[7] & 0xE != 0) {throw Error()}

				cartridge.mapperType |= bytes[7] & 0xF0;
				cartridge.rambanks    = bytes[8] == 0 ? bytes[8] : 1;
				
				// PAL
				if(bytes[9] & 1) {throw Error()};

				if(bytes[9] & 0xFE != 0) {throw Error()};

				if(bytes.Skip(10).Take(5).Aggregate(0, (a, x) => a + x)) {throw Error()};

				uint romsize = 16 * 1024 * cartridge.rombanks, vromsize = 8 * 1024 * cartridge.vrombanks;
				byte[] trainerBytes = new byte[512], 
					   romBytes     = new byte[romsize], 
					   vromBytes    = new byte[vromsize];
                if (cartridge.trainer)
                {
					Array.Copy(trainerBytes, 16, bytes, 0, 512);
					Array.Copy(romBytes, 528, bytes, 0, romsize);
					Array.Copy(vromBytes, romsize + 528, bytes, 0, vromsize);
				}
                else
                {
					Array.Copy(romBytes, 16, bytes, 0, romsize);
					Array.Copy(vromBytes, 16 + romsize, bytes, 0, vromsize);
				}
				// TODO: Bind ROM and VROM to Mapper
			}
		}
	}
}