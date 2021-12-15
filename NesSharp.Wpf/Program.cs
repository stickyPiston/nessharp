using System;
using Eto.Forms;

namespace NesSharp.Wpf
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
            Application application = new Application(Eto.Platforms.Wpf);
            MainForm form = new MainForm();
            form.Show();
            form.Loop(form.panel.NativeHandle);
		}
	}
}
