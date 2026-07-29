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

using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using ToastNotifier = Mosaic.UI.Wpf.Controls.ToastManager;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A Mosaic styled text box with an attached copy button that places the box's text onto the
    /// clipboard.  The button is docked to the right edge of the box and shares the box's border,
    /// and an optional <see cref="ToastMessage"/> can be shown reporting whether the copy
    /// succeeded.
    /// </summary>
    [DefaultEvent(nameof(TextCopied))]
    [DefaultProperty(nameof(Text))]
    [TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartCopyButton, Type = typeof(ButtonBase))]
    public class CopyTextBox : Control
    {
        private const string PartTextBox = "PART_TextBox";
        private const string PartCopyButton = "PART_CopyButton";

        /// <summary>
        /// The number of times a clipboard write is retried before it is reported as a failure.
        /// The clipboard is a shared OS resource another process may hold a lock on.
        /// </summary>
        private const int ClipboardRetryCount = 3;

        private TextBox? _textBox;
        private ButtonBase? _copyButton;

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(CopyTextBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Gets or sets the text displayed in the control and copied to the clipboard.
        /// </summary>
        [Category("Common")]
        [Description("The text displayed in the control and copied to the clipboard.")]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Watermark"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
            nameof(Watermark), typeof(string), typeof(CopyTextBox), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the placeholder text shown when the control is empty and unfocused.
        /// </summary>
        [Category("Appearance")]
        [Description("Placeholder text shown when the control is empty and unfocused.")]
        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            nameof(IsReadOnly), typeof(bool), typeof(CopyTextBox), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the text can be edited by the user.
        /// </summary>
        [Category("Common")]
        [Description("Indicates whether the text can be edited by the user.")]
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(CopyTextBox), new FrameworkPropertyMetadata(new CornerRadius(0)));

        /// <summary>
        /// Gets or sets the corner radius of the control's outer border.
        /// </summary>
        [Category("Appearance")]
        [Description("The corner radius of the control's outer border.")]
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedBorderBrushProperty = DependencyProperty.Register(
            nameof(SelectedBorderBrush), typeof(Brush), typeof(CopyTextBox), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the border brush used while the control has keyboard focus.
        /// </summary>
        [Category("Brushes")]
        [Description("The border brush used while the control has keyboard focus.")]
        public Brush? SelectedBorderBrush
        {
            get => (Brush?)GetValue(SelectedBorderBrushProperty);
            set => SetValue(SelectedBorderBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CopyIcon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CopyIconProperty = DependencyProperty.Register(
            nameof(CopyIcon), typeof(Geometry), typeof(CopyTextBox), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the geometry rendered inside the copy button.  Defaults to a copy glyph and
        /// can be replaced with any icon geometry.
        /// </summary>
        [Category("Appearance")]
        [Description("The geometry rendered inside the copy button.")]
        public Geometry? CopyIcon
        {
            get => (Geometry?)GetValue(CopyIconProperty);
            set => SetValue(CopyIconProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CopyButtonToolTip"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CopyButtonToolTipProperty = DependencyProperty.Register(
            nameof(CopyButtonToolTip), typeof(object), typeof(CopyTextBox), new FrameworkPropertyMetadata("Copy to clipboard"));

        /// <summary>
        /// Gets or sets the tool tip shown when hovering over the copy button.
        /// </summary>
        [Category("Appearance")]
        [Description("The tool tip shown when hovering over the copy button.")]
        public object? CopyButtonToolTip
        {
            get => GetValue(CopyButtonToolTipProperty);
            set => SetValue(CopyButtonToolTipProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowToast"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowToastProperty = DependencyProperty.Register(
            nameof(ShowToast), typeof(bool), typeof(CopyTextBox), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether a <see cref="ToastMessage"/> is shown reporting
        /// whether the copy succeeded.  This is off by default.
        /// </summary>
        [Category("Behavior")]
        [Description("Shows a toast notification reporting whether the copy succeeded.  Off by default.")]
        public bool ShowToast
        {
            get => (bool)GetValue(ShowToastProperty);
            set => SetValue(ShowToastProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ToastManager"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ToastManagerProperty = DependencyProperty.Register(
            nameof(ToastManager), typeof(ToastNotifier), typeof(CopyTextBox), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="ToastNotifier"/> used to display notifications.  When null the
        /// control falls back to <see cref="ToastNotifier.Default"/> and then to a manager created
        /// over the containing window's content.
        /// </summary>
        [Category("Behavior")]
        [Description("The ToastManager used to display notifications.  Falls back to ToastManager.Default when null.")]
        public ToastNotifier? ToastManager
        {
            get => (ToastNotifier?)GetValue(ToastManagerProperty);
            set => SetValue(ToastManagerProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ToastQuadrant"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ToastQuadrantProperty = DependencyProperty.Register(
            nameof(ToastQuadrant), typeof(ToastQuadrant), typeof(CopyTextBox),
            new FrameworkPropertyMetadata(Controls.ToastQuadrant.BottomRight));

        /// <summary>
        /// Gets or sets the quadrant the toast notification is displayed in.
        /// </summary>
        [Category("Behavior")]
        [Description("The quadrant the toast notification is displayed in.")]
        public ToastQuadrant ToastQuadrant
        {
            get => (ToastQuadrant)GetValue(ToastQuadrantProperty);
            set => SetValue(ToastQuadrantProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ToastDuration"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ToastDurationProperty = DependencyProperty.Register(
            nameof(ToastDuration), typeof(TimeSpan?), typeof(CopyTextBox),
            new FrameworkPropertyMetadata(TimeSpan.FromSeconds(3)));

        /// <summary>
        /// Gets or sets how long the toast notification stays open.  When null the toast remains
        /// open until the user closes it.
        /// </summary>
        [Category("Behavior")]
        [Description("How long the toast notification stays open.  Null keeps it open until closed by the user.")]
        public TimeSpan? ToastDuration
        {
            get => (TimeSpan?)GetValue(ToastDurationProperty);
            set => SetValue(ToastDurationProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ToastSuccessTitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ToastSuccessTitleProperty = DependencyProperty.Register(
            nameof(ToastSuccessTitle), typeof(string), typeof(CopyTextBox), new FrameworkPropertyMetadata("Copied"));

        /// <summary>
        /// Gets or sets the title of the toast shown after a successful copy.
        /// </summary>
        [Category("Behavior")]
        [Description("The title of the toast shown after a successful copy.")]
        public string ToastSuccessTitle
        {
            get => (string)GetValue(ToastSuccessTitleProperty);
            set => SetValue(ToastSuccessTitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ToastSuccessMessage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ToastSuccessMessageProperty = DependencyProperty.Register(
            nameof(ToastSuccessMessage), typeof(string), typeof(CopyTextBox),
            new FrameworkPropertyMetadata("The text was copied to the clipboard."));

        /// <summary>
        /// Gets or sets the body of the toast shown after a successful copy.
        /// </summary>
        [Category("Behavior")]
        [Description("The body of the toast shown after a successful copy.")]
        public string ToastSuccessMessage
        {
            get => (string)GetValue(ToastSuccessMessageProperty);
            set => SetValue(ToastSuccessMessageProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ToastErrorTitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ToastErrorTitleProperty = DependencyProperty.Register(
            nameof(ToastErrorTitle), typeof(string), typeof(CopyTextBox), new FrameworkPropertyMetadata("Copy Failed"));

        /// <summary>
        /// Gets or sets the title of the toast shown when the copy fails.
        /// </summary>
        [Category("Behavior")]
        [Description("The title of the toast shown when the copy fails.")]
        public string ToastErrorTitle
        {
            get => (string)GetValue(ToastErrorTitleProperty);
            set => SetValue(ToastErrorTitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ToastErrorMessage"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ToastErrorMessageProperty = DependencyProperty.Register(
            nameof(ToastErrorMessage), typeof(string), typeof(CopyTextBox),
            new FrameworkPropertyMetadata("The text could not be copied to the clipboard."));

        /// <summary>
        /// Gets or sets the body of the toast shown when the copy fails.  When left at its default
        /// the operating system's error message is appended.
        /// </summary>
        [Category("Behavior")]
        [Description("The body of the toast shown when the copy fails.")]
        public string ToastErrorMessage
        {
            get => (string)GetValue(ToastErrorMessageProperty);
            set => SetValue(ToastErrorMessageProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command), typeof(ICommand), typeof(CopyTextBox), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets a command executed after the copy has been attempted.
        /// </summary>
        [Category("Action")]
        [Description("A command executed after the copy has been attempted.")]
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter), typeof(object), typeof(CopyTextBox), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="Command"/>.  When null the control's
        /// <see cref="Text"/> is passed instead.
        /// </summary>
        [Category("Action")]
        [Description("The parameter passed to Command.  Defaults to the control's Text when null.")]
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="TextCopied"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TextCopiedEvent = EventManager.RegisterRoutedEvent(
            nameof(TextCopied), RoutingStrategy.Bubble, typeof(EventHandler<TextCopiedEventArgs>), typeof(CopyTextBox));

        /// <summary>
        /// Occurs after a copy has been attempted, reporting whether it succeeded.
        /// </summary>
        public event EventHandler<TextCopiedEventArgs> TextCopied
        {
            add => AddHandler(TextCopiedEvent, value);
            remove => RemoveHandler(TextCopiedEvent, value);
        }

        #endregion

        /// <summary>
        /// Initializes static members of the <see cref="CopyTextBox"/> class.
        /// </summary>
        static CopyTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CopyTextBox), new FrameworkPropertyMetadata(typeof(CopyTextBox)));
            HeightProperty.OverrideMetadata(typeof(CopyTextBox), new FrameworkPropertyMetadata(28.0));
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_copyButton != null)
            {
                _copyButton.Click -= this.OnCopyButtonClick;
            }

            _textBox = GetTemplateChild(PartTextBox) as TextBox;
            _copyButton = GetTemplateChild(PartCopyButton) as ButtonBase;

            if (_copyButton != null)
            {
                _copyButton.Click += this.OnCopyButtonClick;
            }
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new CopyTextBoxAutomationPeer(this);
        }

        /// <summary>
        /// Places the control's <see cref="Text"/> onto the clipboard, raises
        /// <see cref="TextCopied"/> and, when <see cref="ShowToast"/> is set, displays a toast
        /// notification reporting the outcome.
        /// </summary>
        /// <returns>True when the text was placed onto the clipboard.</returns>
        public bool Copy()
        {
            var text = this.Text ?? string.Empty;
            var exception = this.WriteToClipboard(text);
            var successful = exception == null;

            if (this.ShowToast)
            {
                this.ShowCopyToast(successful, exception);
            }

            this.RaiseEvent(new TextCopiedEventArgs(TextCopiedEvent, this, text, successful, exception));

            var parameter = this.CommandParameter ?? text;

            if (this.Command?.CanExecute(parameter) == true)
            {
                this.Command.Execute(parameter);
            }

            return successful;
        }

        /// <summary>
        /// Selects all of the text and moves keyboard focus into the text box.
        /// </summary>
        public void SelectAllText()
        {
            _textBox?.Focus();
            _textBox?.SelectAll();
        }

        /// <summary>
        /// Writes the text to the clipboard, retrying a small number of times because the clipboard
        /// is a shared operating system resource another process may briefly hold open.
        /// </summary>
        /// <param name="text">The text to place onto the clipboard.</param>
        /// <returns>The exception the clipboard threw, or null when the write succeeded.</returns>
        private Exception? WriteToClipboard(string text)
        {
            Exception? last = null;

            for (int attempt = 0; attempt < ClipboardRetryCount; attempt++)
            {
                try
                {
                    if (text.Length == 0)
                    {
                        Clipboard.Clear();
                    }
                    else
                    {
                        // Copy=true leaves the data on the clipboard after this process exits.
                        Clipboard.SetDataObject(text, true);
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    last = ex;
                }
            }

            return last;
        }

        /// <summary>
        /// Shows the success or failure toast for a copy attempt.  Toast display is best effort:
        /// when no adorner layer is available the notification is skipped rather than throwing.
        /// </summary>
        /// <param name="successful">Whether the copy succeeded.</param>
        /// <param name="exception">The exception the clipboard threw, if any.</param>
        private void ShowCopyToast(bool successful, Exception? exception)
        {
            var manager = this.ResolveToastManager();

            if (manager == null)
            {
                return;
            }

            var title = successful ? this.ToastSuccessTitle : this.ToastErrorTitle;
            var message = successful ? this.ToastSuccessMessage : this.ToastErrorMessage;

            if (!successful && exception != null)
            {
                message = $"{message}  {exception.Message}";
            }

            try
            {
                manager.Show(title, message, successful ? ToastSeverity.Success : ToastSeverity.Error, this.ToastDuration, this.ToastQuadrant);
            }
            catch (InvalidOperationException)
            {
                // No adorner layer above the host element, nothing to display the toast on.
            }
        }

        /// <summary>
        /// Resolves the manager used to display toasts: the <see cref="ToastManager"/> property, then
        /// <see cref="ToastNotifier.Default"/>, then the shared manager for the surface the control is
        /// displayed on.
        /// </summary>
        private ToastNotifier? ResolveToastManager()
        {
            return this.ToastManager ?? ToastNotifier.Default ?? ToastNotifier.ForElement(this);
        }

        /// <summary>
        /// Occurs when the copy button is clicked.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> that contains the event data.</param>
        private void OnCopyButtonClick(object sender, RoutedEventArgs e)
        {
            this.Copy();
        }
    }
}
