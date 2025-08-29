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
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class HyperlinkExample
    {
        public static readonly DependencyProperty HyperlinkForegroundProperty = DependencyProperty.Register(nameof(HyperlinkForeground), typeof(Brush), typeof(HyperlinkExample), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to render the foreground color of hyperlinks.
        /// </summary>
        public Brush HyperlinkForeground
        {
            get => (Brush)GetValue(HyperlinkForegroundProperty);
            set => SetValue(HyperlinkForegroundProperty, value);
        }

        public static readonly DependencyProperty HyperlinkHoverProperty = DependencyProperty.Register(nameof(HyperlinkHover), typeof(Brush), typeof(HyperlinkExample), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to render the hover state of hyperlinks.
        /// </summary>
        public Brush HyperlinkHover
        {
            get => (Brush)GetValue(HyperlinkHoverProperty);
            set => SetValue(HyperlinkHoverProperty, value);
        }

        /// <summary>
        /// A command to execute from the Hyperlink control that will display a message box with the provided message.
        /// </summary>
        public static ICommand HelloCommand { get; } = new RelayCommand<string?>(Hello);

        /// <summary>
        /// Displays a message in a modal dialog box.
        /// </summary>
        public static void Hello(string? msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                return;
            }

            MessageBox.Show(msg);
        }

        public HyperlinkExample()
        {
            InitializeComponent();
            this.DataContext = this;
            this.HyperlinkForeground = Brushes.MediumPurple;
            this.HyperlinkHover = Brushes.Purple;
        }
    }
}
