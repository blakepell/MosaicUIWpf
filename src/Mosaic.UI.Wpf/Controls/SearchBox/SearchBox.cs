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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A custom <see cref="TextBox"/> tailored for searching or filtering.
    /// </summary>
    public class SearchBox : TextBox
    {
        /// <summary>
        /// Represents the border used to visually indicate the search area or container
        /// and wire up events required for it.
        /// </summary>
        private Border? _searchBorder;

        /// <summary>
        /// Identifies the <see cref="HasText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasTextProperty = DependencyProperty.Register(
            nameof(HasText), typeof(bool), typeof(SearchBox), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the associated element contains text.
        /// </summary>
        public bool HasText
        {
            get => Convert.ToBoolean(GetValue(HasTextProperty));
            set => SetValue(HasTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Watermark"/> dependency property, which specifies the placeholder text displayed
        /// in the <see cref="SearchBox"/> control.
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
            nameof(Watermark), typeof(string), typeof(SearchBox), new PropertyMetadata("Search"));

        /// <summary>
        /// Gets or sets the watermark text displayed in the control.
        /// </summary>
        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ClearTextOnEnter"/> dependency property, which determines whether the text in the
        /// search box is cleared when the Enter key is pressed.
        /// </summary>
        public static readonly DependencyProperty ClearTextOnEnterProperty = DependencyProperty.Register(
            nameof(ClearTextOnEnter), typeof(bool), typeof(SearchBox), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the text should be cleared when the Enter key is pressed.
        /// </summary>
        public bool ClearTextOnEnter
        {
            get => (bool)GetValue(ClearTextOnEnterProperty);
            set => SetValue(ClearTextOnEnterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FocusControlOnEnter"/> dependency property, which specifies the control to receive
        /// focus when the Enter key is pressed.
        /// </summary>
        public static readonly DependencyProperty FocusControlOnEnterProperty = DependencyProperty.Register(
            nameof(FocusControlOnEnter), typeof(UIElement), typeof(SearchBox), new PropertyMetadata(default(UIElement)));

        /// <summary>
        /// Gets or sets the UI element that will receive focus when the Enter key is pressed.
        /// </summary>
        /// <remarks>Use this property to specify a target UI element that should automatically receive
        /// focus when the Enter key is pressed. This can be useful for improving user navigation in forms or other
        /// interactive UI scenarios.</remarks>
        public UIElement? FocusControlOnEnter
        {
            get => (UIElement?)GetValue(FocusControlOnEnterProperty);
            set => SetValue(FocusControlOnEnterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedBorderBrush"/> dependency property, which specifies the border brush
        /// when the control has focus.
        /// </summary>
        public static readonly DependencyProperty SelectedBorderBrushProperty = DependencyProperty.Register(
            nameof(SelectedBorderBrush), typeof(Brush), typeof(SearchBox), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the border brush used when the control has focus.
        /// </summary>
        /// <remarks>If not set, defaults to the theme's ControlSelectedBorderBrush resource.</remarks>
        public Brush? SelectedBorderBrush
        {
            get => (Brush?)GetValue(SelectedBorderBrushProperty);
            set => SetValue(SelectedBorderBrushProperty, value);
        }

        /// <summary>
        /// Initializes static members of the <see cref="SearchBox"/> class by overriding default metadata for specific
        /// dependency properties.
        /// </summary>
        /// <remarks>
        /// This static constructor modifies the default style key and height metadata for the
        /// <see cref="SearchBox"/> control. The <see cref="FrameworkElement.HeightProperty"/> is set to a default value
        /// of 28.0, and the <see cref="Control.DefaultStyleKeyProperty"/> is updated to associate the control with its
        /// default style.
        /// </remarks>
        static SearchBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchBox), new FrameworkPropertyMetadata(typeof(SearchBox)));
            HeightProperty.OverrideMetadata(typeof(SearchBox), new FrameworkPropertyMetadata(28.0));
        }

        /// <summary>
        /// Invoked whenever application code or internal processes call <see cref="FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        /// <remarks>This method is overridden to perform additional setup after the control's template is
        /// applied.  It retrieves and initializes the template part named "PART_SearchBorder" and attaches event
        /// handlers as needed.</remarks>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _searchBorder = (Border?)GetTemplateChild("PART_SearchBorder");

            if (_searchBorder != null)
            {
                _searchBorder.MouseLeftButtonUp += SearchButton_MouseLeftButtonUpHandler;
            }
        }

        /// <summary>
        /// Called when the text content of the control changes.
        /// </summary>
        /// <remarks>This method updates the <see cref="HasText"/> property based on the current length of
        /// the text. Derived classes should call the base implementation to ensure proper behavior.</remarks>
        /// <param name="e">An object containing data related to the text change event.</param>
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            HasText = Text.Length > 0;
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewKeyDown"/> event to provide custom behavior for specific keys.
        /// </summary>
        /// <remarks>This method clears the text when the <see cref="Key.Escape"/> key is pressed and
        /// triggers the <see cref="SearchExecuted"/> event when the <see cref="Key.Enter"/> key is pressed. After the
        /// event is triggered, the text is reset to an empty string.</remarks>
        /// <param name="e">The event data for the key press, including information about the key that was pressed.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            switch (e.Key)
            {
                case Key.Escape:
                    Text = string.Empty;
                    break;
                case Key.Enter:
                    SearchExecuted?.Invoke(this, Text);

                    if (ClearTextOnEnter)
                    {
                        Text = string.Empty;
                    }
                    else
                    {
                        this.SelectAll();
                    }

                    FocusControlOnEnter?.Focus();

                    break;
            }
        }

        /// <summary>
        /// A search event that can be subscribed to.
        /// </summary>
        public event EventHandler<string?>? SearchExecuted;

        /// <summary>
        /// Handles the MouseLeftButtonUp event for the search button.
        /// </summary>
        /// <remarks>Clears the text if the search input contains any text when the event is
        /// triggered.</remarks>
        /// <param name="sender">The source of the event, typically the search button.</param>
        /// <param name="e">The event data associated with the MouseLeftButtonUp event.</param>
        private void SearchButton_MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (HasText)
            {
                this.Text = string.Empty;
            }
        }

        /// <summary>
        /// Sets the keyboard focus to the current element.
        /// </summary>
        /// <remarks>This method ensures that the current element receives keyboard input by invoking the
        /// focus mechanism. Use this method to programmatically set focus to an element when user interaction is
        /// required.</remarks>
        public void SetKeyboardFocus()
        {
            this.Focus();
        }
    }
}