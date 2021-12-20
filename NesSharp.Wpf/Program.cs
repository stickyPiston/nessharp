using System;
using Eto.Forms;

namespace NesSharp.Wpf
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
            MainForm.Start(Eto.Platforms.Wpf);
		}
	}
}
