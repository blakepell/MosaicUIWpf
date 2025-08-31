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
    public partial class SymbolRatingExample
    {
        public static readonly DependencyProperty CurrentRatingProperty = DependencyProperty.Register(
            nameof(CurrentRating), typeof(int), typeof(SymbolRatingExample), new PropertyMetadata(3));

        public int CurrentRating
        {
            get => (int)GetValue(CurrentRatingProperty);
            set => SetValue(CurrentRatingProperty, value);
        }

        public SymbolRatingExample()
        {
            this.DataContext = this;
            InitializeComponent();
        }
    }
}