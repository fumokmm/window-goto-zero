using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WindowGotoZero.Native;

namespace WindowGotoZero.Services;

internal sealed class WindowService
{
    public IReadOnlyList<WindowInfo> EnumerateTopLevelWindows()
    {
        var results = new List<WindowInfo>();
        var shell = WindowNative.GetShellWindow();
        var currentProcessId = (uint)Environment.ProcessId;

        WindowNative.EnumWindows((hWnd, lParam) =>
        {
            if (hWnd == shell)
            {
                return true;
            }

            if (!WindowNative.IsWindowVisible(hWnd))
            {
                return true;
            }

            var length = WindowNative.GetWindowTextLength(hWnd);
            if (length <= 0)
            {
                return true;
            }

            var builder = new StringBuilder(length + 1);
            WindowNative.GetWindowText(hWnd, builder, builder.Capacity);
            var title = builder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            WindowNative.GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == currentProcessId)
            {
                return true;
            }

            var processName = ResolveProcessName(processId);
            WindowNative.GetWindowRect(hWnd, out var rect);

            results.Add(new WindowInfo
            {
                Handle = hWnd,
                Title = title,
                ProcessName = processName,
                ProcessId = (int)processId,
                Left = rect.Left,
                Top = rect.Top,
                Width = rect.Width,
                Height = rect.Height,
            });

            return true;
        }, IntPtr.Zero);

        return results
            .OrderBy(w => w.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public void MoveToPrimaryOrigin(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid window handle.", nameof(hWnd));
        }

        // Maximized/minimized windows do not move reliably until restored.
        if (WindowNative.IsIconic(hWnd) || WindowNative.IsZoomed(hWnd))
        {
            WindowNative.ShowWindow(hWnd, WindowNative.SwRestore);
        }

        var moved = WindowNative.SetWindowPos(
            hWnd,
            IntPtr.Zero,
            x: 0,
            y: 0,
            cx: 0,
            cy: 0,
            WindowNative.SwpNosize | WindowNative.SwpNozorder | WindowNative.SwpShowwindow | WindowNative.SwpFramechanged);

        if (!moved)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SetWindowPos failed (Win32 error {error}).");
        }

        WindowNative.SetForegroundWindow(hWnd);
    }

    private static string ResolveProcessName(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }
}
