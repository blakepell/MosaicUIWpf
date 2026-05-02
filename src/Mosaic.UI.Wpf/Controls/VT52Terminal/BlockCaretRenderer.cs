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
        /// Draw the block caret.
        /// </summary>
        /// <param name="textView"></param>
        /// <param name="drawingContext"></param>
        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            var caret = _textArea.Caret;

            // Determine the visual position of the caret.
            var pos = caret.Position;
            var point = textView.GetVisualPosition(pos, VisualYPosition.LineTop) - textView.ScrollOffset;

            // Use a consistent block size based on font metrics
            var formattedText = new FormattedText(
                "M", // Use a wide character for consistent block size
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(_textArea.FontFamily, _textArea.FontStyle, _textArea.FontWeight, _textArea.FontStretch),
                _textArea.FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(textView).PixelsPerDip);

            var rect = new Rect(point, new Size(formattedText.Width, formattedText.Height));

            // Get the character under the caret, if any
            char caretChar = ' ';
            if (pos.Line < _textArea.Document.LineCount)
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
            var caretText = new FormattedText(
                caretChar.ToString(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(_textArea.FontFamily, _textArea.FontStyle, _textArea.FontWeight, _textArea.FontStretch),
                _textArea.FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(textView).PixelsPerDip);

            drawingContext.DrawText(caretText, point);
        }
    }
}
