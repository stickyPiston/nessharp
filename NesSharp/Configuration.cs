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
        public static void LoadConfiguration() {
        var filepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var path = Path.Combine(filepath, ".NesSharp.json");
            string source;
            try
            {
                source = File.ReadAllText(path);
            }
            catch
            {
                File.WriteAllText(path, source = @"{ ""ControllerCount"": 2,
                ""Keymap1"": [""Z"", ""X"", ""LShift"", ""Enter"", ""Up"", ""Down"", ""Left"", ""Right""],
                ""Keymap2"": [""U"", ""O"", ""Escape"", ""Space"", ""I"", ""K"", ""J"", ""L""] } ");
            }

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
