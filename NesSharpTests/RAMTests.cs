using NUnit.Framework;

namespace NesSharpTests
{
    public class RAMTests
    {
        public NesSharp.RAM TRAM = new NesSharp.RAM();

        [Test]
        public void RWTest()
        {
            TRAM.Write(0x0069, 1);
            Assert.AreEqual(TRAM.Read(0x0069), 1);
            Assert.AreEqual(TRAM.Read(0x0096), 0);
        }
    }
}