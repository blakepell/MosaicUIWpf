namespace Mosaic.UI.Wpf.Controls
{
    public class SettingsItem : ContentControl
    {
        static SettingsItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SettingsItem),
                new FrameworkPropertyMetadata(typeof(SettingsItem)));
        }

        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(SettingsItem));

        public ImageSource IconSource
        {
            get => (ImageSource)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingsItem));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsItem));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(SettingsItem), new PropertyMetadata(new CornerRadius(4)));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(SettingsItem), new PropertyMetadata(24.0));

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Footer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FooterProperty =
            DependencyProperty.Register(nameof(Footer), typeof(object), typeof(SettingsItem), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content displayed below the settings row and its divider.
        /// </summary>
        /// <value>
        /// The footer content. The default is <see langword="null"/>, which hides the footer and divider.
        /// Collapsing a footer element also hides the footer and divider.
        /// </value>
        public object? Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }
    }
}
