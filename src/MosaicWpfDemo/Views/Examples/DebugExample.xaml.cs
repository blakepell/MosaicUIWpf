/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Media;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class DebugExample
    {
        private BlockCaretBackgroundRenderer? _blockCaretRenderer;

        public DebugExample()
        {
            this.DataContext = this;
            InitializeComponent();
            
            this.Loaded += DebugExample_Loaded;
        }

        private void DebugExample_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigureTerminal();
        }

        private void ConfigureTerminal()
        {
            // Hide the default caret by making it transparent
            TerminalEditor.TextArea.Caret.CaretBrush = Brushes.Transparent;
            
            // Add the block caret renderer
            _blockCaretRenderer = new BlockCaretBackgroundRenderer(TerminalEditor);
            TerminalEditor.TextArea.TextView.BackgroundRenderers.Add(_blockCaretRenderer);
            
            // Force redraw when caret position changes
            TerminalEditor.TextArea.Caret.PositionChanged += (s, e) =>
            {
                TerminalEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Caret);
            };
            
            // Set selection colors to match terminal theme
            TerminalEditor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(0x60, 0x33, 0xff, 0x33));
            TerminalEditor.TextArea.SelectionBorder = new Pen(new SolidColorBrush(Color.FromRgb(0x33, 0xff, 0x33)), 1);
            TerminalEditor.TextArea.SelectionForeground = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));
            
            // Set line number colors (if enabled)
            TerminalEditor.LineNumbersForeground = new SolidColorBrush(Color.FromRgb(0x1a, 0x5a, 0x1a));
            
            // Add sample terminal text
            TerminalEditor.Text = GetSampleTerminalText();
            
            // Move caret to end
            TerminalEditor.CaretOffset = TerminalEditor.Text.Length;
        }

        private static string GetSampleTerminalText()
        {
            return """
                VT220 TERMINAL EMULATOR v2.1
                ============================

                System initialized...
                Memory check: 640K OK
                Loading configuration...

                [OK] Network interface detected
                [OK] Serial port COM1 active
                [OK] Modem ready at 9600 baud

                Welcome to the mainframe system.
                Last login: Thu Jan 15 14:32:18 1987

                You have 3 new messages.

                user@mainframe:~$ dir

                FILENAME.EXT    SIZE     DATE
                ----------------------------------------
                CONFIG.SYS      1,024    01-15-87
                AUTOEXEC.BAT      512    01-15-87
                COMMAND.COM    25,307    01-15-87
                USER.DAT        4,096    01-14-87

                4 file(s)      30,939 bytes
                512,000 bytes free

                user@mainframe:~$ _
                """;
        }
    }

    /// <summary>
    /// Custom background renderer that draws a block cursor at the caret position.
    /// </summary>
    public class BlockCaretBackgroundRenderer : IBackgroundRenderer
    {
        private readonly TextEditor _editor;

        public BlockCaretBackgroundRenderer(TextEditor editor)
        {
            _editor = editor;
        }

        public KnownLayer Layer => KnownLayer.Caret;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (!_editor.TextArea.IsFocused)
                return;
                
            if (_editor.Document == null)
                return;

            textView.EnsureVisualLines();

            var caretOffset = _editor.TextArea.Caret.Offset;
            if (caretOffset < 0 || caretOffset > _editor.Document.TextLength)
                return;

            var caretLine = _editor.Document.GetLineByOffset(caretOffset);
            var visualLine = textView.GetVisualLine(caretLine.LineNumber);
            
            if (visualLine == null)
                return;

            // Get the visual column
            int visualColumn = _editor.TextArea.Caret.Position.VisualColumn;
            
            // Get the X position of the caret
            double xPos = visualLine.GetVisualPosition(visualColumn, VisualYPosition.LineTop).X;
            
            // Get the Y positions
            double yTop = visualLine.GetVisualPosition(visualColumn, VisualYPosition.LineTop).Y;
            double yBottom = visualLine.GetVisualPosition(visualColumn, VisualYPosition.LineBottom).Y;
            
            // Use the width of a space character for monospace fonts
            double charWidth = textView.WideSpaceWidth;
            
            // Adjust for scroll offset
            xPos -= textView.ScrollOffset.X;
            yTop -= textView.ScrollOffset.Y;
            yBottom -= textView.ScrollOffset.Y;
            
            // Draw the block cursor
            var rect = new Rect(xPos, yTop, charWidth, yBottom - yTop);
            var brush = new SolidColorBrush(Color.FromArgb(0xCC, 0x33, 0xff, 0x33));
            brush.Freeze();
            
            drawingContext.DrawRectangle(brush, null, rect);
        }
    }
}