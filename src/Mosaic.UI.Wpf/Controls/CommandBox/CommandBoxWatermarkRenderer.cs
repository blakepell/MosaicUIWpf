/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit.Rendering;
using System.Globalization;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Draws the <see cref="CommandBox.Watermark"/> placeholder text behind an empty document.
    /// </summary>
    /// <remarks>
    /// AvalonEdit has no notion of placeholder text, so the watermark is painted on the background
    /// layer of the <see cref="TextView"/>. It is only drawn while the document is empty, which
    /// means it can never be overdrawn by real content.
    /// </remarks>
    internal sealed class CommandBoxWatermarkRenderer : IBackgroundRenderer
    {
        /// <summary>
        /// The command box the watermark is drawn for.
        /// </summary>
        private readonly CommandBox _owner;

        /// <summary>
        /// Creates a new renderer.
        /// </summary>
        /// <param name="owner">The command box the watermark is drawn for.</param>
        internal CommandBoxWatermarkRenderer(CommandBox owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// The layer the watermark is drawn on.
        /// </summary>
        public KnownLayer Layer => KnownLayer.Background;

        /// <summary>
        /// Draws the watermark when the document is empty.
        /// </summary>
        /// <param name="textView">The text view being rendered.</param>
        /// <param name="drawingContext">The drawing context to render into.</param>
        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            string watermark = _owner.Watermark;

            if (string.IsNullOrEmpty(watermark) || _owner.Document is not { TextLength: 0 })
            {
                return;
            }

            var brush = _owner.WatermarkBrush;

            if (brush == null)
            {
                // No explicit brush, so fade the control foreground back far enough that the
                // watermark reads as a hint rather than as content.
                brush = _owner.Foreground?.Clone();

                if (brush == null)
                {
                    return;
                }

                brush.Opacity *= 0.5;

                if (brush.CanFreeze)
                {
                    brush.Freeze();
                }
            }

            var typeface = new Typeface(_owner.FontFamily, _owner.FontStyle, _owner.FontWeight, _owner.FontStretch);

            var formattedText = new FormattedText(
                watermark,
                CultureInfo.CurrentCulture,
                _owner.FlowDirection,
                typeface,
                _owner.FontSize,
                brush,
                VisualTreeHelper.GetDpi(_owner).PixelsPerDip);

            double y = (textView.DefaultLineHeight - formattedText.Height) / 2.0;

            drawingContext.DrawText(formattedText, new Point(-textView.ScrollOffset.X, Math.Max(0, y) - textView.ScrollOffset.Y));
        }
    }
}
