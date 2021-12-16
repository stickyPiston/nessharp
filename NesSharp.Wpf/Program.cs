using System;
using Eto.Forms;

namespace NesSharp.Wpf
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
            new Application(Eto.Platforms.Wpf).Run(new MainForm(ptr => ptr));
		}
	}
}
