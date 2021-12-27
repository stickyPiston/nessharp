using NUnit.Framework;
using NesSharp;
using System.IO;
using SFML.Window;
using static SFML.Window.Keyboard;

namespace NesSharpTests {
    public class ConfigurationTests {
        [Test]
        public void TestConfigLoad() {
            ConfigurationManager.LoadConfiguration();
            var config = ConfigurationManager.getConfig();

            Assert.AreEqual(2, config.ControllerCount);
            Assert.AreEqual(new Keyboard.Key[]{ Key.Q, Key.E, Key.Escape, Key.Space, Key.W, Key.S, Key.A, Key.D }, config.Keymap1);
            Assert.AreEqual(new Keyboard.Key[]{ Key.U, Key.O, Key.Escape, Key.Space, Key.I, Key.K, Key.J, Key.L }, config.Keymap2);
        }
    };
};
