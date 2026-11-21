/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// Displays the bundled scripting reference with back and table of contents navigation.
    /// </summary>
    public partial class ScriptReferenceExample
    {
        /// <summary>
        /// The resource URI of the scripting reference's table of contents.
        /// </summary>
        private static readonly Uri HomeUri = new("/Mosaic.UI.Wpf;component/Scripting/Docs/README.md", UriKind.Relative);

        /// <summary>
        /// Initializes the scripting reference page.
        /// </summary>
        public ScriptReferenceExample()
        {
            InitializeComponent();
        }

        private void Back_OnClick(object sender, RoutedEventArgs e)
        {
            Viewer.GoBack();
        }

        private void Home_OnClick(object sender, RoutedEventArgs e)
        {
            Viewer.Source = HomeUri;
        }
    }
}
