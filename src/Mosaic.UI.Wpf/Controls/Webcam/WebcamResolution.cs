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
    /// Describes a capture mode supported by a webcam: frame size in pixels and frame rate.
    /// </summary>
    /// <param name="Width">The frame width, in pixels.</param>
    /// <param name="Height">The frame height, in pixels.</param>
    /// <param name="FramesPerSecond">The frame rate, rounded to the nearest whole frame per second (29.97 is reported as 30).</param>
    public sealed record WebcamResolution(int Width, int Height, int FramesPerSecond)
    {
        /// <summary>
        /// Returns a display string such as "1920 × 1080 @ 30 fps".
        /// </summary>
        public override string ToString() => $"{Width} × {Height} @ {FramesPerSecond} fps";
    }
}
