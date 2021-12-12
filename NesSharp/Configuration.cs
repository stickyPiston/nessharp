using System.IO;
using System;
using System.Text.Json;

namespace NesSharp {
    // This class mirrors the json layout in Configuration.json
    public class Configuration {
        public uint ControllerCount {get; set;}
        public string[] Keymap1 {get; set;}
        public string[] Keymap2 {get; set;}
    };

    public class ConfigurationManager {
        private static Configuration instance;
        public static Configuration getConfig() => instance;
        public static void LoadConfiguration(string source) {
          instance = JsonSerializer.Deserialize<Configuration>(source);
        }
    };
};
