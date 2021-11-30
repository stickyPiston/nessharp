using SFML.Audio;
using SFML.Window;
using System;
using System.Threading;

namespace NesSharp {
  class Program {
    static void Main(string[] args) {
      uint duration    = 5;
      uint sampleRate  = 1000;
      uint frequency   = 441; 
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
      var sound = new Sound(buffer);
      sound.Play();

      while (sound.Status == SoundStatus.Playing) {
        Thread.Sleep(100);
      }
    }
  }
}
