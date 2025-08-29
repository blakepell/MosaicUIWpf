/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable InconsistentNaming

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a control that displays text in a non-editable mode and allows users to switch to an editable mode to
    /// modify the text. The control supports double-click editing, text trimming, and customizable appearance.
    /// </summary>
    /// <remarks>
    /// The <see cref="IsFilePath"/> property is used to indicate whether the text should be treated like a file path
    /// instead of a regular string.  The file path behavior will select only the file name and not the extension when
    /// the control is in edit mode.
    /// </remarks>
    [TemplatePart(Type = typeof(Grid), Name = GRID_NAME)]
    [TemplatePart(Type = typeof(TextBlock), Name = TEXTBLOCK_DISPLAY_TEXT_NAME)]
    [TemplatePart(Type = typeof(TextBox), Name = TEXTBOX_EDITTEXT_NAME)]
    public class EditableTextBlock : Control
    {
        /// <summary>
        /// Event that is raised when the new input has been accepted and has changed.  This won't fire
        /// if the text in the box was the same as it was when invoked.
        /// </summary>
        public event EventHandler? TextUpdated;

        private const string GRID_NAME = "PART_GridContainer";
        private const string TEXTBLOCK_DISPLAY_TEXT_NAME = "PART_TbDisplayText";
        private const string TEXTBOX_EDITTEXT_NAME = "PART_TbEditText";

        private Grid? _gridContainer;
        private TextBlock? _textBlockDisplayText;
        private TextBox? _textBoxEditText;

        /// <summary>
        /// Gets or sets the most recent text value processed or stored by the application.
        /// </summary>
        public string? LastTextValue { get; set; }

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(EditableTextBlock), new UIPropertyMetadata(string.Empty));

        /// <summary>
        /// The text of the TextBlock and TextBox.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TextBlockForegroundColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextBlockForegroundColorProperty = DependencyProperty.Register(
            nameof(TextBlockForegroundColor), typeof(Brush), typeof(EditableTextBlock), new UIPropertyMetadata(Brushes.Black));

        /// <summary>
        /// The <see cref="TextBlock"/> for the <see cref="Brush"/> foreground color.
        /// </summary>
        public Brush TextBlockForegroundColor
        {
            get => (Brush)GetValue(TextBlockForegroundColorProperty);
            set => SetValue(TextBlockForegroundColorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TrimOnTextUpdated"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrimOnTextUpdatedProperty = DependencyProperty.Register(
            nameof(TrimOnTextUpdated), typeof(bool), typeof(EditableTextBlock), new PropertyMetadata(false));

        /// <summary>
        /// If the control will run Trim on the value before the TextUpdated event is raised.
        /// </summary>
        public bool TrimOnTextUpdated
        {
            get => (bool)GetValue(TrimOnTextUpdatedProperty);
            set => SetValue(TrimOnTextUpdatedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsDoubleClickToEditEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDoubleClickToEditEnabledProperty = DependencyProperty.Register(
            nameof(IsDoubleClickToEditEnabled), typeof(bool), typeof(EditableTextBlock), new PropertyMetadata(true));

        /// <summary>
        /// If the control will allow double click to edit.  If this is set to false, the control will
        /// require manual intervention to become editable.
        /// </summary>
        public bool IsDoubleClickToEditEnabled
        {
            get => (bool)GetValue(IsDoubleClickToEditEnabledProperty);
            set => SetValue(IsDoubleClickToEditEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsFilePath"/> dependency property.
        /// </summary>
        /// <remarks>This property is used to indicate whether the text in the <see
        /// cref="EditableTextBlock"/> represents a file path.</remarks>
        public static readonly DependencyProperty IsFilePathProperty = DependencyProperty.Register(
            nameof(IsFilePath), typeof(bool), typeof(EditableTextBlock), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the current value represents a file path.
        /// </summary>
        public bool IsFilePath
        {
            get => (bool)GetValue(IsFilePathProperty);
            set => SetValue(IsFilePathProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TextBoxPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextBoxPaddingProperty = DependencyProperty.Register(
            nameof(TextBoxPadding), typeof(Thickness), typeof(EditableTextBlock), new PropertyMetadata(default(Thickness)));

        /// <summary>
        /// Gets or sets the padding inside the text box.
        /// </summary>
        public Thickness TextBoxPadding
        {
            get => (Thickness)GetValue(TextBoxPaddingProperty);
            set => SetValue(TextBoxPaddingProperty, value);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        static EditableTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EditableTextBlock), new FrameworkPropertyMetadata(typeof(EditableTextBlock)));
        }

        /// <summary>
        /// Overrides the OnApplyTemplate method of the base class to apply the template and
        /// initialize the necessary controls.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _gridContainer = Template.FindName(GRID_NAME, this) as Grid;

            if (_gridContainer != null)
            {
                _textBlockDisplayText = _gridContainer.Children[0] as TextBlock;
                _textBoxEditText = _gridContainer.Children[1] as TextBox;

                if (_textBoxEditText != null)
                {
                    _textBoxEditText.LostFocus += OnTextBoxLostFocus;
                    LastTextValue = _textBoxEditText.Text;
                }
            }
        }

        /// <summary>
        /// Double click event used to show the control in edit mode.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (IsDoubleClickToEditEnabled)
            {
                EditMode();
            }
        }

        /// <summary>
        /// Lost focus event used to show the control in view mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            ViewMode();

            if (UpdateTextValue() && TextUpdated != null)
            {
                TextUpdated(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Overrides the OnKeyDown method of the base class to handle key events in the TextBox.  This
        /// will allow us to handle Key.Enter as accepting the new value and Key.Escape for cancelling the
        /// edit and returning to the original value.
        /// </summary>
        /// <param name="e">The KeyEventArgs containing event data.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_textBoxEditText != null)
            {
                if (e.Key == Key.Enter)
                {
                    ViewMode();
                    if (UpdateTextValue() && TextUpdated != null)
                    {
                        TextUpdated(this, EventArgs.Empty);
                    }
                }
                else if (e.Key == Key.Escape)
                {
                    ViewMode();
                    ResetTextBoxValue();
                }
            }
        }

        /// <summary>
        /// Hides the <see cref="TextBox"/> and shows the <see cref="TextBlock"/>.
        /// </summary>
        public void ViewMode()
        {
            if (_textBoxEditText != null)
            {
                _textBoxEditText.Visibility = Visibility.Collapsed;
            }

            if (_textBlockDisplayText != null)
            {
                _textBlockDisplayText.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Hides the <see cref="TextBlock"/> and shows the <see cref="TextBox"/>.
        /// </summary>
        public void EditMode()
        {
            if (_textBoxEditText != null)
            {
                _textBoxEditText.Visibility = Visibility.Visible;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _textBoxEditText.Focus();

                    // If it's a file path, select just the file name and not the extension.
                    if (this.IsFilePath)
                    {
                        this.SelectFileName();
                    }
                }));
            }

            if (_textBlockDisplayText != null)
            {
                _textBlockDisplayText.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Updates the Text property with the TextBox value.
        /// </summary>
        private bool UpdateTextValue()
        {
            if (_textBoxEditText != null && Text != _textBoxEditText.Text)
            {
                Text = _textBoxEditText.Text;

                if (TrimOnTextUpdated)
                {
                    Text = Text.Trim();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the TextBox value to the previous Text property value.
        /// </summary>
        private void ResetTextBoxValue()
        {
            if (_textBoxEditText != null)
            {
                _textBoxEditText.Text = Text;
            }
        }

        /// <summary>
        /// Selects just the filename portion (i.e. everything before the last '.').
        /// If there's no '.', selects the entire Text and moves the caret to the end.
        /// Otherwise, selects from 0 to the character before the last '.', and places the caret at the start.
        /// </summary>
        private void SelectFileName()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var textBox = _textBoxEditText;

                if (textBox == null)
                {
                    throw new ArgumentNullException(nameof(textBox));
                }

                // Ensure the box is focused so selection shows up
                textBox.Focus();

                // Safely grab the text
                var text = textBox.Text ?? string.Empty;

                // Find the last period
                int dot = text.LastIndexOf('.');

                if (dot <= 0)
                {
                    // Bo extension (or leading dot only): select all
                    textBox.SelectAll();
                    // Move caret to end of the selection
                    textBox.CaretIndex = text.Length;
                }
                else
                {
                    // has an extension: select just the name part
                    textBox.Select(0, dot);
                }
            }));
        }
    }
}