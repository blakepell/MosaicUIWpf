/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.Input;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a hyperlink control that displays text and provides navigation functionality.
    /// </summary>
    /// <remarks>The <see cref="Hyperlink"/> class allows you to create a clickable link that can navigate to
    /// a specified URI or execute an ICommand.
    /// </remarks>
    public class Hyperlink : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        /// <remarks>This field is used to register and reference the <see cref="Text"/> property in the
        /// dependency property system.</remarks>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(Hyperlink), new PropertyMetadata(default(string)));

        /// <summary>
        /// Gets or sets the text content that is displayed for the Hyperlink.
        /// </summary>
        public string? Text
        {
            get => (string?)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="NavigateUri"/> dependency property, which represents the URI to navigate to when
        /// the hyperlink is activated.
        /// </summary>
        /// <remarks>This property is used to store the navigation target for a <see cref="Hyperlink"/>
        /// control.  The value can be set to a valid URI string, and the hyperlink will navigate to that URI when
        /// clicked.  This shells the URI with Explorer causing it to take the system default action.</remarks>
        public static readonly DependencyProperty NavigateUrlProperty = DependencyProperty.Register(
            nameof(NavigateUri), typeof(string), typeof(Hyperlink), new PropertyMetadata(default(string)));

        /// <summary>
        /// The URL that should be navigated to.
        /// </summary>
        public string NavigateUri
        {
            get => (string)GetValue(NavigateUrlProperty);
            set => SetValue(NavigateUrlProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HoverBrush"/> dependency property, which specifies the brush used to render the
        /// hyperlink when hovered.
        /// </summary>
        public static readonly DependencyProperty HoverBrushProperty = DependencyProperty.Register(nameof(HoverBrush), typeof(Brush), typeof(Hyperlink), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to render the hover state of the control.
        /// </summary>
        public Brush HoverBrush
        {
            get => (Brush)GetValue(HoverBrushProperty);
            set => SetValue(HoverBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                nameof(Command), typeof(ICommand), typeof(Hyperlink), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command that will be executed when the hyperlink is clicked.
        /// </summary>
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter), typeof(object), typeof(Hyperlink), new PropertyMetadata(default(object?)));

        /// <summary>
        /// Gets or sets the parameter to pass to the command when executed.
        /// </summary>
        public object? CommandParameter
        {
            get => (object?)GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EnableAutoToolTip"/> dependency property, which determines whether automatic
        /// tooltips are enabled for the hyperlink.
        /// </summary>
        public static readonly DependencyProperty EnableAutoToolTipProperty = DependencyProperty.Register(
            nameof(EnableAutoToolTip), typeof(bool), typeof(Hyperlink), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether automatic tooltips are enabled.
        /// </summary>
        public bool EnableAutoToolTip
        {
            get => (bool)GetValue(EnableAutoToolTipProperty);
            set => SetValue(EnableAutoToolTipProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TextWrapping"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(Hyperlink), new PropertyMetadata(default(TextWrapping)));

        /// <summary>
        /// Gets or sets the text wrapping behavior for the content within the control.
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TextTrimming"/> dependency property, which determines how text is trimmed when it
        /// overflows the layout bounds.
        /// </summary>
        public static readonly DependencyProperty TextTrimmingProperty = DependencyProperty.Register(nameof(TextTrimming), typeof(TextTrimming), typeof(Hyperlink), new PropertyMetadata(default(TextTrimming)));

        /// <summary>
        /// Gets or sets the text trimming behavior for the control.
        /// </summary>
        public TextTrimming TextTrimming
        {
            get => (TextTrimming)GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HasVisited"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasVisitedProperty = DependencyProperty.Register(nameof(HasVisited), typeof(bool), typeof(Hyperlink), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the user has visited the specified location in this session.
        /// </summary>
        public bool HasVisited
        {
            get => (bool)GetValue(HasVisitedProperty);
            set => SetValue(HasVisitedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ChangeVisitedLinkColor"/> dependency property, which determines whether the color
        /// of visited hyperlinks should change.
        /// </summary>
        public static readonly DependencyProperty ChangeVisitedLinkColorProperty = DependencyProperty.Register(nameof(ChangeVisitedLinkColor), typeof(bool), typeof(Hyperlink), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the color of visited links should be changed.
        /// </summary>
        public bool ChangeVisitedLinkColor
        {
            get => (bool)GetValue(ChangeVisitedLinkColorProperty);
            set => SetValue(ChangeVisitedLinkColorProperty, value);
        }

        /// <summary>
        /// Static initialization of the <see cref="Hyperlink"/> class.
        /// </summary>
        static Hyperlink()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Hyperlink), new FrameworkPropertyMetadata(typeof(Hyperlink)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hyperlink"/> class.
        /// </summary>
        public Hyperlink()
        {
            // Set default Foreground to the dynamic resource for HyperLinkBrushKey
            if (Application.Current.TryFindResource("HyperLinkBrush") != null)
            {
                SetResourceReference(ForegroundProperty, "HyperLinkBrush");
            }
            
            if (Application.Current.TryFindResource("HyperLinkHoverBrush") != null)
            {
                SetResourceReference(HoverBrushProperty, "HyperLinkHoverBrush");
            }
        }

        /// <summary>
        /// Code to execute when the link is clicked.  By default, this will shell Windows Explorer
        /// with the NavigationUri specified.
        /// </summary>
        public RelayCommand OnClick => new(() =>
        {
            this.HasVisited = true;

            // If this is a command hyperlink, execute the command with the provided parameter first.
            if (Command != null && Command.CanExecute(CommandParameter))
            {
                Command.Execute(CommandParameter);
                return;
            }

            // If this is a NavigateUri hyperlink, shell the URI with Explorer.
            if (string.IsNullOrWhiteSpace(NavigateUri))
            {
                return;
            }

            try
            {
                Process.Start($"explorer.exe", NavigateUri);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
    }
}
