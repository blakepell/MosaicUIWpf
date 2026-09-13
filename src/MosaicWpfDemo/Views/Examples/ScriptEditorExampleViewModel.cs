/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// A view model shared by the script editor example's UI and its script, where it is registered as vm.
    /// </summary>
    public partial class ScriptEditorExampleViewModel : ObservableObject
    {
        /// <summary>
        /// The text of the label beside the editor; scripts can change it with vm.TestLabel = '...'.
        /// </summary>
        [ObservableProperty]
        [property: Description("The text shown in the example's label, outside of the script editor.")]
        private string _testLabel = "This label is bound to vm.TestLabel. Run the script to change it.";
    }
}
