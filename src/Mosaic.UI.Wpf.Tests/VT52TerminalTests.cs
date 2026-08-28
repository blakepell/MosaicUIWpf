/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Runtime.ExceptionServices;
using System.Windows;
using Mosaic.UI.Wpf.Controls.VT52Terminal;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class VT52TerminalTests
    {
        private static void RunSta(Action action)
        {
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        private static string[] ScreenLines(VT52Terminal terminal) =>
            terminal.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        [Fact]
        public void Esc_D_Uses_Ansi_Index_By_Default()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal { MaxScrollbackLines = 0 };
                terminal.Reset(2, 3);

                terminal.Add("aaa\r\nbbb\u001BD");

                string[] lines = ScreenLines(terminal);
                Assert.Equal("bbb", lines[0]);
                Assert.Equal("   ", lines[1]);
            });
        }

        [Fact]
        public void Decrst_2_Selects_Vt52_Cursor_Left_Semantics()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal();
                terminal.Reset(2, 3);

                terminal.Add("\u001B[?2lA\u001BDB");

                string[] lines = ScreenLines(terminal);
                Assert.Equal("B  ", lines[0]);
                Assert.Equal("   ", lines[1]);
            });
        }

        [Fact]
        public void Esc_LessThan_Returns_From_Vt52_To_Ansi_Mode()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal();
                terminal.Reset(2, 3);

                terminal.Add("\u001B[?2l\u001B<A\u001BDB");

                string[] lines = ScreenLines(terminal);
                Assert.Equal("A  ", lines[0]);
                Assert.Equal(" B ", lines[1]);
            });
        }

        [Fact]
        public void Esc_H_Sets_A_Tab_Stop_In_Ansi_Mode()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal();
                terminal.Reset(2, 10);

                terminal.Add("AB\u001BH\r\tX");

                string[] lines = ScreenLines(terminal);
                Assert.Equal("ABX       ", lines[0]);
            });
        }

        [Fact]
        public void Vt52_Emulation_Starts_With_Vt52_Escape_Semantics()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal { EmulationMode = TerminalEmulationMode.Vt52 };
                terminal.Reset(2, 3);

                terminal.Add("A\u001BDB");

                string[] lines = ScreenLines(terminal);
                Assert.Equal("B  ", lines[0]);
            });
        }

        [Fact]
        public void Tty_Emulation_Does_Not_Interpret_Ansi_Sequences()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal { EmulationMode = TerminalEmulationMode.Tty };
                terminal.Reset(2, 12);

                terminal.Add("A\u001B[31mB");

                string[] lines = ScreenLines(terminal);
                Assert.Equal("A[31mB      ", lines[0]);
            });
        }

        [Fact]
        public void Fixed_Size_Mode_Preserves_Explicit_Grid()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal { AutoResizeTerminal = false };
                terminal.Resize(25, 80);

                terminal.OnSizeChanged();

                Assert.Equal(25, terminal.Rows);
                Assert.Equal(80, terminal.Columns);
            });
        }

        [Fact]
        public void Document_Has_Exactly_One_Line_Per_Terminal_Row()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal { MaxScrollbackLines = 0 };
                terminal.Reset(4, 5);

                terminal.Add("abc");

                // A trailing newline would add a phantom empty line that steals a row of the viewport
                // and leaves a scrollback line parked above the live screen.
                Assert.Equal(4, terminal.Document.LineCount);
                Assert.False(terminal.Text.EndsWith('\n'));
            });
        }

        [Fact]
        public void Document_Line_Count_Tracks_Scrollback_Growth()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal { MaxScrollbackLines = 10 };
                terminal.Reset(2, 3);

                // Two of these line feeds scroll the top row off into the scrollback.
                terminal.Add("aaa\r\nbbb\r\nccc\r\nddd");

                Assert.Equal(2 + 2, terminal.Document.LineCount);
            });
        }

        [Fact]
        public void Sgr_Does_Not_Cancel_A_Pending_Autowrap()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal { MaxScrollbackLines = 0 };
                terminal.Reset(2, 3);

                // Fill the row, emit a colour change, then print. The glyph belongs on the next row;
                // cancelling the deferred wrap would overwrite the last cell instead.
                terminal.Add("abc\u001B[31md");

                string[] lines = ScreenLines(terminal);
                Assert.Equal("abc", lines[0]);
                Assert.Equal("d  ", lines[1]);
            });
        }

        [Fact]
        public void Decset_Applies_Every_Parameter()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal { MaxScrollbackLines = 0 };
                terminal.Reset(2, 3);

                // DECAWM off is the second parameter; a parser that only read the first would keep
                // autowrap enabled and push the fourth glyph onto row two.
                terminal.Add("\u001B[?1;7labcd");

                string[] lines = ScreenLines(terminal);
                Assert.Equal("abd", lines[0]);
                Assert.Equal("   ", lines[1]);
            });
        }

        [Fact]
        public void Cursor_Position_Report_Is_Relative_To_Scroll_Region_In_Origin_Mode()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal { MaxScrollbackLines = 0 };
                terminal.Reset(10, 10);

                var transmitted = new List<byte>();
                terminal.Transmit += bytes => transmitted.AddRange(bytes!);

                // Margins 3..8, origin mode on, cursor to the region's second row.
                terminal.Add("\u001B[3;8r\u001B[?6h\u001B[2;1H\u001B[6n");

                Assert.Equal("\u001B[2;1R", System.Text.Encoding.ASCII.GetString(transmitted.ToArray()));
            });
        }

        [Fact]
        public void Decaln_Fills_The_Screen_And_Consumes_Its_Parameter_Byte()
        {
            RunSta(() =>
            {
                var terminal = new VT52Terminal { MaxScrollbackLines = 0 };
                terminal.Reset(2, 3);

                terminal.Add("\u001B#8");

                // An unconsumed parameter byte would leave a stray '8' on the screen.
                string[] lines = ScreenLines(terminal);
                Assert.Equal("EEE", lines[0]);
                Assert.Equal("EEE", lines[1]);
            });
        }
    }
}
