using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace HazzMusicBingo.Services;

public static class MonitorHelper
{
    private static readonly IntPtr HWND_TOP = IntPtr.Zero;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    public static bool HasSecondaryMonitor =>
        Forms.Screen.AllScreens.Length > 1;

    public static void ConfigureAudienceWindow(Window window)
    {
        var screens = Forms.Screen.AllScreens;

        if (screens.Length > 1)
        {
            // A real second display exists: make the audience output full-screen there.
            var target = screens.First(screen => !screen.Primary);
            var bounds = target.Bounds;

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.ShowInTaskbar = false;

            window.SourceInitialized += (_, _) =>
            {
                var handle = new WindowInteropHelper(window).Handle;
                SetWindowPos(
                    handle,
                    HWND_TOP,
                    bounds.Left,
                    bounds.Top,
                    bounds.Width,
                    bounds.Height,
                    SWP_SHOWWINDOW);
            };
        }
        else
        {
            // No second display: NEVER take over the host screen.
            // "Show Audience Screen" opens this as a normal floating preview window.
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.WindowStyle = WindowStyle.SingleBorderWindow;
            window.ResizeMode = ResizeMode.CanResize;
            window.ShowInTaskbar = true;
            window.Width = 900;
            window.Height = 560;
            window.MinWidth = 640;
            window.MinHeight = 400;
        }
    }
}
