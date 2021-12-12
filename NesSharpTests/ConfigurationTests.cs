using NUnit.Framework;
using NesSharp;
using System.IO;

namespace NesSharpTests {
    public class ConfigurationTests {
        [Test]
        public void TestConfigLoad() {
            string source = File.ReadAllText("../../../Configuration.json");
            ConfigurationManager.LoadConfiguration(source);
            var config = ConfigurationManager.getConfig();

            Assert.AreEqual(2, config.ControllerCount);
            Assert.AreEqual(new string[]{ "Q", "E", "Esc", "Spc", "W", "S", "A", "D" }, config.Keymap1);
            Assert.AreEqual(new string[]{ "U", "O", "Esc", "Spc", "I", "K", "J", "L" }, config.Keymap2);
        }
    };
};
