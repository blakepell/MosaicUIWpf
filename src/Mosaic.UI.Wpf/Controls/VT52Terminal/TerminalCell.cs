namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// A single cell in the terminal buffer.
    /// </summary>
    public struct TerminalCell
    {
        public char Char;
        public TerminalAttributes Attrs;

        public TerminalCell(char c, TerminalAttributes attrs)
        {
            Char = c;
            Attrs = attrs;
        }
    }
}
