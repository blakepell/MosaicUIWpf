/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using System.Globalization;

namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// AvalonEdit background render that draws a block caret.
    /// </summary>
    public class BlockCaretRenderer : IBackgroundRenderer
    {
        /// <summary>
        /// The text area to render the block caret for.
        /// </summary>
        private readonly TextArea _textArea;
        private Typeface? _cachedTypeface;
        private FontFamily? _cachedFontFamily;
        private FontStyle _cachedFontStyle;
        private FontWeight _cachedFontWeight;
        private FontStretch _cachedFontStretch;

        /// <summary>
        /// Create a new block caret renderer.
        /// </summary>
        /// <param name="textArea"></param>
        public BlockCaretRenderer(TextArea textArea)
        {
            _textArea = textArea;
        }

        /// <summary>
        /// The layer to render the block caret on.
        /// </summary>
        public KnownLayer Layer => KnownLayer.Caret;

        /// <summary>
        /// Gets or sets whether the block caret is drawn. Set to <see langword="false"/> to honor
        /// the DECTCEM (hide cursor) terminal mode.
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets the terminal-controlled position of the block caret.
        /// </summary>
        internal TextViewPosition Position { get; set; } = new(1, 1);

        /// <summary>
        /// Draw the block caret.
        /// </summary>
        /// <param name="textView"></param>
        /// <param name="drawingContext"></param>
        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (!Visible)
            {
                return;
            }

            // Determine the visual position of the caret.
            var pos = Position;
            var point = textView.GetVisualPosition(pos, VisualYPosition.LineTop) - textView.ScrollOffset;
            var rect = new Rect(point, new Size(textView.WideSpaceWidth, textView.DefaultLineHeight));

            // Get the character under the caret, if any
            char caretChar = ' ';
            if (pos.Line <= _textArea.Document.LineCount)
            {
                var line = _textArea.Document.GetLineByNumber(pos.Line);
                int offset = line.Offset + pos.Column - 1;
                if (offset >= line.Offset && offset < line.EndOffset)
                {
                    caretChar = _textArea.Document.GetCharAt(offset);
                }
            }

            // Draw a rectangle (filled with white) to represent the block caret background.
            drawingContext.DrawRectangle(Brushes.White, null, rect);

            // Draw the character under the caret in black (inverted color)
            var dpi = VisualTreeHelper.GetDpi(textView);
            var caretText = new FormattedText(
                caretChar.ToString(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                GetTypeface(),
                _textArea.FontSize,
                Brushes.Black,
                dpi.PixelsPerDip);

            drawingContext.DrawText(caretText, point);
        }

        private Typeface GetTypeface()
        {
            if (_cachedTypeface != null &&
                Equals(_cachedFontFamily, _textArea.FontFamily) &&
                _cachedFontStyle == _textArea.FontStyle &&
                _cachedFontWeight == _textArea.FontWeight &&
                _cachedFontStretch == _textArea.FontStretch)
            {
                return _cachedTypeface;
            }

            _cachedFontFamily = _textArea.FontFamily;
            _cachedFontStyle = _textArea.FontStyle;
            _cachedFontWeight = _textArea.FontWeight;
            _cachedFontStretch = _textArea.FontStretch;
            _cachedTypeface = new Typeface(_cachedFontFamily, _cachedFontStyle, _cachedFontWeight, _cachedFontStretch);
            return _cachedTypeface;
        }
    }
}
