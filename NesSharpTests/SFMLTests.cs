using NUnit.Framework;

#if !SERVER
using System;
using System.Threading;
using SFML.Graphics;
using SFML.Window;
using SFML.Audio;
#endif

namespace NesSharpTests {
#if !SERVER
  public class WindowTests {
    [Test(ExpectedResult = 0), Description("SFML window spawns without errors")]
    public uint Graphics() {
      var mode = new VideoMode(500, 500);
      var win  = new RenderWindow(mode, "SFML window test #1");

      if (win == null) return 1;

      var circle = new SFML.Graphics.CircleShape(100f) {
        FillColor = SFML.Graphics.Color.Blue
      };

      if (circle == null) return 1;

      return 0;
    }

    [Test(ExpectedResult = 0), Description("SFML Audio plays without errrors")]
    public uint Audio() {
      uint duration    = 2;
      uint sampleRate  = 5000;
      uint frequency   = 220; 
      uint amplitude   = Int16.MaxValue / 2;

      uint sampleCount = duration * sampleRate;
      var samples = new short[sampleCount];
      for (uint i = 0; i < sampleCount; i++) {
        samples[i] = (short)(
          amplitude *
          Math.Sin(2 * Math.PI * (i / (double)sampleRate) * frequency)
        );
      }

      var buffer = new SoundBuffer(samples, 1, sampleRate);
      if (buffer == null) return 1;

      var sound = new Sound(buffer);
      if (sound == null) return 1;

      sound.Play();

      while (sound.Status == SoundStatus.Playing) {
        Thread.Sleep(100);
      }
      
      return 0;
    }
  }
#endif
}
