/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

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

        // Cache for dimmed (50% brightness) palette brushes, lazily populated.
        private static readonly Brush?[] DimColorCache = new Brush?[256];

        // Cache derived bold/italic typefaces keyed by base family + style flags to avoid per-run allocation.
        private readonly Dictionary<(string family, bool bold, bool italic), Typeface> _typefaceCache = new();

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
            if (lineNumber < 0 || lineNumber >= _terminal.TotalRows)
            {
                return;
            }

            int cols = line.Length;
            if (cols <= 0)
            {
                return;
            }

            // Track runs of identical attributes for efficiency
            int runStart = 0;
            TerminalAttributes currentAttrs = _terminal.GetCellAttributes(lineNumber, 0);

            for (int col = 1; col <= cols; col++)
            {
                TerminalAttributes attrs = col < cols ? _terminal.GetCellAttributes(lineNumber, col) : default;
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
            // Background is drawn at full line-height by TerminalBackgroundRenderer.
            // Here we only handle foreground color, text decorations, and typeface.
            bool isDefault = attrs.DefaultForeground && !attrs.Reverse &&
                            !attrs.Bold && !attrs.Dim && !attrs.Italic &&
                            !attrs.Underline && !attrs.Strikethrough;

            if (isDefault)
            {
                return;
            }

            try
            {
                ChangeLinePart(startOffset, endOffset, element =>
                {
                    byte fg = attrs.Foreground;
                    bool fgDefault = attrs.DefaultForeground;

                    // Reverse video: effective fg = original bg.
                    if (attrs.Reverse)
                    {
                        byte bg = attrs.Background;
                        if (attrs.DefaultBackground) bg = 0; // default bg = black (index 0)
                        fg = bg;
                        fgDefault = false;
                    }

                    // Bold promotes standard colors to their bright variant.
                    if (attrs.Bold && !fgDefault && fg < 8)
                    {
                        fg = (byte)(fg + 8);
                    }

                    // Foreground color.
                    if (!fgDefault)
                    {
                        var fgBrush = attrs.Dim ? GetDimBrush(fg) : GetPaletteBrush(fg);
                        element.TextRunProperties.SetForegroundBrush(fgBrush);
                    }

                    // Text decorations.
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

                    // Bold/italic typeface (cached).
                    if (attrs.Bold || attrs.Italic)
                    {
                        var tf = element.TextRunProperties.Typeface;
                        element.TextRunProperties.SetTypeface(GetTypeface(tf, attrs.Bold, attrs.Italic));
                    }
                });
            }
            catch
            {
                // Ignore colorization errors - document may have changed
            }
        }

        private Typeface GetTypeface(Typeface baseTypeface, bool bold, bool italic)
        {
            string family = baseTypeface.FontFamily.Source;
            var key = (family, bold, italic);

            if (_typefaceCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var tf = new Typeface(
                baseTypeface.FontFamily,
                italic ? FontStyles.Italic : baseTypeface.Style,
                bold ? FontWeights.Bold : baseTypeface.Weight,
                baseTypeface.Stretch);

            _typefaceCache[key] = tf;
            return tf;
        }

        private static Brush GetDimBrush(byte colorIndex)
        {
            var cached = DimColorCache[colorIndex];
            if (cached != null)
            {
                return cached;
            }

            if (GetPaletteBrush(colorIndex) is SolidColorBrush solid)
            {
                var c = solid.Color;
                var dimmed = new SolidColorBrush(Color.FromRgb((byte)(c.R / 2), (byte)(c.G / 2), (byte)(c.B / 2)));
                dimmed.Freeze();
                DimColorCache[colorIndex] = dimmed;
                return dimmed;
            }

            return GetPaletteBrush(colorIndex);
        }

        internal static Brush GetPaletteBrush(byte colorIndex)
        {
            if (colorIndex < Color256Cache.Length)
            {
                return Color256Cache[colorIndex];
            }

            return StandardColors[7]; // Default to light gray
        }
    }
}
