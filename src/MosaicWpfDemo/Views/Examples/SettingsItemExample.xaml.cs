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
    public partial class SettingsItemExample
    {
        public static readonly DependencyProperty TestSettingProperty = DependencyProperty.Register(
            nameof(TestSetting), typeof(bool), typeof(SettingsItemExample), new PropertyMetadata(false));

        public bool TestSetting
        {
            get => (bool)GetValue(TestSettingProperty);
            set => SetValue(TestSettingProperty, value);
        }

        public static readonly DependencyProperty DefaultUsernameProperty = DependencyProperty.Register(
            nameof(DefaultUsername), typeof(string), typeof(SettingsItemExample), new PropertyMetadata(Environment.UserName));

        public string DefaultUsername
        {
            get => (string)GetValue(DefaultUsernameProperty);
            set => SetValue(DefaultUsernameProperty, value);
        }

        public SettingsItemExample()
        {
            this.DataContext = this;
            InitializeComponent();
        }
    }
}