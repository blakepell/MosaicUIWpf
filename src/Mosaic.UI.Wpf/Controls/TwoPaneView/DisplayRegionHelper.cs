namespace Mosaic.UI.Wpf.Controls
{
    internal class DisplayRegionHelper
    {
        public static Rect WindowRect(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var rect))
            {
                return Rect.Empty;
            }

            return rect.AsRect();
        }

        internal static DisplayRegionHelperInfo GetRegionInfo(IntPtr hwnd)
        {
            var info = new DisplayRegionHelperInfo(TwoPaneViewMode.SinglePane);

            if (hwnd != IntPtr.Zero && _isGetContentRectsSupported)
            {
                var rects = GetRegions(hwnd);
                if (rects != null && rects.Count == 2)
                {
                    info.Regions[0] = rects[0];
                    info.Regions[1] = rects[1];

                    // Determine orientation. If neither of these are true, default to doing nothing.
                    if (info.Regions[0].X < info.Regions[1].X && info.Regions[0].Y == info.Regions[1].Y)
                    {
                        // Double portrait
                        info.Mode = TwoPaneViewMode.Wide;
                    }
                    else if (info.Regions[0].X == info.Regions[1].X && info.Regions[0].Y < info.Regions[1].Y)
                    {
                        // Double landscape
                        info.Mode = TwoPaneViewMode.Tall;
                    }
                }
            }
            return info;
        }

        static bool _isGetContentRectsSupported = true;

        private static List<Rect> GetRegions(IntPtr hwnd)
        {
            try
            {
                uint count = 2;
                var regions = new RECT[2];
                var result = GetContentRects(hwnd, ref count, regions);
                if (result)
                {
                    var rects = new List<Rect>((int)count);
                    for (var i = 0; i < (int)count; i++)
                    {
                        rects.Add(regions[i].AsRect());
                    }
                    return rects;
                }
            }
            catch (EntryPointNotFoundException) // Expected to throw on older OS
            {
                _isGetContentRectsSupported = false;
            }
            return null;
        }


        [DllImport("user32.dll")]
        [System.Security.SuppressUnmanagedCodeSecurity()]
        private static extern bool GetContentRects(IntPtr hwnd, ref UInt32 count, [MarshalAs(UnmanagedType.LPArray)] RECT[] rects);

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window.
        /// The dimensions are given in screen coordinates that are relative to the upper-left corner of the screen.
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lpRect"></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        [System.Security.SuppressUnmanagedCodeSecurity()]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public Rect AsRect() => new(Left, Top, Right - Left, Top - Bottom);
        }
    }
}
