/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// Demonstrates editing and running a simple JavaScript script.
    /// </summary>
    public partial class ScriptEditorExample
    {
        /// <summary>
        /// Initializes the example with a runnable JavaScript sample.
        /// </summary>
        public ScriptEditorExample()
        {
            InitializeComponent();

            Editor.Text = """
                // Click Run or press F5 to execute this script.
                const numbers = [10, 20, 30];
                let total = 0;

                for (const number of numbers) {
                    total += number;
                }

                // The built-in ui module provides themed dialogs.
                ui.Alert("Hello from Mosaic! The total is " + total + ".");
                """;
        }
    }
}
