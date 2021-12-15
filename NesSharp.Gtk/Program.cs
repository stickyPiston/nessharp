using System;
using Eto.Forms;

using System.Runtime.InteropServices;

namespace NesSharp.Gtk
{
	class Program
	{

        public const string LIB = "libgtk-3.so.0";
        public const string LIB_GDK = "libgdk-3.so.0";

        [DllImport(LIB_GDK)]
        static extern IntPtr gdk_x11_window_get_xid(IntPtr drawable);

        [DllImport(LIB)]
        static extern IntPtr gtk_widget_get_window(IntPtr widget);

		[STAThread]
		public static void Main(string[] args)
		{
            Application application = new Application(Eto.Platforms.Gtk);
            MainForm form = new MainForm();
            form.Show();
            form.Loop(gdk_x11_window_get_xid(gtk_widget_get_window(form.panel.NativeHandle)));
		}
	}
}
