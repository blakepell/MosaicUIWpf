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
    public partial class BindablePasswordBoxExample
    {
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
            nameof(Password), typeof(string), typeof(BindablePasswordBoxExample), new PropertyMetadata(default(string)));

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        public BindablePasswordBoxExample()
        {
            this.DataContext = this;
            InitializeComponent();
            this.Password = "t@c0 tu3sd@y!";
        }
    }
}