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
    }
}
