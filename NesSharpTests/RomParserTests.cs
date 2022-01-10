using NesSharp;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System;

namespace NesSharpTests {
	public class RomParserTests
	{
		[Test]
		public void INesTest()
        {
			byte[] bytes = File.ReadAllBytes("../../../roms/nestest/nestest.nes");
			Cartridge c = RomParser.Parse("../../../roms/nestest/nestest.nes");
			Assert.AreEqual(1, c.rombanks);
			Assert.AreEqual(1, c.vrombanks);
			Assert.AreEqual(MirrorType.horizontal, c.mirroring);
			Assert.AreEqual(false, c.batteryRam);
			Assert.AreEqual(false, c.trainer);
			Assert.AreEqual(false, c.fourScreen);
			Assert.AreEqual(0, c.mapperType);
			Assert.AreEqual(ConsoleType.NES, c.consoleType);
			Assert.AreEqual(1, c.rambanks);
			Assert.AreEqual(TimingType.NTSC, c.timingType);
			Assert.AreEqual(0, c.trainerbytes.Aggregate(0, (a, x) => a + x));
			Assert.AreEqual(bytes.Skip(16).Take(16 * 1024).ToArray(), c.rombytes); ;
			Assert.AreEqual(bytes.Skip(16*1025).Take(8*1024).ToArray(), c.vrombytes);
			Assert.AreEqual(0, c.submapper);
			Assert.AreEqual(0, c.prgramSize);
			Assert.AreEqual(0, c.eepromSize);
			Assert.AreEqual(0, c.chrramSize);
			Assert.AreEqual(0, c.chrnvramSize);
			Assert.AreEqual(0, c.misrom);
			Assert.AreEqual(0, c.expansionDevice);
			Assert.AreEqual(null, c.consoleTypeSpecifics);
			Assert.AreEqual(null, c.misrombytes);
		}
	}
}