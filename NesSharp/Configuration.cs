using System.IO;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SFML.Window;

namespace NesSharp {
    // This class mirrors the json layout in Configuration.json
    public class Configuration {
        public uint ControllerCount {get; set;}
        public Keyboard.Key[] Keymap1 {get; set;}
        public Keyboard.Key[] Keymap2 {get; set;}
    };

    public class ConfigurationManager {
        private static Configuration instance;
        public static Configuration getConfig() => instance;
        public static void LoadConfiguration(string source) {
            var options = new JsonSerializerOptions
            {
                Converters = {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
            instance = JsonSerializer.Deserialize<Configuration>(source, options);
        }
    };
};
