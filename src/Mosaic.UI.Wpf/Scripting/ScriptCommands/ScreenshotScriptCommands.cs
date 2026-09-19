/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Drawing;
using System.Windows.Forms;

namespace Mosaic.UI.Wpf.Scripting.ScriptCommands;

/// <summary>
/// Captures desktop regions as caller-owned bitmaps.
/// </summary>
[ScriptModule(Name = "screenshot")]
public class ScreenshotScriptCommands
{
    /// <summary>
    /// Captures the foreground window. The caller must dispose the returned bitmap.
    /// </summary>
    public Bitmap CurrentWindow()
    {
        if (!GetWindowRect(GetForegroundWindow(), out var rect))
        {
            throw new Win32Exception();
        }

        return ByLocation(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    /// <summary>
    /// Captures the primary screen. The caller must dispose the returned bitmap.
    /// </summary>
    public Bitmap PrimaryScreen()
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? throw new InvalidOperationException("No primary screen is available.");
        return ByLocation(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }

    /// <summary>
    /// Captures a desktop rectangle in physical pixels, including negative coordinates on other monitors.
    /// </summary>
    public Bitmap ByLocation(int x, int y, int width, int height)
    {
        var bitmap = new Bitmap(width, height);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(x, y, 0, 0, bitmap.Size);
            return bitmap;
        }
        catch { bitmap.Dispose(); throw; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);
}