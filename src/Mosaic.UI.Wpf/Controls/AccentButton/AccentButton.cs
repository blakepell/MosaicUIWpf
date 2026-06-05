namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Identifies the visual treatment used by an <see cref="AccentButton" />.
    /// </summary>
    public enum AccentButtonType
    {
        Default = 0,
        ThemeAccent = 1,
        Gray = 2,
        FluentGreen = 3,
        FluentRed = 4
    }

    /// <summary>
    /// Represents a themed button that changes its accent color based on <see cref="AccentButtonType" />.
    /// </summary>
    public class AccentButton : Button
    {
        /// <summary>
        /// Identifies the <see cref="AccentButtonType" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty AccentButtonTypeProperty = DependencyProperty.Register(
            nameof(AccentButtonType), typeof(AccentButtonType), typeof(AccentButton),
            new FrameworkPropertyMetadata(AccentButtonType.ThemeAccent));

        /// <summary>
        /// Initializes a new instance of the <see cref="AccentButton" /> class.
        /// </summary>
        public AccentButton()
        {
            this.DefaultStyleKey = typeof(AccentButton);
        }

        /// <summary>
        /// Gets or sets the accent style applied to the button.
        /// </summary>
        /// <value>The accent style used to determine the button's background, border, and hover treatment. The default is <see langword="ThemeAccent" />.</value>
        public AccentButtonType AccentButtonType
        {
            get => (AccentButtonType)this.GetValue(AccentButtonTypeProperty);
            set => this.SetValue(AccentButtonTypeProperty, value);
        }
    }
}
