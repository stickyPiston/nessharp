using System;
using Eto.Forms;

namespace NesSharp.Mac
{
	class Program
	{
        public const string LIB = "libgtk-3.dylib";
        public const string LIB_GDK = "libgdk-3.dylib";

        [DllImport(LIB_GDK)]
        static extern IntPtr gdk_quartz_window_get_nsview(IntPtr drawable);

        [DllImport(LIB)]
        static extern IntPtr gtk_widget_get_window(IntPtr widget);

        private static IntPtr GetXID(IntPtr widget) {
            return gdk_quartz_window_get_nsview(gtk_widget_get_window(widget));
        }

		[STAThread]
		public static void Main(string[] args)
		{
            MainForm.Start(Eto.Platforms.Gtk, GetXID);
		}
	}
}
