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
            Assert.AreEqual(new Keyboard.Key[]{ Key.Z, Key.X, Key.LShift, Key.Enter, Key.Up, Key.Down, Key.Left, Key.Right }, config.Keymap1);
            Assert.AreEqual(new Keyboard.Key[]{ Key.U, Key.O, Key.Escape, Key.Space, Key.I, Key.K, Key.J, Key.L }, config.Keymap2);
        }
    };
};
