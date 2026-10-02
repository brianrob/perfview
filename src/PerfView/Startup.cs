using Microsoft.Diagnostics.Utilities;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PerfView
{
    internal static class Startup
    {
#if PERFVIEW_COLLECT
        [MTAThread]
#else
        [STAThread]
#endif
        public static int Main(string[] args)
        {
            return Run(() => OperatingSystemVersion.IsSupported, ReportUnsupported, () => RunApplication(args));
        }

        internal static int Run(Func<bool> isSupported, Action<string> reportUnsupported, Func<int> runApplication)
        {
            bool supported;
            try
            {
                supported = isSupported();
            }
            catch (Win32Exception ex)
            {
                reportUnsupported(ex.Message);
                return 1;
            }

            if (!supported)
            {
                reportUnsupported("This operating system is not supported. " + OperatingSystemVersion.Requirement);
                return 1;
            }

            return runApplication();
        }

        #region private
        // Keep TraceEvent-dependent types out of the unsupported-OS startup path.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int RunApplication(string[] args)
        {
            return App.Main(args);
        }

        private static void ReportUnsupported(string message)
        {
#if PERFVIEW_COLLECT
            Console.Error.WriteLine("PerfViewCollect: " + message);
#else
            MessageBoxW(IntPtr.Zero, message, "PerfView", 0x10);
#endif
        }

#if !PERFVIEW_COLLECT
        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int MessageBoxW(IntPtr owner, string text, string caption, uint type);
#endif
        #endregion
    }
}
