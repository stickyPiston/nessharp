using System.Runtime.InteropServices;
using SFML.Graphics;

namespace NesSharp.PPU
{
    public class Palette : IAddressable
    {
        public static readonly Color[] BasicColors =
        {
            new Color(0x656565ff),
            new Color(0x002D69ff),
            new Color(0x131F7Fff),
            new Color(0x3C137Cff),
            new Color(0x600B62ff),
            new Color(0x730A37ff),
            new Color(0x710F07ff),
            new Color(0x5A1A00ff),
            new Color(0x342800ff),
            new Color(0x0B3400ff),
            new Color(0x003C00ff),
            new Color(0x003D10ff),
            new Color(0x003840ff),
            new Color(0x000000ff),
            new Color(0x000000ff),
            new Color(0x000000ff),

            new Color(0xAEAEAEff),
            new Color(0x0F63B3ff),
            new Color(0x4051D0ff),
            new Color(0x7841CCff),
            new Color(0xA736A9ff),
            new Color(0xC03470ff),
            new Color(0xBD3C30ff),
            new Color(0x9F4A00ff),
            new Color(0x6D5C00ff),
            new Color(0x366D00ff),
            new Color(0x077704ff),
            new Color(0x00793Dff),
            new Color(0x00727Dff),
            new Color(0x000000ff),
            new Color(0x000000ff),
            new Color(0x000000ff),

            new Color(0xFEFEFFff),
            new Color(0x5DB3FFff),
            new Color(0x8FA1FFff),
            new Color(0xC890FFff),
            new Color(0xF785FAff),
            new Color(0xFF83C0ff),
            new Color(0xFF8B7Fff),
            new Color(0xEF9A49ff),
            new Color(0xBDAC2Cff),
            new Color(0x85BC2Fff),
            new Color(0x55C753ff),
            new Color(0x3CC98Cff),
            new Color(0x3EC2CDff),
            new Color(0x4E4E4Eff),
            new Color(0x000000ff),
            new Color(0x000000ff),

            new Color(0xFEFEFFff),
            new Color(0xBCDFFFff),
            new Color(0xD1D8FFff),
            new Color(0xE8D1FFff),
            new Color(0xFBCDFDff),
            new Color(0xFFCCE5ff),
            new Color(0xFFCFCAff),
            new Color(0xF8D5B4ff),
            new Color(0xE4DCA8ff),
            new Color(0xCCE3A9ff),
            new Color(0xB9E8B8ff),
            new Color(0xAEE8D0ff),
            new Color(0xAFE5EAff),
            new Color(0xB6B6B6ff),
            new Color(0x000000ff),
            new Color(0x000000ff),
        };
        
        private byte[] ColorIndices = new byte[4];
        
        public Color this[int index]
        {
            get => BasicColors[ColorIndices[index]];
        }

        public byte Read(ushort addr)
        {
            return ColorIndices[addr];
        }

        public void Write(ushort addr, byte data)
        {
            ColorIndices[addr] = data;
        }
    }
}