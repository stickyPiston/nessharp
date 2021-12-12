using System.IO;
using System.Text.Json;

namespace NesSharp {
  class Emulator {
    static void Main(string[] args) {
      string source = File.ReadAllText("./NesSharp/Configuration.json");
      ConfigurationManager.LoadConfiguration(source);

      var bus = new Bus();
      var cpu = new CPU(bus);
      var controllerPort = new ControllerPort();

      var controller = new Controller(1);
      controllerPort.register(controller);
      bus.Register(cpu);
      bus.Register(controllerPort, new Range[] {new Range(0x4016, 0x4017)});
      bus.Run();
    }
  }
}
