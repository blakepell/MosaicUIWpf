/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A native media type offered by a capture device.
    /// </summary>
    /// <param name="Index">The native media type index passed to IMFSourceReader::GetNativeMediaType.</param>
    /// <param name="Subtype">The Media Foundation video subtype (NV12, YUY2, MJPG, ...).</param>
    /// <param name="Width">The frame width, in pixels.</param>
    /// <param name="Height">The frame height, in pixels.</param>
    /// <param name="FrameRateNumerator">The frame-rate numerator.</param>
    /// <param name="FrameRateDenominator">The frame-rate denominator.</param>
    internal readonly record struct NativeVideoMode(int Index, Guid Subtype, int Width, int Height, uint FrameRateNumerator, uint FrameRateDenominator)
    {
        public int FramesPerSecond => FrameRateDenominator == 0 ? 0 : (int)Math.Round(FrameRateNumerator / (double)FrameRateDenominator);

        public long PixelCount => (long)Width * Height;

        public bool IsUsable => Width > 0 && Height > 0 && FramesPerSecond > 0;

        public WebcamResolution ToResolution() => new(Width, Height, FramesPerSecond);
    }

    /// <summary>
    /// Chooses which native capture mode to open.  Pure logic, independent of Media Foundation.
    /// </summary>
    internal static class WebcamModeSelector
    {
        /// <summary>
        /// The mainstream mode automatic selection aims for: good quality without the CPU cost of 4K or
        /// high-frame-rate modes that a preview does not benefit from.
        /// </summary>
        public static readonly WebcamResolution PreferredResolution = new(1920, 1080, 30);

        private const int MinimumSmoothFramesPerSecond = 25;
        private const int MinimumUsableFramesPerSecond = 15;

        /// <summary>
        /// Orders the usable modes from most to least preferred.  The camera tries them in order, so later
        /// entries are fallbacks if the pipeline cannot be built for an earlier one.
        /// </summary>
        /// <param name="modes">The native modes offered by the device.</param>
        /// <param name="requested">The mode requested by the consumer, or <see langword="null"/> for automatic selection.</param>
        public static List<NativeVideoMode> OrderCandidates(IReadOnlyList<NativeVideoMode> modes, WebcamResolution? requested)
        {
            var candidates = new List<NativeVideoMode>(modes.Count);

            foreach (var mode in modes)
            {
                if (mode.IsUsable)
                {
                    candidates.Add(mode);
                }
            }

            if (requested is null)
            {
                candidates.Sort(CompareAutomatic);
            }
            else
            {
                candidates.Sort((a, b) => CompareToRequested(a, b, requested));
            }

            return candidates;
        }

        /// <summary>
        /// Returns the distinct resolutions offered by the device, largest first.
        /// </summary>
        public static List<WebcamResolution> GetDistinctResolutions(IReadOnlyList<NativeVideoMode> modes)
        {
            var seen = new HashSet<WebcamResolution>();
            var list = new List<WebcamResolution>();

            foreach (var mode in modes)
            {
                if (mode.IsUsable && seen.Add(mode.ToResolution()))
                {
                    list.Add(mode.ToResolution());
                }
            }

            list.Sort(static (a, b) =>
            {
                int result = ((long)b.Width * b.Height).CompareTo((long)a.Width * a.Height);
                return result != 0 ? result : b.FramesPerSecond.CompareTo(a.FramesPerSecond);
            });

            return list;
        }

        private static int CompareAutomatic(NativeVideoMode a, NativeVideoMode b)
        {
            int result = Tier(a).CompareTo(Tier(b));

            if (result != 0)
            {
                return result;
            }

            // Within the "too large" tier the smallest oversize mode is best; otherwise larger is better.
            result = Tier(a) == 2 ? a.PixelCount.CompareTo(b.PixelCount) : b.PixelCount.CompareTo(a.PixelCount);

            if (result != 0)
            {
                return result;
            }

            result = FrameRatePenalty(a.FramesPerSecond, PreferredResolution.FramesPerSecond)
                .CompareTo(FrameRatePenalty(b.FramesPerSecond, PreferredResolution.FramesPerSecond));

            return result != 0 ? result : CompareSubtypeThenIndex(a, b);
        }

        private static int CompareToRequested(NativeVideoMode a, NativeVideoMode b, WebcamResolution requested)
        {
            // Exact matches first, then the closest frame size, then the closest frame rate.
            long requestedPixels = (long)requested.Width * requested.Height;
            int result = IsExact(b, requested).CompareTo(IsExact(a, requested));

            if (result != 0)
            {
                return result;
            }

            result = Math.Abs(a.PixelCount - requestedPixels).CompareTo(Math.Abs(b.PixelCount - requestedPixels));

            if (result != 0)
            {
                return result;
            }

            result = FrameRatePenalty(a.FramesPerSecond, requested.FramesPerSecond)
                .CompareTo(FrameRatePenalty(b.FramesPerSecond, requested.FramesPerSecond));

            return result != 0 ? result : CompareSubtypeThenIndex(a, b);
        }

        private static bool IsExact(NativeVideoMode mode, WebcamResolution requested)
        {
            return mode.Width == requested.Width && mode.Height == requested.Height && mode.FramesPerSecond == requested.FramesPerSecond;
        }

        /// <summary>
        /// 0: fits within 1080p and is smooth; 1: fits and is usable; 2: larger than 1080p; 3: too slow.
        /// </summary>
        private static int Tier(NativeVideoMode mode)
        {
            long preferredPixels = (long)PreferredResolution.Width * PreferredResolution.Height;

            if (mode.FramesPerSecond < MinimumUsableFramesPerSecond)
            {
                return 3;
            }

            if (mode.PixelCount > preferredPixels)
            {
                return 2;
            }

            return mode.FramesPerSecond >= MinimumSmoothFramesPerSecond ? 0 : 1;
        }

        /// <summary>
        /// Rates distance from a target frame rate, penalizing rates below the target twice as much as
        /// rates above it (a 60 fps mode is preferable to a 15 fps mode when 30 is unavailable).
        /// </summary>
        private static int FrameRatePenalty(int framesPerSecond, int target)
        {
            return framesPerSecond >= target ? framesPerSecond - target : (target - framesPerSecond) * 2;
        }

        private static int CompareSubtypeThenIndex(NativeVideoMode a, NativeVideoMode b)
        {
            int result = SubtypeRank(a.Subtype).CompareTo(SubtypeRank(b.Subtype));
            return result != 0 ? result : a.Index.CompareTo(b.Index);
        }

        /// <summary>
        /// Prefers formats that are cheapest to turn into RGB32: RGB32 needs no conversion, NV12/YUY2 need
        /// only a color conversion, and MJPG additionally needs a JPEG decode.
        /// </summary>
        private static int SubtypeRank(Guid subtype)
        {
            if (subtype == MediaFoundationInterop.MFVideoFormat_RGB32)
            {
                return 0;
            }

            if (subtype == MediaFoundationInterop.MFVideoFormat_NV12)
            {
                return 1;
            }

            if (subtype == MediaFoundationInterop.MFVideoFormat_YUY2)
            {
                return 2;
            }

            return subtype == MediaFoundationInterop.MFVideoFormat_MJPG ? 3 : 4;
        }
    }
}
