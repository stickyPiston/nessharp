using SFML.Audio;
using SFML.Window;
using System;
using System.Threading;

namespace NesSharp {
  class Emulator {
    static void Main(string[] args) {
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
