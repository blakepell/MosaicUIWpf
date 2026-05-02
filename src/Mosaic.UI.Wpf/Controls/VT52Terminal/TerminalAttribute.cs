namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// Terminal cell attributes (colors, bold, underline, etc.)
    /// </summary>
    public struct TerminalAttributes : IEquatable<TerminalAttributes>
    {
        public byte Foreground; // 0-15 for standard, 16-255 for 256-color, 0 = default
        public byte Background; // 0-15 for standard, 16-255 for 256-color, 0 = default
        public bool Bold;
        public bool Dim;
        public bool Italic;
        public bool Underline;
        public bool Blink;
        public bool Reverse;
        public bool Hidden;
        public bool Strikethrough;

        public static TerminalAttributes Default => new() { Foreground = 7, Background = 0 };

        public bool Equals(TerminalAttributes other) =>
            Foreground == other.Foreground && Background == other.Background &&
            Bold == other.Bold && Dim == other.Dim && Italic == other.Italic &&
            Underline == other.Underline && Blink == other.Blink && Reverse == other.Reverse &&
            Hidden == other.Hidden && Strikethrough == other.Strikethrough;

        public override bool Equals(object? obj) => obj is TerminalAttributes other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            Foreground, Background, Bold, Dim, Italic,
            Underline | (Blink ? (true) : false),
            Reverse | (Hidden ? (true) : false),
            Strikethrough);

        public static bool operator ==(TerminalAttributes left, TerminalAttributes right) => left.Equals(right);

        public static bool operator !=(TerminalAttributes left, TerminalAttributes right) => !left.Equals(right);
    }
}
