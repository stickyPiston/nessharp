using System;
using Eto.Forms;

namespace NesSharp.Mac
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
            Application application = new Application(Eto.Platforms.Mac64);
            MainForm form = new MainForm();
            form.Show();
            form.Loop(form.panel.NativeHandle);
		}
	}
}
