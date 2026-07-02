using System.Windows.Interop;

namespace Mosaic.UI.Wpf.Common
{
    /// <summary>
    /// Exposes Win32 interop methods and constants used by the library.
    /// </summary>
    public static class Win32
    {
        /// <summary>
        /// The User32 native library name.
        /// </summary>
        public const string User32 = "user32.dll";

        /// <summary>
        /// The Gdi32 native library name.
        /// </summary>
        public const string Gdi32 = "gdi32.dll";

        /// <summary>
        /// The GdiPlus native library name.
        /// </summary>
        public const string GdiPlus = "gdiplus.dll";

        /// <summary>
        /// The Kernel32 native library name.
        /// </summary>
        public const string Kernel32 = "kernel32.dll";

        /// <summary>
        /// The Shell32 native library name.
        /// </summary>
        public const string Shell32 = "shell32.dll";

        /// <summary>
        /// The MsImg native library name.
        /// </summary>
        public const string MsImg = "msimg32.dll";

        /// <summary>
        /// The NTDll native library name.
        /// </summary>
        public const string NTdll = "ntdll.dll";

        /// <summary>
        /// The DwmApi native library name.
        /// </summary>
        public const string DwmApi = "dwmapi.dll";

        /// <summary>
        /// The Winmm native library name.
        /// </summary>
        public const string Winmm = "winmm.dll";

        /// <summary>
        /// The Shcore native library name.
        /// </summary>
        public const string Shcore = "shcore.dll";

        /// <summary>
        /// Represents a callback used when enumerating top-level windows.
        /// </summary>
        /// <param name="hwnd">The handle of the enumerated window.</param>
        /// <param name="lParam">An application-defined value.</param>
        /// <returns><see langword="true" /> to continue enumeration; otherwise, <see langword="false" />.</returns>
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        /// <summary>
        /// Finds a top-level window by class name and window name.
        /// </summary>
        /// <param name="className">The class name of the window, or <see langword="null" />.</param>
        /// <param name="winName">The window name, or <see langword="null" />.</param>
        /// <returns>The window handle.</returns>
        [DllImport(User32)]
        public static extern IntPtr FindWindow(string className, string winName);

        /// <summary>
        /// Sends a message to a window and waits for the response.
        /// </summary>
        /// <param name="hwnd">The target window handle.</param>
        /// <param name="msg">The message identifier.</param>
        /// <param name="wParam">The first message parameter.</param>
        /// <param name="lParam">The second message parameter.</param>
        /// <param name="fuFlage">The timeout behavior flags.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <param name="result">When this method returns, contains the result value.</param>
        /// <returns>The result of the message send.</returns>
        [DllImport(User32)]
        public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam,
            uint fuFlage, uint timeout, IntPtr result);

        /// <summary>
        /// Sends a message to a window.
        /// </summary>
        /// <param name="hWnd">The target window handle.</param>
        /// <param name="wMsg">The message identifier.</param>
        /// <param name="wParam">The first message parameter.</param>
        /// <param name="lParam">The second message parameter.</param>
        /// <returns>The message result.</returns>
        [DllImport(User32)]
        internal static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Enumerates all top-level windows.
        /// </summary>
        /// <param name="proc">The callback used to process each window.</param>
        /// <param name="lParam">An application-defined value.</param>
        /// <returns><see langword="true" /> if the enumeration succeeds; otherwise, <see langword="false" />.</returns>
        [DllImport(User32)]
        public static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);

        /// <summary>
        /// Finds a child window by class name and window name.
        /// </summary>
        /// <param name="hwndParent">The parent window handle.</param>
        /// <param name="hwndChildAfter">The child window handle to begin after.</param>
        /// <param name="className">The class name of the child window, or <see langword="null" />.</param>
        /// <param name="winName">The window name, or <see langword="null" />.</param>
        /// <returns>The child window handle.</returns>
        [DllImport(User32)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className,
            string winName);

        /// <summary>
        /// Shows or hides a window.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="nCmdShow">The show state.</param>
        /// <returns><see langword="true" /> if the function succeeds; otherwise, <see langword="false" />.</returns>
        [DllImport(User32)]
        public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        /// <summary>
        /// Changes the parent of a window.
        /// </summary>
        /// <param name="hwnd">The child window handle.</param>
        /// <param name="parentHwnd">The new parent window handle.</param>
        /// <returns>The previous parent window handle.</returns>
        [DllImport(User32)]
        public static extern IntPtr SetParent(IntPtr hwnd, IntPtr parentHwnd);

        /// <summary>
        /// Sets the value of a window's long pointer.
        /// </summary>
        /// <param name="hWnd">The window handle.</param>
        /// <param name="nIndex">The zero-based offset to the value to set.</param>
        /// <param name="dwNewLong">The replacement value.</param>
        /// <returns>The previous value.</returns>
        [DllImport(User32)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <summary>
        /// Brings a window to the foreground.
        /// </summary>
        /// <param name="hWnd">The window handle.</param>
        /// <returns><see langword="true" /> if the call succeeds; otherwise, <see langword="false" />.</returns>
        [DllImport(User32)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Posts a message to a window.
        /// </summary>
        /// <param name="hWnd">The target window handle.</param>
        /// <param name="Msg">The message identifier.</param>
        /// <param name="wParam">The first message parameter.</param>
        /// <param name="lParam">The second message parameter.</param>
        /// <returns><see langword="true" /> if the call succeeds; otherwise, <see langword="false" />.</returns>
        [DllImport(User32)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Gets the text of a window.
        /// </summary>
        /// <param name="hWnd">The window handle.</param>
        /// <param name="lpString">The buffer that receives the text.</param>
        /// <param name="nMaxCount">The maximum number of characters to copy.</param>
        /// <returns>The number of characters copied.</returns>
        [DllImport(User32)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Sends an MCI command string.
        /// </summary>
        /// <param name="strCommand">The command string.</param>
        /// <param name="strReturn">The buffer that receives the return string.</param>
        /// <param name="iReturnLength">The length of the return buffer.</param>
        /// <param name="hwndCallback">The callback window handle.</param>
        /// <returns>The MCI result code.</returns>
        [DllImport(Winmm)]
        public static extern long mciSendString(string strCommand, StringBuilder strReturn,
            int iReturnLength, IntPtr hwndCallback);

        #region WINAPI DLL Imports

        /// <summary>
        /// Selects a GDI object into a device context.
        /// </summary>
        /// <param name="hdc">The device context handle.</param>
        /// <param name="hgdiobj">The GDI object handle.</param>
        /// <returns>The previously selected object handle.</returns>
        [DllImport(Gdi32, ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        /// <summary>
        /// Creates a bitmap compatible with a device context.
        /// </summary>
        /// <param name="hdc">The device context handle.</param>
        /// <param name="nWidth">The bitmap width.</param>
        /// <param name="nHeight">The bitmap height.</param>
        /// <returns>The bitmap handle.</returns>
        [DllImport(Gdi32)]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        /// <summary>
        /// Creates a memory device context compatible with a device context.
        /// </summary>
        /// <param name="hdc">The device context handle.</param>
        /// <returns>The memory device context handle.</returns>
        [DllImport(Gdi32, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        /// <summary>
        /// Deletes a GDI object.
        /// </summary>
        /// <param name="hObject">The GDI object handle.</param>
        /// <returns><see langword="true" /> if the object is deleted; otherwise, <see langword="false" />.</returns>
        [DllImport(Gdi32)]
        public static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// Creates a region with rounded corners.
        /// </summary>
        /// <param name="nLeftRect">The x-coordinate of the upper-left corner.</param>
        /// <param name="nTopRect">The y-coordinate of the upper-left corner.</param>
        /// <param name="nRightRect">The x-coordinate of the lower-right corner.</param>
        /// <param name="nBottomRect">The y-coordinate of the lower-right corner.</param>
        /// <param name="nWidthEllipse">The width of the ellipse used for the rounded corners.</param>
        /// <param name="nHeightEllipse">The height of the ellipse used for the rounded corners.</param>
        /// <returns>The region handle.</returns>
        [DllImport(Gdi32, SetLastError = true)]
        public static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse);

        /// <summary>
        /// Creates a bitmap from raw pixel data.
        /// </summary>
        /// <param name="nWidth">The bitmap width.</param>
        /// <param name="nHeight">The bitmap height.</param>
        /// <param name="cPlanes">The number of color planes.</param>
        /// <param name="cBitsPerPel">The number of bits per pixel.</param>
        /// <param name="lpvBits">The pixel data.</param>
        /// <returns>The bitmap handle.</returns>
        [DllImport(Gdi32)]
        public static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel,
            IntPtr lpvBits);

        /// <summary>
        /// Gets the device context for a window.
        /// </summary>
        /// <param name="hWnd">The window handle.</param>
        /// <returns>The device context handle.</returns>
        [DllImport(User32)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        /// <summary>
        /// Gets a device capability value.
        /// </summary>
        /// <param name="hdc">The device context handle.</param>
        /// <param name="nIndex">The capability index.</param>
        /// <returns>The requested capability value.</returns>
        [DllImport(Gdi32)]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        /// <summary>
        /// Releases a device context.
        /// </summary>
        /// <param name="hWnd">The window handle.</param>
        /// <param name="hDC">The device context handle.</param>
        /// <returns>The result of the release operation.</returns>
        [DllImport(User32)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);


        /// <summary>
        /// Deletes a device context.
        /// </summary>
        /// <param name="hDc">The device context handle.</param>
        /// <returns>The deleted device context handle.</returns>
        [DllImport(Gdi32, EntryPoint = "DeleteDC")]
        public static extern IntPtr DeleteDC(IntPtr hDc);


        public const int SM_CXSCREEN = 0;

        public const int SM_CYSCREEN = 1;

        /// <summary>
        /// Gets the desktop window handle.
        /// </summary>
        /// <returns>The desktop window handle.</returns>
        [DllImport(User32, EntryPoint = "GetDesktopWindow")]
        public static extern IntPtr GetDesktopWindow();

        /// <summary>
        /// Gets a system metric value.
        /// </summary>
        /// <param name="abc">The metric index.</param>
        /// <returns>The requested system metric.</returns>
        [DllImport(User32, EntryPoint = "GetSystemMetrics")]
        public static extern int GetSystemMetrics(int abc);

        /// <summary>
        /// Gets the device context for a window, including the nonclient area.
        /// </summary>
        /// <param name="ptr">The window handle.</param>
        /// <returns>The device context handle.</returns>
        [DllImport(User32, EntryPoint = "GetWindowDC")]
        public static extern IntPtr GetWindowDC(int ptr);

        /// <summary>
        /// Stores a desktop size.
        /// </summary>
        public struct DeskTopSize
        {
            public int cx;
            public int cy;
        }

        public enum TernaryRasterOperations : uint
        {
            /// <summary>dest = source</summary>
            SRCCOPY = 0x00CC0020,

            /// <summary>dest = source OR dest</summary>
            SRCPAINT = 0x00EE0086,

            /// <summary>dest = source AND dest</summary>
            SRCAND = 0x008800C6,

            /// <summary>dest = source XOR dest</summary>
            SRCINVERT = 0x00660046,

            /// <summary>dest = source AND (NOT dest)</summary>
            SRCERASE = 0x00440328,

            /// <summary>dest = (NOT source)</summary>
            NOTSRCCOPY = 0x00330008,

            /// <summary>dest = (NOT src) AND (NOT dest)</summary>
            NOTSRCERASE = 0x001100A6,

            /// <summary>dest = (source AND pattern)</summary>
            MERGECOPY = 0x00C000CA,

            /// <summary>dest = (NOT source) OR dest</summary>
            MERGEPAINT = 0x00BB0226,

            /// <summary>dest = pattern</summary>
            PATCOPY = 0x00F00021,

            /// <summary>dest = DPSnoo</summary>
            PATPAINT = 0x00FB0A09,

            /// <summary>dest = pattern XOR dest</summary>
            PATINVERT = 0x005A0049,

            /// <summary>dest = (NOT dest)</summary>
            DSTINVERT = 0x00550009,

            /// <summary>dest = BLACK</summary>
            BLACKNESS = 0x00000042,

            /// <summary>dest = WHITE</summary>
            WHITENESS = 0x00FF0062
        }

        /// <summary>
        /// Copies a bitmap from one device context to another.
        /// </summary>
        /// <param name="hdc">The destination device context.</param>
        /// <param name="nXDest">The x-coordinate of the destination rectangle.</param>
        /// <param name="nYDest">The y-coordinate of the destination rectangle.</param>
        /// <param name="nWidth">The width of the destination rectangle.</param>
        /// <param name="nHeight">The height of the destination rectangle.</param>
        /// <param name="hdcSrc">The source device context.</param>
        /// <param name="nXSrc">The x-coordinate of the source rectangle.</param>
        /// <param name="nYSrc">The y-coordinate of the source rectangle.</param>
        /// <param name="dwRop">The raster operation code.</param>
        /// <returns><see langword="true" /> if the operation succeeds; otherwise, <see langword="false" />.</returns>
        [DllImport(Gdi32)]
        public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc,
            int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        #endregion

        /// <summary>   
        /// 设置鼠标的坐标   
        /// </summary>   
        /// <param name="x">横坐标</param>   
        /// <param name="y">纵坐标</param>   
        [DllImport(User32)]
        public extern static void SetCursorPos(int x, int y);

        [DllImport(DwmApi)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttributes attr, ref int attrValue, int attrSize);

        /// <summary>
        /// Enables or disables immersive dark mode for a window.
        /// </summary>
        /// <param name="source">The window source.</param>
        /// <param name="enable"><see langword="true" /> to enable dark mode; otherwise, <see langword="false" />.</param>
        /// <returns><see langword="true" /> if the call succeeds; otherwise, <see langword="false" />.</returns>
        public static bool EnableDarkModeForWindow(HwndSource source, bool enable)
        {
            int darkMode = enable ? 1 : 0;
            int hr = DwmSetWindowAttribute(source.Handle, DwmWindowAttributes.UseImmersiveDarkMode, ref darkMode, sizeof(int));
            return hr >= 0;
        }

        [DllImport(DwmApi)]
        public static extern int DwmSetIconicThumbnail(IntPtr hwnd, IntPtr hbmp, int dwSITFlags);

        [DllImport(DwmApi)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttributes dwAttribute, IntPtr pvAttribute, int cbAttribute);

        [DllImport(DwmApi)]
        public static extern int DwmInvalidateIconicBitmaps(IntPtr hwnd);

        [DllImport(DwmApi)]
        public static extern int DwmSetIconicLivePreviewBitmap(IntPtr hwnd, IntPtr hBitmap, IntPtr pptClient, DWM_SIT dwSITFlags);

        [DllImport(User32)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport(User32, SetLastError = true)]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport(User32)]
        public static extern int GetDpiForWindow(IntPtr hwnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport(Kernel32)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport(Kernel32)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport(Kernel32)]
        public static extern bool FreeLibrary(IntPtr hModule);
    }

    internal class WindowsMessageCodes
    {
        public const int SC_RESTORE = 0xF120;
        public const int SC_MINIMIZE = 0xF020;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int WM_DEADCHAR = 0x0024;
        public const int WM_NCLBUTTONDOWN = 0x00A1;
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_NCCALCSIZE = 0x0083;

        public const int WM_DWMSENDICONICTHUMBNAIL = 0x0323;
        public const int WM_DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326;
    }

    [Flags]
    public enum DwmWindowAttributes : uint
    {
        None = 0,
        DISPLAYFRAME = 1,
        FORCE_ICONIC_REPRESENTATION = 7,
        HAS_ICONIC_BITMAP = 10,
        UseImmersiveDarkMode = 20,
        WindowCornerPreference = 33
    }

    /// <summary>
    /// DWM corner preference values for top-level windows.
    /// </summary>
    public enum DwmWindowCornerPreference
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3
    }

    [Flags]
    public enum DWM_SIT : uint
    {
        None = 0x0,
        DisplayFrame = 0x1
    }
}
