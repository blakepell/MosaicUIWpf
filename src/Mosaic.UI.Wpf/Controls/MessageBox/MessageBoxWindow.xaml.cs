/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Media.Imaging;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// The themed window that backs <see cref="MessageBox"/>.  It renders a chrome-less, two-tone
    /// dialog with a title and close button across the top, an icon and message in the middle, and
    /// the action buttons along the bottom.
    /// </summary>
    /// <remarks>
    /// This type is intentionally not public; consumers interact with it exclusively through the
    /// <see cref="MessageBox"/> static façade, which mirrors <see cref="System.Windows.MessageBox"/>.
    /// </remarks>
    internal partial class MessageBoxWindow : Window
    {
        /// <summary>
        /// The result that is returned when the dialog is dismissed via the close button or the
        /// <see cref="Key.Escape"/> key.
        /// </summary>
        private readonly MessageBoxResult _cancelResult;

        /// <summary>
        /// Gets the result selected by the user.
        /// </summary>
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBoxWindow"/> class.
        /// </summary>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar caption.</param>
        /// <param name="button">The set of buttons to display.</param>
        /// <param name="icon">The icon to display next to the message.</param>
        /// <param name="defaultResult">The button that should be the default (focused) button.</param>
        /// <param name="options">Display and reading-order options.</param>
        public MessageBoxWindow(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            InitializeComponent();

            this.Title = caption ?? string.Empty;
            TitleTextBlock.Text = caption ?? string.Empty;
            MessageTextBlock.Text = messageBoxText ?? string.Empty;

            ApplyOptions(options);
            SetIcon(icon);

            _cancelResult = GetCancelResult(button);
            BuildButtons(button, defaultResult);

            this.KeyDown += OnKeyDown;
        }

        /// <summary>
        /// Applies <see cref="MessageBoxOptions"/> that affect flow direction and text alignment.
        /// </summary>
        private void ApplyOptions(MessageBoxOptions options)
        {
            if (options.HasFlag(MessageBoxOptions.RtlReading))
            {
                this.FlowDirection = FlowDirection.RightToLeft;
            }

            if (options.HasFlag(MessageBoxOptions.RightAlign))
            {
                MessageTextBlock.TextAlignment = TextAlignment.Right;
                TitleTextBlock.TextAlignment = TextAlignment.Right;
            }
        }

        /// <summary>
        /// Resolves and assigns the icon image for the supplied <see cref="MessageBoxImage"/>.  When
        /// <see cref="MessageBoxImage.None"/> is specified the icon is hidden entirely.
        /// </summary>
        private void SetIcon(MessageBoxImage icon)
        {
            // Several enum members share the same numeric value (e.g. Hand/Stop/Error == 16), so the
            // numeric value is the reliable discriminator.
            string? fileName = (int)icon switch
            {
                16 => "error-48.png",   // Error / Hand / Stop
                32 => "question-48.png", // Question
                48 => "warning-48.png",  // Warning / Exclamation
                64 => "info-48.png",     // Information / Asterisk
                _ => null                // None
            };

            if (fileName is null)
            {
                IconImage.Visibility = Visibility.Collapsed;
                IconImage.Margin = new Thickness(0);
                return;
            }

            var uri = new Uri($"pack://application:,,,/Mosaic.UI.Wpf;component/Assets/Images/{fileName}", UriKind.Absolute);
            IconImage.Source = new BitmapImage(uri);
        }

        /// <summary>
        /// Determines which result is returned when the dialog is closed without an explicit choice
        /// (close button or <see cref="Key.Escape"/>).
        /// </summary>
        private static MessageBoxResult GetCancelResult(MessageBoxButton button)
        {
            return button switch
            {
                MessageBoxButton.OK => MessageBoxResult.OK,
                MessageBoxButton.OKCancel => MessageBoxResult.Cancel,
                MessageBoxButton.YesNoCancel => MessageBoxResult.Cancel,
                MessageBoxButton.YesNo => MessageBoxResult.No,
                _ => MessageBoxResult.None
            };
        }

        /// <summary>
        /// Builds the action buttons for the supplied <see cref="MessageBoxButton"/> set and applies
        /// the default-button styling/focus.
        /// </summary>
        private void BuildButtons(MessageBoxButton button, MessageBoxResult defaultResult)
        {
            var results = button switch
            {
                MessageBoxButton.OK => new[] { MessageBoxResult.OK },
                MessageBoxButton.OKCancel => new[] { MessageBoxResult.OK, MessageBoxResult.Cancel },
                MessageBoxButton.YesNo => new[] { MessageBoxResult.Yes, MessageBoxResult.No },
                MessageBoxButton.YesNoCancel => new[] { MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel },
                _ => new[] { MessageBoxResult.OK }
            };

            // If the requested default is not one of the available buttons, fall back to the first.
            if (Array.IndexOf(results, defaultResult) < 0)
            {
                defaultResult = results[0];
            }

            AccentButton? defaultButton = null;

            foreach (var result in results)
            {
                bool isDefault = result == defaultResult;

                var btn = new AccentButton
                {
                    Content = GetButtonText(result),
                    Margin = new Thickness(8, 0, 0, 0),
                    MinWidth = 80,
                    AccentButtonType = isDefault ? AccentButtonType.ThemeAccent : AccentButtonType.Gray,
                    IsDefault = isDefault,
                    Tag = result
                };

                btn.Click += OnButtonClick;
                ButtonPanel.Children.Add(btn);

                if (isDefault)
                {
                    defaultButton = btn;
                }
            }

            if (defaultButton is not null)
            {
                this.Loaded += (_, _) => defaultButton.Focus();
            }
        }

        /// <summary>
        /// Gets the display text for a given <see cref="MessageBoxResult"/>.
        /// </summary>
        private static string GetButtonText(MessageBoxResult result)
        {
            return result switch
            {
                MessageBoxResult.OK => "OK",
                MessageBoxResult.Cancel => "Cancel",
                MessageBoxResult.Yes => "Yes",
                MessageBoxResult.No => "No",
                _ => result.ToString()
            };
        }

        /// <summary>
        /// Handles a click on one of the action buttons by recording the result and closing.
        /// </summary>
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: MessageBoxResult result })
            {
                Result = result;
                Close();
            }
        }

        /// <summary>
        /// Handles a click on the close button.
        /// </summary>
        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Result = _cancelResult;
            Close();
        }

        /// <summary>
        /// Allows the window to be dragged by its title bar.
        /// </summary>
        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// Dismisses the dialog with the cancel result when the user presses <see cref="Key.Escape"/>.
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Result = _cancelResult;
                Close();
            }
        }
    }
}
