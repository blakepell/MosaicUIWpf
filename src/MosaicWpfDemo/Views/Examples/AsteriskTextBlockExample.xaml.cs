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
    public partial class AsteriskTextBlockExample
    {
        public static readonly DependencyProperty TestValueProperty = DependencyProperty.Register(nameof(TestValue), typeof(string), typeof(AsteriskTextBlockExample), new PropertyMetadata(string.Empty));

        public string TestValue
        {
            get => (string)GetValue(TestValueProperty);
            set => SetValue(TestValueProperty, value);
        }

        public AsteriskTextBlockExample()
        {
            this.DataContext = this;
            InitializeComponent();
            this.TestValue = "this is a test";
        }
    }
}