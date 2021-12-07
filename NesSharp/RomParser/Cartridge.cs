using System;

namespace NesSharp 
{
	public enum MirrorType {vertical, horizontal}

	public class Cartridge
	{
		public int rombanks, vrombanks, mapperType, rambanks;
		public bool batteryRam, trainer, fourScreen, vsystem, pal;
		public MirrorType mirroring;
	}
}