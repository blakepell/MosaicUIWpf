/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Windows.Markup;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A shield component (shows a property and a value).
    /// </summary>
    [ContentProperty(nameof(Value))]
    public class Shield : Control
    {
        /// <summary>
        /// Identifies the <see cref="Label"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(Shield), new PropertyMetadata(default(string)));

        /// <summary>
        /// Gets or sets the label text associated with this element.
        /// </summary>
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(object), typeof(Shield), new PropertyMetadata(default(object)));

        /// <summary>
        /// Gets or sets the value associated with this property.   
        /// </summary>
        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ValueBackgroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueBackgroundBrushProperty = DependencyProperty.Register(nameof(ValueBackgroundBrush), typeof(Brush), typeof(Shield), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the background brush used to render the value area.
        /// </summary>
        public Brush ValueBackgroundBrush
        {
            get => (Brush)GetValue(ValueBackgroundBrushProperty);
            set => SetValue(ValueBackgroundBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ValueForegroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueForegroundBrushProperty = DependencyProperty.Register(
            nameof(ValueForegroundBrush), typeof(Brush), typeof(Shield), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to paint the foreground of the value display.
        /// </summary>
        public Brush ValueForegroundBrush
        {
            get => (Brush)GetValue(ValueForegroundBrushProperty);
            set => SetValue(ValueForegroundBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(Shield), new PropertyMetadata(new CornerRadius(0)));

        /// <summary>
        /// Gets or sets the radius of the corners for the shield.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Initializes static metadata for the <see cref="Shield"/> class.
        /// </summary>
        static Shield()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Shield), new FrameworkPropertyMetadata(typeof(Shield)));
        }
    }
}
