using System;
using Eto.Forms;

using System.Runtime.InteropServices;

namespace NesSharp.Gtk
{
	class Program
	{

        [System.Runtime.InteropServices.DllImport("X11")]
        extern public static int XInitThreads();

		[STAThread]
		public static void Main(string[] args)
		{
            XInitThreads();
            MainForm.Start(Eto.Platforms.Gtk);
		}
	}
}
