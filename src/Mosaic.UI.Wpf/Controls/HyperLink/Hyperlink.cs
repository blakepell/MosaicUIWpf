/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.Input;
using Mosaic.UI.Wpf.Themes;

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

        private static readonly DependencyPropertyKey DisplayTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(DisplayText), typeof(string), typeof(Hyperlink), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="DisplayText"/> read-only dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayTextProperty = DisplayTextPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the text that the control template renders.  This is <see cref="Text"/> when it is set, otherwise it
        /// falls back to <see cref="NavigateUri"/>.
        /// </summary>
        /// <remarks>
        /// This is recalculated whenever <see cref="Text"/> or <see cref="NavigateUri"/> changes, which is what allows
        /// the rendered text to stay correct when the control is reused (for example a recycled row in a virtualized
        /// list) or when it is bound to a view model property that updates after the first render.
        /// </remarks>
        public string DisplayText => (string)GetValue(DisplayTextProperty);

        private static readonly DependencyPropertyKey AutoToolTipPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(AutoToolTip), typeof(object), typeof(Hyperlink), new FrameworkPropertyMetadata(default(object)));

        /// <summary>
        /// Identifies the <see cref="AutoToolTip"/> read-only dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoToolTipProperty = AutoToolTipPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the tooltip content that the control template renders.
        /// </summary>
        /// <remarks>
        /// This is recalculated whenever <see cref="ToolTip"/>, <see cref="NavigateUri"/>, <see cref="Command"/>, or
        /// <see cref="EnableAutoToolTip"/> changes so that a reused or re-bound control never shows a stale tooltip.
        /// </remarks>
        public object? AutoToolTip => GetValue(AutoToolTipProperty);

        /// <summary>
        /// Static initialization of the <see cref="Hyperlink"/> class.
        /// </summary>
        static Hyperlink()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Hyperlink), new FrameworkPropertyMetadata(typeof(Hyperlink)));
        }

        /// <summary>
        /// Recomputes the derived <see cref="DisplayText"/> and <see cref="AutoToolTip"/> values when any of the
        /// properties they are built from changes.
        /// </summary>
        /// <param name="e">The property change details.</param>
        /// <remarks>
        /// The template cannot bind directly to the <see cref="Hyperlink"/> instance and run a converter over it,
        /// because such a binding only re-evaluates when the source object reference changes, never when a property on
        /// that object changes.  Projecting the values onto dependency properties here gives the template a source that
        /// raises change notifications.
        /// </remarks>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == TextProperty || e.Property == NavigateUrlProperty)
            {
                this.UpdateDisplayText();
            }

            if (e.Property == ToolTipProperty || e.Property == NavigateUrlProperty || e.Property == CommandProperty || e.Property == EnableAutoToolTipProperty)
            {
                this.UpdateAutoToolTip();
            }
        }

        /// <summary>
        /// Recalculates <see cref="DisplayText"/> from the current <see cref="Text"/> and <see cref="NavigateUri"/>.
        /// </summary>
        private void UpdateDisplayText()
        {
            SetValue(DisplayTextPropertyKey, this.Text ?? this.NavigateUri ?? string.Empty);
        }

        /// <summary>
        /// Recalculates <see cref="AutoToolTip"/> from the current tooltip, URI, and command state.
        /// </summary>
        private void UpdateAutoToolTip()
        {
            SetValue(AutoToolTipPropertyKey, this.ResolveAutoToolTip());
        }

        /// <summary>
        /// Determines the tooltip content that should be shown for the link.
        /// </summary>
        /// <returns>
        /// The explicitly assigned <see cref="ToolTip"/> if one is set, otherwise the <see cref="NavigateUri"/> so the
        /// user can see where the link goes, otherwise a generic message when the link only executes a
        /// <see cref="Command"/>.  Returns <see langword="null"/> when there is nothing to show or when
        /// <see cref="EnableAutoToolTip"/> is <see langword="false"/>.
        /// </returns>
        private object? ResolveAutoToolTip()
        {
            if (!this.EnableAutoToolTip)
            {
                return null;
            }

            if (this.ToolTip != null)
            {
                return this.ToolTip;
            }

            // Otherwise, if the NavigateUri is set, return it.  This is useful if a link displays text, but is going
            // to navigate to a URI, to let the user know where it's going.
            if (!string.IsNullOrWhiteSpace(this.NavigateUri))
            {
                return this.NavigateUri;
            }

            // Let the user know that this link will execute code if it has a command set.
            if (this.Command != null)
            {
                return "This link will execute code defined by the application.";
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hyperlink"/> class.
        /// </summary>
        public Hyperlink()
        {
            // Set default Foreground to the dynamic resource for HyperLinkBrushKey
            if (Application.Current?.TryFindResource(MosaicTheme.HyperLinkBrush) != null)
            {
                SetResourceReference(ForegroundProperty, MosaicTheme.HyperLinkBrush);
            }

            if (Application.Current?.TryFindResource(MosaicTheme.HyperLinkHoverBrush) != null)
            {
                SetResourceReference(HoverBrushProperty, MosaicTheme.HyperLinkHoverBrush);
            }
        }

        /// <summary>
        /// Backing field for <see cref="OnClick"/> so the same command instance is handed out on every get.
        /// </summary>
        private RelayCommand? _onClick;

        /// <summary>
        /// Code to execute when the link is clicked.  By default, this will shell Windows Explorer
        /// with the NavigationUri specified.
        /// </summary>
        public RelayCommand OnClick => _onClick ??= new RelayCommand(() =>
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
