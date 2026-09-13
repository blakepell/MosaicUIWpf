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
        /// The view model bound to the example's label and exposed to the script as vm.
        /// </summary>
        public ScriptEditorExampleViewModel ViewModel { get; } = new();

        /// <summary>
        /// Initializes the example with a runnable JavaScript sample.
        /// </summary>
        public ScriptEditorExample()
        {
            InitializeComponent();
            DataContext = ViewModel;

            // Registering the object makes it callable from JavaScript and adds vm to completion,
            // so typing vm. lists the view model's public properties and methods.
            Editor.Environment?.RegisterObject("vm", ViewModel);

            Editor.Text = """
                // Click Run or press F5 to execute this script.
                const numbers = [10, 20, 30];
                let total = 0;

                for (const number of numbers) {
                    total += number;
                }

                // vm is the example's view model; setting its property updates the bound label above.
                vm.TestLabel = "This was set from JavaScript. The total is " + total + ".";

                // The built-in ui module provides themed dialogs.
                ui.Alert("Hello from Mosaic! The total is " + total + ".");
                """;
        }
    }
}
