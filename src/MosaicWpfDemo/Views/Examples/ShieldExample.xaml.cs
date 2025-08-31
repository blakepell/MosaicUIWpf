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
    public partial class ShieldExample
    {
        /// <summary>
        /// Identifies the <see cref="Version"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register(
            nameof(Version), typeof(string), typeof(ShieldExample), new PropertyMetadata(default(string)));

        /// <summary>
        /// Gets or sets the version information associated with the object.
        /// </summary>
        public string Version
        {
            get => (string)GetValue(VersionProperty);
            set => SetValue(VersionProperty, value);
        }

        public ShieldExample()
        {
            this.DataContext = this;
            InitializeComponent();

            // Get the version number of Mosaic.UI.Wpf from the shield control.
            this.Version = typeof(Mosaic.UI.Wpf.Controls.Shield).Assembly.GetName().Version?.ToString() ?? "No Version Info Available";
        }
    }
}