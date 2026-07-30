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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A transport style control that exposes Play, Pause and Stop buttons suitable for a tool bar.
    /// Each button binds to its own command and is automatically disabled (and visually muted) when
    /// that command reports it cannot execute.
    /// </summary>
    [DefaultEvent(nameof(PlayClick))]
    [DefaultProperty(nameof(PlayCommand))]
    [TemplatePart(Name = PartPlayButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartPauseButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartStopButton, Type = typeof(ButtonBase))]
    public class ExecutionControl : Control
    {
        private const string PartPlayButton = "PART_PlayButton";
        private const string PartPauseButton = "PART_PauseButton";
        private const string PartStopButton = "PART_StopButton";

        private ButtonBase? _playButton;
        private ButtonBase? _pauseButton;
        private ButtonBase? _stopButton;

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="PlayCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(
            nameof(PlayCommand), typeof(ICommand), typeof(ExecutionControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command invoked when the play button is clicked.  The play button is
        /// disabled whenever this command reports that it cannot execute.
        /// </summary>
        [Category("Action")]
        [Description("Command invoked when the play button is clicked.")]
        public ICommand? PlayCommand
        {
            get => (ICommand?)GetValue(PlayCommandProperty);
            set => SetValue(PlayCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PlayCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlayCommandParameterProperty = DependencyProperty.Register(
            nameof(PlayCommandParameter), typeof(object), typeof(ExecutionControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="PlayCommand"/>.
        /// </summary>
        [Category("Action")]
        [Description("Optional parameter passed to the play command.")]
        public object? PlayCommandParameter
        {
            get => GetValue(PlayCommandParameterProperty);
            set => SetValue(PlayCommandParameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PauseCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PauseCommandProperty = DependencyProperty.Register(
            nameof(PauseCommand), typeof(ICommand), typeof(ExecutionControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command invoked when the pause button is clicked.  The pause button is
        /// disabled whenever this command reports that it cannot execute.
        /// </summary>
        [Category("Action")]
        [Description("Command invoked when the pause button is clicked.")]
        public ICommand? PauseCommand
        {
            get => (ICommand?)GetValue(PauseCommandProperty);
            set => SetValue(PauseCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PauseCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PauseCommandParameterProperty = DependencyProperty.Register(
            nameof(PauseCommandParameter), typeof(object), typeof(ExecutionControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="PauseCommand"/>.
        /// </summary>
        [Category("Action")]
        [Description("Optional parameter passed to the pause command.")]
        public object? PauseCommandParameter
        {
            get => GetValue(PauseCommandParameterProperty);
            set => SetValue(PauseCommandParameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StopCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StopCommandProperty = DependencyProperty.Register(
            nameof(StopCommand), typeof(ICommand), typeof(ExecutionControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command invoked when the stop button is clicked.  The stop button is
        /// disabled whenever this command reports that it cannot execute.
        /// </summary>
        [Category("Action")]
        [Description("Command invoked when the stop button is clicked.")]
        public ICommand? StopCommand
        {
            get => (ICommand?)GetValue(StopCommandProperty);
            set => SetValue(StopCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StopCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StopCommandParameterProperty = DependencyProperty.Register(
            nameof(StopCommandParameter), typeof(object), typeof(ExecutionControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="StopCommand"/>.
        /// </summary>
        [Category("Action")]
        [Description("Optional parameter passed to the stop command.")]
        public object? StopCommandParameter
        {
            get => GetValue(StopCommandParameterProperty);
            set => SetValue(StopCommandParameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowPlayButton"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowPlayButtonProperty = DependencyProperty.Register(
            nameof(ShowPlayButton), typeof(bool), typeof(ExecutionControl), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the play button is shown.
        /// </summary>
        [Category("Common")]
        [Description("Indicates whether the play button is shown.")]
        public bool ShowPlayButton
        {
            get => (bool)GetValue(ShowPlayButtonProperty);
            set => SetValue(ShowPlayButtonProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowPauseButton"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowPauseButtonProperty = DependencyProperty.Register(
            nameof(ShowPauseButton), typeof(bool), typeof(ExecutionControl), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the pause button is shown.
        /// </summary>
        [Category("Common")]
        [Description("Indicates whether the pause button is shown.")]
        public bool ShowPauseButton
        {
            get => (bool)GetValue(ShowPauseButtonProperty);
            set => SetValue(ShowPauseButtonProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowStopButton"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStopButtonProperty = DependencyProperty.Register(
            nameof(ShowStopButton), typeof(bool), typeof(ExecutionControl), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the stop button is shown.
        /// </summary>
        [Category("Common")]
        [Description("Indicates whether the stop button is shown.")]
        public bool ShowStopButton
        {
            get => (bool)GetValue(ShowStopButtonProperty);
            set => SetValue(ShowStopButtonProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PlayToolTip"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlayToolTipProperty = DependencyProperty.Register(
            nameof(PlayToolTip), typeof(object), typeof(ExecutionControl), new FrameworkPropertyMetadata("Play"));

        /// <summary>
        /// Gets or sets the tool tip shown for the play button.
        /// </summary>
        [Category("Common")]
        [Description("Tool tip shown for the play button.")]
        public object? PlayToolTip
        {
            get => GetValue(PlayToolTipProperty);
            set => SetValue(PlayToolTipProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PauseToolTip"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PauseToolTipProperty = DependencyProperty.Register(
            nameof(PauseToolTip), typeof(object), typeof(ExecutionControl), new FrameworkPropertyMetadata("Pause"));

        /// <summary>
        /// Gets or sets the tool tip shown for the pause button.
        /// </summary>
        [Category("Common")]
        [Description("Tool tip shown for the pause button.")]
        public object? PauseToolTip
        {
            get => GetValue(PauseToolTipProperty);
            set => SetValue(PauseToolTipProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StopToolTip"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StopToolTipProperty = DependencyProperty.Register(
            nameof(StopToolTip), typeof(object), typeof(ExecutionControl), new FrameworkPropertyMetadata("Stop"));

        /// <summary>
        /// Gets or sets the tool tip shown for the stop button.
        /// </summary>
        [Category("Common")]
        [Description("Tool tip shown for the stop button.")]
        public object? StopToolTip
        {
            get => GetValue(StopToolTipProperty);
            set => SetValue(StopToolTipProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IconSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
            nameof(IconSize), typeof(double), typeof(ExecutionControl), new FrameworkPropertyMetadata(16d));

        /// <summary>
        /// Gets or sets the rendered width and height of each transport icon.
        /// </summary>
        [Category("Layout")]
        [Description("Rendered width and height of each transport icon.")]
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ButtonPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ButtonPaddingProperty = DependencyProperty.Register(
            nameof(ButtonPadding), typeof(Thickness), typeof(ExecutionControl), new FrameworkPropertyMetadata(new Thickness(6, 3, 6, 3)));

        /// <summary>
        /// Gets or sets the padding applied around each transport icon.
        /// </summary>
        [Category("Layout")]
        [Description("Padding applied around each transport icon.")]
        public Thickness ButtonPadding
        {
            get => (Thickness)GetValue(ButtonPaddingProperty);
            set => SetValue(ButtonPaddingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Orientation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation), typeof(Orientation), typeof(ExecutionControl), new FrameworkPropertyMetadata(Orientation.Horizontal));

        /// <summary>
        /// Gets or sets the direction the transport buttons are laid out in.
        /// </summary>
        [Category("Layout")]
        [Description("Direction the transport buttons are laid out in.")]
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PlayBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlayBrushProperty = DependencyProperty.Register(
            nameof(PlayBrush), typeof(Brush), typeof(ExecutionControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the brush used to draw the play icon when it is enabled.
        /// </summary>
        [Category("Brushes")]
        [Description("Brush used to draw the play icon when it is enabled.")]
        public Brush? PlayBrush
        {
            get => (Brush?)GetValue(PlayBrushProperty);
            set => SetValue(PlayBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PauseBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PauseBrushProperty = DependencyProperty.Register(
            nameof(PauseBrush), typeof(Brush), typeof(ExecutionControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the brush used to draw the pause icon when it is enabled.
        /// </summary>
        [Category("Brushes")]
        [Description("Brush used to draw the pause icon when it is enabled.")]
        public Brush? PauseBrush
        {
            get => (Brush?)GetValue(PauseBrushProperty);
            set => SetValue(PauseBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StopBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StopBrushProperty = DependencyProperty.Register(
            nameof(StopBrush), typeof(Brush), typeof(ExecutionControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the brush used to draw the stop icon when it is enabled.
        /// </summary>
        [Category("Brushes")]
        [Description("Brush used to draw the stop icon when it is enabled.")]
        public Brush? StopBrush
        {
            get => (Brush?)GetValue(StopBrushProperty);
            set => SetValue(StopBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DisabledBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisabledBrushProperty = DependencyProperty.Register(
            nameof(DisabledBrush), typeof(Brush), typeof(ExecutionControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the brush used to draw an icon whose command cannot currently execute.
        /// </summary>
        [Category("Brushes")]
        [Description("Brush used to draw an icon whose command cannot currently execute.")]
        public Brush? DisabledBrush
        {
            get => (Brush?)GetValue(DisabledBrushProperty);
            set => SetValue(DisabledBrushProperty, value);
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="PlayClick"/> routed event.
        /// </summary>
        public static readonly RoutedEvent PlayClickEvent = EventManager.RegisterRoutedEvent(
            nameof(PlayClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExecutionControl));

        /// <summary>
        /// Occurs when the play button is clicked.
        /// </summary>
        [Category("Action")]
        [Description("Raised when the play button is clicked.")]
        public event RoutedEventHandler PlayClick
        {
            add => AddHandler(PlayClickEvent, value);
            remove => RemoveHandler(PlayClickEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="PauseClick"/> routed event.
        /// </summary>
        public static readonly RoutedEvent PauseClickEvent = EventManager.RegisterRoutedEvent(
            nameof(PauseClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExecutionControl));

        /// <summary>
        /// Occurs when the pause button is clicked.
        /// </summary>
        [Category("Action")]
        [Description("Raised when the pause button is clicked.")]
        public event RoutedEventHandler PauseClick
        {
            add => AddHandler(PauseClickEvent, value);
            remove => RemoveHandler(PauseClickEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="StopClick"/> routed event.
        /// </summary>
        public static readonly RoutedEvent StopClickEvent = EventManager.RegisterRoutedEvent(
            nameof(StopClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ExecutionControl));

        /// <summary>
        /// Occurs when the stop button is clicked.
        /// </summary>
        [Category("Action")]
        [Description("Raised when the stop button is clicked.")]
        public event RoutedEventHandler StopClick
        {
            add => AddHandler(StopClickEvent, value);
            remove => RemoveHandler(StopClickEvent, value);
        }

        #endregion

        static ExecutionControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ExecutionControl), new FrameworkPropertyMetadata(typeof(ExecutionControl)));
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            DetachButton(_playButton, OnPlayButtonClick);
            DetachButton(_pauseButton, OnPauseButtonClick);
            DetachButton(_stopButton, OnStopButtonClick);

            _playButton = GetTemplateChild(PartPlayButton) as ButtonBase;
            _pauseButton = GetTemplateChild(PartPauseButton) as ButtonBase;
            _stopButton = GetTemplateChild(PartStopButton) as ButtonBase;

            AttachButton(_playButton, OnPlayButtonClick);
            AttachButton(_pauseButton, OnPauseButtonClick);
            AttachButton(_stopButton, OnStopButtonClick);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ExecutionControlAutomationPeer(this);
        }

        private static void AttachButton(ButtonBase? button, RoutedEventHandler handler)
        {
            if (button != null)
            {
                button.Click += handler;
            }
        }

        private static void DetachButton(ButtonBase? button, RoutedEventHandler handler)
        {
            if (button != null)
            {
                button.Click -= handler;
            }
        }

        private void OnPlayButtonClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(PlayClickEvent, this));
        }

        private void OnPauseButtonClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(PauseClickEvent, this));
        }

        private void OnStopButtonClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(StopClickEvent, this));
        }
    }
}
