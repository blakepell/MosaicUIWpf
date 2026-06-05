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
using System.Windows.Controls.Primitives;

namespace Mosaic.UI.Wpf.Controls
{
    public class SliderRepeatButton : RepeatButton
    {
        public static readonly DependencyProperty RadiusOrientationProperty =
            DependencyProperty.Register(nameof(RadiusOrientation), typeof(RadiusOrientation), typeof(SliderRepeatButton),
                new PropertyMetadata(null));

        public RadiusOrientation RadiusOrientation
        {
            get => (RadiusOrientation)GetValue(RadiusOrientationProperty);
            set => SetValue(RadiusOrientationProperty, value);
        }
    }

    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public enum RadiusOrientation
    {
        Down = ExpandDirection.Down,
        Up = ExpandDirection.Up,
        Left = ExpandDirection.Left,
        Right = ExpandDirection.Right,
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    }
}
