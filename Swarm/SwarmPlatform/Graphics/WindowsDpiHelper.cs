using System.Runtime.InteropServices;

namespace SwarmPlatform.Graphics
{
    public static class WindowsDpiHelper
    {
        [DllImport("user32.dll", SetLastError = true)] static extern bool SetProcessDpiAwarenessContext(int dpiFlag);
        [DllImport("SHCore.dll", SetLastError = true)] static extern bool SetProcessDpiAwareness(int awareness);
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        public static bool TryEnableDpiAwareness()
        {
            const int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = 34;
            try { return SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); }
            catch { }

            const int PROCESS_DPI_AWARENESS_PROCESS_PER_MONITOR_DPI_AWARE = 2;
            try { return SetProcessDpiAwareness(PROCESS_DPI_AWARENESS_PROCESS_PER_MONITOR_DPI_AWARE); }
            catch { }

            try { return SetProcessDPIAware(); }
            catch { }

            return false;
        }
    }
}
