/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Common;
using System.Windows;

namespace BbsNavigator.Views
{
    /// <summary>
    /// Displays persisted application options.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        /// <summary>
        /// Initializes the options window.
        /// </summary>
        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            DataContext = settings;
        }
    }
}
