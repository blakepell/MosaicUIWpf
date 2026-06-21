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
    /// An info card with a highlight color on the left hand side.
    /// </summary>
    public class ContentPanel : ContentControl
    {
        public new static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            nameof(Background), typeof(Brush), typeof(ContentPanel), new PropertyMetadata(default(Brush)));

        public new Brush Background
        {
            get => (Brush)this.GetValue(BackgroundProperty);
            set => this.SetValue(BackgroundProperty, value);
        }

        public new static readonly DependencyProperty OpacityProperty = DependencyProperty.Register(
            nameof(Opacity), typeof(double), typeof(ContentPanel), new PropertyMetadata(1.0));

        public new double Opacity
        {
            get => (double)this.GetValue(OpacityProperty);
            set => this.SetValue(OpacityProperty, value);
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(ContentPanel), new PropertyMetadata("Info"));

        public string Title
        {
            get => (string)this.GetValue(TitleProperty);
            set => this.SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(ContentPanel));

        public object HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }

        public static readonly DependencyProperty FooterContentProperty =
            DependencyProperty.Register(nameof(FooterContent), typeof(object), typeof(ContentPanel));

        public object FooterContent
        {
            get => GetValue(FooterContentProperty);
            set => SetValue(FooterContentProperty, value);
        }

        public static readonly DependencyProperty SeparatorVisibilityProperty = DependencyProperty.Register(
            nameof(SeparatorVisibility), typeof(Visibility), typeof(ContentPanel), new PropertyMetadata(Visibility.Visible));

        public Visibility SeparatorVisibility
        {
            get => (Visibility)GetValue(SeparatorVisibilityProperty);
            set => SetValue(SeparatorVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderVisibilityProperty = DependencyProperty.Register(
            nameof(HeaderVisibility), typeof(Visibility), typeof(ContentPanel), new PropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Gets or sets the visibility of the header area.
        /// </summary>
        public Visibility HeaderVisibility
        {
            get => (Visibility)GetValue(HeaderVisibilityProperty);
            set => SetValue(HeaderVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(ContentPanel), new PropertyMetadata(new CornerRadius(5)));

        /// <summary>
        /// Gets or sets the radius used to round the panel corners.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderBackgroundProperty = DependencyProperty.Register(
            nameof(HeaderBackground), typeof(Brush), typeof(ContentPanel), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to paint the header background.
        /// </summary>
        public Brush HeaderBackground
        {
            get => (Brush)GetValue(HeaderBackgroundProperty);
            set => SetValue(HeaderBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderForegroundProperty = DependencyProperty.Register(
            nameof(HeaderForeground), typeof(Brush), typeof(ContentPanel), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to paint header text.
        /// </summary>
        public Brush HeaderForeground
        {
            get => (Brush)GetValue(HeaderForegroundProperty);
            set => SetValue(HeaderForegroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FooterBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FooterBackgroundProperty = DependencyProperty.Register(
            nameof(FooterBackground), typeof(Brush), typeof(ContentPanel), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to paint the footer background.
        /// </summary>
        public Brush FooterBackground
        {
            get => (Brush)GetValue(FooterBackgroundProperty);
            set => SetValue(FooterBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FooterForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FooterForegroundProperty = DependencyProperty.Register(
            nameof(FooterForeground), typeof(Brush), typeof(ContentPanel), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to paint footer text.
        /// </summary>
        public Brush FooterForeground
        {
            get => (Brush)GetValue(FooterForegroundProperty);
            set => SetValue(FooterForegroundProperty, value);
        }

        static ContentPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContentPanel), new FrameworkPropertyMetadata(typeof(ContentPanel)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var contentPresenter = this.GetTemplateChild("PART_Content") as ContentPresenter;
            if (contentPresenter != null)
            {
                // Handle the content here if needed
            }
        }
    }
}
