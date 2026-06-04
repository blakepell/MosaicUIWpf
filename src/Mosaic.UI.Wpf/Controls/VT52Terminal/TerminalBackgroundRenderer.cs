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

namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// Renders terminal cell background colors as full-height rectangles so that
    /// consecutive reversed or colored lines sit seamlessly against each other
    /// without the sub-pixel gap that AvalonEdit's per-run SetBackgroundBrush leaves
    /// (caused by the font's external leading, which is part of each visual line's
    /// height but excluded from individual text-run bounds).
    /// </summary>
    internal class TerminalBackgroundRenderer : IBackgroundRenderer
    {
        private readonly VT52Terminal _terminal;

        public TerminalBackgroundRenderer(VT52Terminal terminal)
        {
            _terminal = terminal;
        }

        /// <summary>
        /// Render at the Background layer so text and the caret draw on top of our rects.
        /// </summary>
        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            double charWidth = textView.WideSpaceWidth;
            double scrollX = textView.ScrollOffset.X;
            double scrollY = textView.ScrollOffset.Y;
            int cols = _terminal.Columns;

            // Snap line edges to the device-pixel grid so that the bottom of one line and the
            // top of the next resolve to the exact same physical pixel. Without this, sub-pixel
            // rounding of fractional line heights leaves a faint transparent seam between
            // consecutive colored (e.g. reversed) lines.
            double dpiScaleY = VisualTreeHelper.GetDpi(textView).DpiScaleY;
            if (dpiScaleY <= 0)
            {
                dpiScaleY = 1.0;
            }

            foreach (var visualLine in textView.VisualLines)
            {
                int docLineNum = visualLine.FirstDocumentLine.LineNumber - 1; // 0-based

                double rawTop = visualLine.VisualTop - scrollY;
                double rawBottom = rawTop + visualLine.Height;
                double top = Math.Round(rawTop * dpiScaleY) / dpiScaleY;
                double bottom = Math.Round(rawBottom * dpiScaleY) / dpiScaleY;
                double lineHeight = bottom - top;

                // Walk cells, emitting a rectangle for each run of identical background.
                Brush? runBrush = null;
                int runStart = 0;

                for (int col = 0; col <= cols; col++)
                {
                    Brush? cellBrush = col < cols
                        ? GetEffectiveBackground(_terminal.GetCellAttributes(docLineNum, col))
                        : null; // sentinel to flush last run

                    if (!ReferenceEquals(cellBrush, runBrush))
                    {
                        if (runBrush != null)
                        {
                            drawingContext.DrawRectangle(runBrush, null,
                                new Rect(
                                    runStart * charWidth - scrollX,
                                    top,
                                    (col - runStart) * charWidth,
                                    lineHeight));
                        }

                        runBrush = cellBrush;
                        runStart = col;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the effective background brush for a cell, accounting for reverse video.
        /// Returns <see langword="null"/> when the cell uses the terminal's default background
        /// (transparent — the control's Background brush shows through).
        /// </summary>
        internal static Brush? GetEffectiveBackground(TerminalAttributes attrs)
        {
            byte fg = attrs.Foreground;
            byte bg = attrs.Background;
            bool fgDefault = attrs.DefaultForeground;
            bool bgDefault = attrs.DefaultBackground;

            if (attrs.Reverse)
            {
                // Resolve default sentinel values to their concrete palette indices before swapping.
                if (fgDefault) fg = 7; // default fg = index 7 (light gray)
                if (bgDefault) bg = 0; // default bg = index 0 (black)
                (fg, bg) = (bg, fg);
                bgDefault = false; // resolved + swapped → always explicit now
            }

            return bgDefault ? null : VT52Colorizer.GetPaletteBrush(bg);
        }
    }
}
