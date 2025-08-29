/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a stopwatch control that provides functionality to display a stopwatch timer as UI element.
    /// </summary>
    public class StopwatchDisplay : ContentControl
    {
        private readonly Stopwatch _stopwatch;
        private readonly DispatcherTimer _timer;
        private TextBlock? _textBlock;

        /// <summary>
        /// Identifies the <see cref="ShowMilliseconds"/> dependency property, which determines whether the stopwatch
        /// display includes milliseconds.
        /// </summary>
        public static readonly DependencyProperty ShowMillisecondsProperty = DependencyProperty.Register(
            nameof(ShowMilliseconds), typeof(bool), typeof(StopwatchDisplay), new PropertyMetadata(true, OnShowMillisecondsChanged));

        /// <summary>
        /// Gets or sets a value indicating whether milliseconds should be displayed.
        /// </summary>
        public bool ShowMilliseconds
        {
            get => (bool)GetValue(ShowMillisecondsProperty);
            set => SetValue(ShowMillisecondsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ElapsedTime"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ElapsedTimeProperty = DependencyProperty.Register(
            nameof(ElapsedTime), typeof(TimeSpan), typeof(StopwatchDisplay), new PropertyMetadata(TimeSpan.Zero));

        /// <summary>
        /// Gets the total elapsed time represented as a <see cref="TimeSpan"/>.
        /// </summary>
        public TimeSpan ElapsedTime
        {
            get => (TimeSpan)GetValue(ElapsedTimeProperty);
            private set => SetValue(ElapsedTimeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsRunning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
            nameof(IsRunning), typeof(bool), typeof(StopwatchDisplay), new PropertyMetadata(false));

        /// <summary>
        /// Gets a value indicating whether the process or operation is currently running.
        /// </summary>
        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            private set => SetValue(IsRunningProperty, value);
        }

        /// <summary>
        /// Initializes static members of the <see cref="StopwatchDisplay"/> class.
        /// </summary>
        static StopwatchDisplay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StopwatchDisplay), new FrameworkPropertyMetadata(typeof(StopwatchDisplay)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StopwatchDisplay"/> class, setting up the internal timer and event
        /// handlers.
        /// </summary>
        public StopwatchDisplay()
        {
            _stopwatch = new System.Diagnostics.Stopwatch();
            _timer = new DispatcherTimer();
            _timer.Tick += OnTimerTick;
            UpdateTimerInterval();
        }

        /// <summary>
        /// Invoked whenever application code or internal processes call <see cref="OnApplyTemplate"/>.  Ensures that the
        /// control's template contains the required elements and initializes the control's state.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _textBlock = GetTemplateChild("TextBlockContent") as TextBlock;

            if (_textBlock == null)
            {
                throw new InvalidOperationException("Template must contain a TextBlock with x:Name='TextBlockContent'.");
            }

            UpdateDisplay();
        }

        /// <summary>
        /// Starts the stopwatch and begins updating the elapsed time display.
        /// </summary>
        public void Start()
        {
            if (!_stopwatch.IsRunning)
            {
                _stopwatch.Start();
                _timer.Start();
                IsRunning = true;
            }
        }

        /// <summary>
        /// Stops the timer and the associated stopwatch, if they are currently running.
        /// </summary>
        public void Stop()
        {
            if (_stopwatch.IsRunning)
            {
                _stopwatch.Stop();
                _timer.Stop();
                IsRunning = false;
            }
        }

        /// <summary>
        /// Resets the timer to its initial state.
        /// </summary>
        public void Reset()
        {
            _stopwatch.Reset();
            _timer.Stop();
            IsRunning = false;
            ElapsedTime = TimeSpan.Zero;
            UpdateDisplay();
        }

        /// <summary>
        /// Sets the interval for the timer in milliseconds.
        /// </summary>
        public void SetInterval(int milliseconds)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(milliseconds);
        }

        /// <summary>
        /// Handles changes to the <see cref="ShowMilliseconds"/> dependency property.
        /// </summary>
        /// <param name="d">The <see cref="DependencyObject"/> on which the property value has changed.</param>
        /// <param name="e">The event data containing information about the property change.</param>
        private static void OnShowMillisecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StopwatchDisplay stopwatch)
            {
                stopwatch.UpdateTimerInterval();
                stopwatch.UpdateDisplay();
            }
        }

        /// <summary>
        /// Updates the timer interval based on whether milliseconds are displayed.
        /// </summary>
        private void UpdateTimerInterval()
        {
            // Update more frequently when showing milliseconds, less frequently for seconds only
            _timer.Interval = ShowMilliseconds ? TimeSpan.FromMilliseconds(10) : TimeSpan.FromMilliseconds(250);
        }

        /// <summary>
        /// Handles the timer tick event and updates the elapsed time and display.
        /// </summary>
        private void OnTimerTick(object? sender, EventArgs e)
        {
            ElapsedTime = _stopwatch.Elapsed;
            UpdateDisplay();
        }

        /// <summary>
        /// Updates the display text of the associated text block to reflect the current elapsed time.
        /// </summary>
        private void UpdateDisplay()
        {
            if (_textBlock == null)
            {
                return;
            }

            _textBlock.Text = ElapsedTime.ToString(ShowMilliseconds ? @"hh\:mm\:ss\.fff" : @"hh\:mm\:ss");
        }
    }
}