/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A checkbox that displays a single favorite symbol.
    /// </summary>
    [TemplatePart(Name = PartSymbolTextBlock, Type = typeof(TextBlock))]
    public class FavoriteCheckBox : CheckBox
    {
        private const string PartSymbolTextBlock = "PART_SymbolTextBlock";

        private TextBlock? _symbolTextBlock;

        /// <summary>
        /// Identifies the <see cref="Symbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
            nameof(Symbol), typeof(string), typeof(FavoriteCheckBox), new PropertyMetadata("★"));

        /// <summary>
        /// Gets or sets the symbol character to display.
        /// </summary>
        public string Symbol
        {
            get => (string)GetValue(SymbolProperty);
            set => SetValue(SymbolProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SymbolFont"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolFontProperty = DependencyProperty.Register(
            nameof(SymbolFont), typeof(FontFamily), typeof(FavoriteCheckBox), new PropertyMetadata(new FontFamily("Segoe UI Symbol")));

        /// <summary>
        /// Gets or sets the font family for the symbol.
        /// </summary>
        public FontFamily SymbolFont
        {
            get => (FontFamily)GetValue(SymbolFontProperty);
            set => SetValue(SymbolFontProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SymbolSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SymbolSizeProperty = DependencyProperty.Register(
            nameof(SymbolSize), typeof(double), typeof(FavoriteCheckBox), new PropertyMetadata(20.0));

        /// <summary>
        /// Gets or sets the size of the symbol.
        /// </summary>
        public double SymbolSize
        {
            get => (double)GetValue(SymbolSizeProperty);
            set => SetValue(SymbolSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CheckedBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CheckedBrushProperty = DependencyProperty.Register(
            nameof(CheckedBrush), typeof(Brush), typeof(FavoriteCheckBox), new PropertyMetadata(new SolidColorBrush(Colors.Gold)));

        /// <summary>
        /// Gets or sets the brush used when the checkbox is checked.
        /// </summary>
        public Brush CheckedBrush
        {
            get => (Brush)GetValue(CheckedBrushProperty);
            set => SetValue(CheckedBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="UncheckedBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UncheckedBrushProperty = DependencyProperty.Register(
            nameof(UncheckedBrush), typeof(Brush), typeof(FavoriteCheckBox), new PropertyMetadata(new SolidColorBrush(Colors.LightGray)));

        /// <summary>
        /// Gets or sets the brush used when the checkbox is unchecked.
        /// </summary>
        public Brush UncheckedBrush
        {
            get => (Brush)GetValue(UncheckedBrushProperty);
            set => SetValue(UncheckedBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HoverBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HoverBrushProperty = DependencyProperty.Register(
            nameof(HoverBrush), typeof(Brush), typeof(FavoriteCheckBox), new PropertyMetadata(new SolidColorBrush(Colors.Orange)));

        /// <summary>
        /// Gets or sets the brush used when the checkbox is hovered.
        /// </summary>
        public Brush HoverBrush
        {
            get => (Brush)GetValue(HoverBrushProperty);
            set => SetValue(HoverBrushProperty, value);
        }

        static FavoriteCheckBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FavoriteCheckBox), new FrameworkPropertyMetadata(typeof(FavoriteCheckBox)));
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _symbolTextBlock = GetTemplateChild(PartSymbolTextBlock) as TextBlock;
            UpdateVisualState();
        }

        /// <inheritdoc />
        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);
            UpdateVisualState();
        }

        /// <inheritdoc />
        protected override void OnUnchecked(RoutedEventArgs e)
        {
            base.OnUnchecked(e);
            UpdateVisualState();
        }

        /// <inheritdoc />
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            UpdateVisualState();
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_symbolTextBlock == null)
            {
                return;
            }

            _symbolTextBlock.Text = Symbol;
            _symbolTextBlock.FontFamily = SymbolFont;
            _symbolTextBlock.FontSize = SymbolSize;
            _symbolTextBlock.Foreground = !IsEnabled
                ? SystemColors.GrayTextBrush
                : IsMouseOver
                    ? HoverBrush
                    : IsChecked == true
                        ? CheckedBrush
                        : UncheckedBrush;
        }
    }
}
