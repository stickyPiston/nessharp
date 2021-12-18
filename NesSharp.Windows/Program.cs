using System;
using Eto.Forms;

namespace NesSharp.Wpf
{
	class Program
	{
        public const string LIB = "libgtk-3-0.dll";
        public const string LIB_GDK = "libgdk-3-0.dll";

        [DllImport(LIB_GDK)]
        static extern IntPtr gdk_win32_window_get_handle(IntPtr drawable);

        [DllImport(LIB)]
        static extern IntPtr gtk_widget_get_window(IntPtr widget);

        private static IntPtr GetXID(IntPtr widget) {
            return gdk_win32_window_get_handle(gtk_widget_get_window(widget));
        }

		[STAThread]
		public static void Main(string[] args)
		{
            MainForm.Start(Eto.Platforms.Gtk, GetXID);
		}
	}
}
