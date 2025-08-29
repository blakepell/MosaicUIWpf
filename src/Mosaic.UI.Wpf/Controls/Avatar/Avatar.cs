/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a customizable avatar control that displays content with support for corner radius and template
    /// selection.
    /// </summary>
    public class Avatar : Button
    {
        /// <summary>
        /// Initializes static members of the <see cref="Avatar"/> class.
        /// </summary>
        static Avatar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Avatar), new FrameworkPropertyMetadata(typeof(Avatar)));
            BackgroundProperty.OverrideMetadata(typeof(Avatar), new FrameworkPropertyMetadata(Brushes.Gray));
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property, which specifies the radius of the corners for
        /// the <see cref="Avatar"/> control.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(Avatar), new PropertyMetadata(new CornerRadius(999)));

        /// <summary>
        /// Gets or sets the radius of the corners for the element.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AvatarTemplateSelector"/> dependency property, which allows customization of the
        /// template selection logic for avatar rendering.
        /// </summary>
        public static readonly DependencyProperty AvatarTemplateSelectorProperty = DependencyProperty.Register(
            nameof(AvatarTemplateSelector), typeof(DataTemplateSelector), typeof(Avatar), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="DataTemplateSelector"/> used to select a template for rendering avatars.
        /// </summary>
        public DataTemplateSelector AvatarTemplateSelector
        {
            get => (DataTemplateSelector)GetValue(AvatarTemplateSelectorProperty);
            set => SetValue(AvatarTemplateSelectorProperty, value);
        }

        /// <summary>
        /// Invoked when the control's template is applied.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ContentTemplateSelector = AvatarTemplateSelector;
        }
    }
}
