using System;
using Eto.Forms;

namespace NesSharp.Mac
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
            MainForm.Start(Eto.Platforms.Mac64, ptr => ptr);
		}
	}
}
