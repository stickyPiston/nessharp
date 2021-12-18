using System;
using Eto.Forms;

using System.Runtime.InteropServices;

namespace NesSharp.Mac
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
            MainForm.Start(Eto.Platforms.Mac64);
		}
	}
}
