using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// ANSI colorizer for Vt52Terminal using AvalonEdit's DocumentColorizingTransformer.
    /// Reads color attributes from the terminal's cell buffer and applies them to the rendered text.
    /// </summary>
    public class VT52Colorizer : DocumentColorizingTransformer
    {
        private readonly VT52Terminal _terminal;

        // Standard 16-color palette (matches xterm defaults)
        private static readonly Brush[] StandardColors =
        [
            new SolidColorBrush(Color.FromRgb(0, 0, 0)),       // 0: Black
            new SolidColorBrush(Color.FromRgb(205, 0, 0)),     // 1: Red
            new SolidColorBrush(Color.FromRgb(0, 205, 0)),     // 2: Green
            new SolidColorBrush(Color.FromRgb(205, 205, 0)),   // 3: Yellow
            new SolidColorBrush(Color.FromRgb(0, 0, 238)),     // 4: Blue
            new SolidColorBrush(Color.FromRgb(205, 0, 205)),   // 5: Magenta
            new SolidColorBrush(Color.FromRgb(0, 205, 205)),   // 6: Cyan
            new SolidColorBrush(Color.FromRgb(229, 229, 229)), // 7: White (light gray)
            new SolidColorBrush(Color.FromRgb(127, 127, 127)), // 8: Bright Black (dark gray)
            new SolidColorBrush(Color.FromRgb(255, 0, 0)),     // 9: Bright Red
            new SolidColorBrush(Color.FromRgb(0, 255, 0)),     // 10: Bright Green
            new SolidColorBrush(Color.FromRgb(255, 255, 0)),   // 11: Bright Yellow
            new SolidColorBrush(Color.FromRgb(92, 92, 255)),   // 12: Bright Blue
            new SolidColorBrush(Color.FromRgb(255, 0, 255)),   // 13: Bright Magenta
            new SolidColorBrush(Color.FromRgb(0, 255, 255)),   // 14: Bright Cyan
            new SolidColorBrush(Color.FromRgb(255, 255, 255)), // 15: Bright White
        ];

        // Cache for 256-color palette brushes
        private static readonly Brush[] Color256Cache = new Brush[256];

        static VT52Colorizer()
        {
            // Freeze standard colors for performance
            foreach (var brush in StandardColors)
            {
                brush.Freeze();
            }

            // Pre-populate the first 16 colors
            for (int i = 0; i < 16; i++)
            {
                Color256Cache[i] = StandardColors[i];
            }

            // Generate 6x6x6 color cube (indices 16-231)
            int[] levels = [0, 95, 135, 175, 215, 255];
            for (int r = 0; r < 6; r++)
            {
                for (int g = 0; g < 6; g++)
                {
                    for (int b = 0; b < 6; b++)
                    {
                        int index = 16 + (36 * r) + (6 * g) + b;
                        var brush = new SolidColorBrush(Color.FromRgb(
                            (byte)levels[r], (byte)levels[g], (byte)levels[b]));
                        brush.Freeze();
                        Color256Cache[index] = brush;
                    }
                }
            }

            // Generate grayscale ramp (indices 232-255)
            for (int i = 0; i < 24; i++)
            {
                byte gray = (byte)(8 + i * 10);
                var brush = new SolidColorBrush(Color.FromRgb(gray, gray, gray));
                brush.Freeze();
                Color256Cache[232 + i] = brush;
            }
        }

        public VT52Colorizer(VT52Terminal terminal)
        {
            _terminal = terminal;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineNumber = line.LineNumber - 1; // Convert to 0-based
            if (lineNumber < 0 || lineNumber >= _terminal.BufferRows)
            {
                return;
            }

            var buffer = _terminal.Buffer;
            int cols = Math.Min(_terminal.BufferColumns, line.Length);

            // Track runs of identical attributes for efficiency
            int runStart = 0;
            TerminalAttributes currentAttrs = buffer[lineNumber, 0].Attrs;

            for (int col = 1; col <= cols; col++)
            {
                TerminalAttributes attrs = col < cols ? buffer[lineNumber, col].Attrs : default;
                bool endOfRun = col == cols || attrs != currentAttrs;

                if (endOfRun && runStart < col)
                {
                    ApplyAttributes(line.Offset + runStart, line.Offset + col, currentAttrs);
                    runStart = col;
                    currentAttrs = attrs;
                }
            }
        }

        private void ApplyAttributes(int startOffset, int endOffset, TerminalAttributes attrs)
        {
            // Skip if default attributes (light gray on black, no styling)
            bool isDefault = attrs.Foreground == 7 && attrs.Background == 0 &&
                            !attrs.Bold && !attrs.Dim && !attrs.Italic &&
                            !attrs.Underline && !attrs.Reverse && !attrs.Strikethrough;

            if (isDefault)
            {
                return;
            }

            try
            {
                ChangeLinePart(startOffset, endOffset, element =>
                {
                    var fg = attrs.Foreground;
                    var bg = attrs.Background;

                    // Handle reverse video
                    if (attrs.Reverse)
                    {
                        (fg, bg) = (bg, fg);
                    }

                    // Apply bold as bright (add 8 to color index for standard colors)
                    if (attrs.Bold && fg < 8)
                    {
                        fg = (byte)(fg + 8);
                    }

                    // Apply foreground color
                    if (fg != 7 || attrs.Reverse) // Not default gray
                    {
                        var fgBrush = GetBrush(fg);
                        if (attrs.Dim && fgBrush is SolidColorBrush solidBrush)
                        {
                            // Dim: reduce brightness by 50%
                            var c = solidBrush.Color;
                            var dimmed = new SolidColorBrush(Color.FromRgb(
                                (byte)(c.R / 2), (byte)(c.G / 2), (byte)(c.B / 2)));
                            dimmed.Freeze();
                            element.TextRunProperties.SetForegroundBrush(dimmed);
                        }
                        else
                        {
                            element.TextRunProperties.SetForegroundBrush(fgBrush);
                        }
                    }

                    // Apply background color
                    if (bg != 0 || attrs.Reverse) // Not default black
                    {
                        element.TextRunProperties.SetBackgroundBrush(GetBrush(bg));
                    }

                    // Apply text decorations
                    TextDecorationCollection? decorations = null;
                    if (attrs.Underline)
                    {
                        decorations = TextDecorations.Underline;
                    }
                    if (attrs.Strikethrough)
                    {
                        decorations = decorations == null
                            ? TextDecorations.Strikethrough
                            : new TextDecorationCollection(decorations.Concat(TextDecorations.Strikethrough));
                    }
                    if (decorations != null)
                    {
                        element.TextRunProperties.SetTextDecorations(decorations);
                    }

                    // Apply italic via typeface
                    if (attrs.Italic)
                    {
                        var tf = element.TextRunProperties.Typeface;
                        element.TextRunProperties.SetTypeface(new Typeface(
                            tf.FontFamily, FontStyles.Italic, tf.Weight, tf.Stretch));
                    }

                    // Apply bold via typeface weight
                    if (attrs.Bold)
                    {
                        var tf = element.TextRunProperties.Typeface;
                        element.TextRunProperties.SetTypeface(new Typeface(
                            tf.FontFamily, tf.Style, FontWeights.Bold, tf.Stretch));
                    }
                });
            }
            catch
            {
                // Ignore colorization errors - document may have changed
            }
        }

        private static Brush GetBrush(byte colorIndex)
        {
            if (colorIndex < Color256Cache.Length)
            {
                return Color256Cache[colorIndex];
            }

            return StandardColors[7]; // Default to light gray
        }
    }
}
