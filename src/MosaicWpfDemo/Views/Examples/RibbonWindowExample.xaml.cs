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
    /// Launches <see cref="RibbonDemoWindow"/>, a standalone window that hosts the Ribbon like a real application.
    /// </summary>
    public partial class RibbonWindowExample
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonWindowExample"/> class.
        /// </summary>
        public RibbonWindowExample()
        {
            InitializeComponent();
        }

        private void OpenWindow_Click(object sender, RoutedEventArgs e)
        {
            var window = new RibbonDemoWindow { Owner = Window.GetWindow(this) };
            window.Show();
        }
    }
}
