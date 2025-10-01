/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls;
using System.Windows.Media;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class PropertyGridExample
    {
        public PropertyGridExample()
        {
            InitializeComponent();

            DataContext = new Person()
            {
                Username = "john.doe",
                Name = "John Doe",
                LastActive = new DateTime(2026, 1, 1),
                Age = 42,
                Active = true,
                Color = Colors.Blue
            };
        }
    }
}