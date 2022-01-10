using System;
using NesSharp.Mappers;

namespace NesSharp 
{
	public enum MirrorType {vertical, horizontal};
	public enum ConsoleType {NES, VSYS, PC10, ECT};
	public enum TimingType {NTSC, PAL, MR, DENDY};

	public class VsysSpecifics
    {
		public int ppuType, hardwareType;
    }

	public class EctSpecifics
    {
		public int consoleType;
    }

	public class Cartridge
	{
		public int rombanks, vrombanks, rambanks, mapperType, submapper, prgramSize, eepromSize, chrramSize, chrnvramSize, misrom, expansionDevice;
		public bool batteryRam, trainer, fourScreen;
		public byte[] misrombytes;
		public object consoleTypeSpecifics;
		public MirrorType mirroring;
		public ConsoleType consoleType;
		public TimingType timingType;
		public BaseMapper mapper;
	}
	
	
}