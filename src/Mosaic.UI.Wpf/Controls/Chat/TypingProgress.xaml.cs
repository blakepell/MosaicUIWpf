/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a control that visually indicates typing progress, typically used in chat or messaging scenarios.
    /// </summary>
    /// <remarks>
    /// The <see cref="TypingProgress"/> control displays an animated visual effect to indicate that typing is in progress.
    /// The animation is controlled by the <see cref="IsRunning"/>
    /// property, which determines whether the animation is active. The control uses a storyboard resource named
    /// "TypingStoryboard" to define the animation, which must be present in the control's resources.  The control
    /// automatically adjusts its visibility based on the value of <see cref="IsRunning"/>: <list type="bullet">
    /// <item><description>When <see cref="IsRunning"/> is <see langword="true"/>, the control becomes visible and the
    /// animation starts.</description></item> <item><description>When <see cref="IsRunning"/> is <see
    /// langword="false"/>, the control becomes hidden and the animation stops.</description></item> </list>
    /// </remarks>
    public partial class TypingProgress
    {
        private Storyboard? _typingStoryboard;

        /// <summary>
        /// Identifies the <see cref="IsRunning"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
            nameof(IsRunning), typeof(bool), typeof(TypingProgress), new PropertyMetadata(false, OnIsRunningChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the typing animation is currently running.
        /// </summary>
        public bool IsRunning
        {
            get => (bool)GetValue(IsRunningProperty);
            set => SetValue(IsRunningProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypingProgress"/> class.
        /// </summary>
        public TypingProgress()
        {
            InitializeComponent();
            Loaded += TypingProgress_Loaded;
            Unloaded += TypingProgress_Unloaded;

            // Ensure initial visibility matches default property value
            Visibility = IsRunning ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Handles the Loaded event for the TypingProgress control, initializing and starting the typing animation if
        /// applicable.
        /// </summary>
        private void TypingProgress_Loaded(object? sender, RoutedEventArgs e)
        {
            // Try to retrieve the storyboard resource
            if (TryFindResource("TypingStoryboard") is Storyboard sb)
            {
                // Assign concrete targets to avoid issues when the storyboard is defined in resources
                // Expect three DoubleAnimation children in the same order as defined in XAML
                if (sb.Children.Count >= 3)
                {
                    if (Bubble1 != null)
                    {
                        Storyboard.SetTarget(sb.Children[0], Bubble1);
                        Storyboard.SetTargetProperty(sb.Children[0], new PropertyPath("Opacity"));
                    }

                    if (Bubble2 != null)
                    {
                        Storyboard.SetTarget(sb.Children[1], Bubble2);
                        Storyboard.SetTargetProperty(sb.Children[1], new PropertyPath("Opacity"));
                    }

                    if (Bubble3 != null)
                    {
                        Storyboard.SetTarget(sb.Children[2], Bubble3);
                        Storyboard.SetTargetProperty(sb.Children[2], new PropertyPath("Opacity"));
                    }
                }

                _typingStoryboard = sb;

                // Start the animation only if IsRunning is true
                if (IsRunning)
                {
                    _typingStoryboard.Begin(this, true);
                }
            }
        }

        /// <summary>
        /// Handles the Unloaded event for the TypingProgress control.
        /// </summary>
        /// <remarks>This method ensures that any associated storyboard is removed and resources are
        /// released  when the control is unloaded.</remarks>
        /// <param name="sender">The source of the event. This can be <see langword="null"/>.</param>
        /// <param name="e">The event data associated with the Unloaded event.</param>
        private void TypingProgress_Unloaded(object? sender, RoutedEventArgs e)
        {
            if (_typingStoryboard is not null)
            {
                _typingStoryboard.Remove(this);
                _typingStoryboard = null;
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="IsRunning"/> dependency property.
        /// </summary>
        private static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TypingProgress control && e.NewValue is bool isRunning)
            {
                control.OnIsRunningChanged(isRunning);
            }
        }

        /// <summary>
        /// Handles changes to the running state of the control.
        /// </summary>
        /// <remarks>
        /// This method updates the visibility of the control based on the <paramref name="isRunning"/>  parameter and
        /// starts or stops the associated storyboard animation if it is loaded.
        /// </remarks>
        /// <param name="isRunning">A value indicating whether the control is in a running state.  <see langword="true"/> to make the control
        /// visible and start the animation;  otherwise, <see langword="false"/> to hide the control and stop the
        /// animation.</param>
        private void OnIsRunningChanged(bool isRunning)
        {
            // Update control visibility immediately
            Visibility = isRunning ? Visibility.Visible : Visibility.Hidden;

            // Start or stop the storyboard if it is already loaded
            if (_typingStoryboard is not null)
            {
                if (isRunning)
                {
                    // Begin (or restart) the animation targeting this control
                    _typingStoryboard.Begin(this, true);
                }
                else
                {
                    // Stop and remove the animation from this control
                    _typingStoryboard.Remove(this);
                }
            }
        }

        /// <summary>
        /// Starts the operation by setting the running state to active.
        /// </summary>
        /// <remarks>This method sets the <see cref="IsRunning"/> property to <see langword="true"/>. 
        /// Ensure that any necessary preconditions are met before calling this method.</remarks>
        public void Start()
        {
            this.IsRunning = true;
        }

        /// <summary>
        /// Stops the operation and sets the running state to inactive.
        /// </summary>
        /// <remarks>This method sets the <see cref="IsRunning"/> property to <see langword="false"/>, 
        /// indicating that the operation is no longer running.</remarks>
        public void Stop()
        {
            this.IsRunning = false;
        }
    }
}