/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class DebugExample
    {
        public DebugExample()
        {
            this.DataContext = this;
            InitializeComponent();

            this.Loaded += DebugExample_Loaded;
        }

        private void DebugExample_Loaded(object sender, RoutedEventArgs e)
        {
            TerminalEditor.Text = GetSampleTerminalText();
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
}