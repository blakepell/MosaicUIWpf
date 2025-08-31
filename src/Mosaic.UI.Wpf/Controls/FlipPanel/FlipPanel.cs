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

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Defines the direction of the flip animation.
    /// </summary>
    public enum FlipDirection
    {
        /// <summary>
        /// Flips horizontally (like a coin toss).
        /// </summary>
        Horizontal,
        /// <summary>
        /// Flips vertically (like a card flip).
        /// </summary>
        Vertical
    }

    /// <summary>
    /// A flip panel component that can display two different content sides and animate between them.
    /// </summary>
    public class FlipPanel : ContentControl
    {
        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="FrontContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FrontContentProperty = DependencyProperty.Register(
            nameof(FrontContent), typeof(object), typeof(FlipPanel), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content displayed on the front side of the panel.
        /// </summary>
        public object FrontContent
        {
            get => GetValue(FrontContentProperty);
            set => SetValue(FrontContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BackContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BackContentProperty = DependencyProperty.Register(
            nameof(BackContent), typeof(object), typeof(FlipPanel), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content displayed on the back side of the panel.
        /// </summary>
        public object BackContent
        {
            get => GetValue(BackContentProperty);
            set => SetValue(BackContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Direction"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(
            nameof(Direction), typeof(FlipDirection), typeof(FlipPanel), new PropertyMetadata(FlipDirection.Horizontal));

        /// <summary>
        /// Gets or sets the direction of the flip animation.
        /// </summary>
        public FlipDirection Direction
        {
            get => (FlipDirection)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsFlipped"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFlippedProperty = DependencyProperty.Register(
            nameof(IsFlipped), typeof(bool), typeof(FlipPanel), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the panel is currently showing the back content.
        /// </summary>
        public bool IsFlipped
        {
            get => (bool)GetValue(IsFlippedProperty);
            set => SetValue(IsFlippedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FlipDuration"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FlipDurationProperty = DependencyProperty.Register(
            nameof(FlipDuration), typeof(TimeSpan), typeof(FlipPanel), new PropertyMetadata(TimeSpan.FromMilliseconds(600)));

        /// <summary>
        /// Gets or sets the duration of the flip animation.
        /// </summary>
        public TimeSpan FlipDuration
        {
            get => (TimeSpan)GetValue(FlipDurationProperty);
            set => SetValue(FlipDurationProperty, value);
        }

        #endregion

        #region Private Fields

        private ContentPresenter? _frontContentPresenter;
        private ContentPresenter? _backContentPresenter;
        private bool _isAnimating;

        #endregion

        /// <summary>
        /// Initializes static metadata for the <see cref="FlipPanel"/> class.
        /// </summary>
        static FlipPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FlipPanel), new FrameworkPropertyMetadata(typeof(FlipPanel)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlipPanel"/> class.
        /// </summary>
        public FlipPanel()
        {

        }

        /// <summary>
        /// Called when the template is applied to the control.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _frontContentPresenter = Template?.FindName("PART_FrontContent", this) as ContentPresenter;
            _backContentPresenter = Template?.FindName("PART_BackContent", this) as ContentPresenter;
        }

        /// <summary>
        /// Flips the panel to show the opposite side.
        /// </summary>
        public void Flip()
        {
            if (_isAnimating || _frontContentPresenter == null || _backContentPresenter == null)
            {
                return;
            }

            _isAnimating = true;

            var currentPresenter = IsFlipped ? _backContentPresenter : _frontContentPresenter;
            var targetPresenter = IsFlipped ? _frontContentPresenter : _backContentPresenter;

            // Ensure transforms exist and are not frozen
            if (currentPresenter.RenderTransform is not ScaleTransform || ((Freezable)currentPresenter.RenderTransform).IsFrozen)
            {
                currentPresenter.RenderTransform = new ScaleTransform(1, 1);
            }

            if (targetPresenter.RenderTransform is not ScaleTransform || ((Freezable)targetPresenter.RenderTransform).IsFrozen)
            {
                targetPresenter.RenderTransform = new ScaleTransform(1, 1);
            }

            var currentTransform = (ScaleTransform)currentPresenter.RenderTransform;
            var targetTransform = (ScaleTransform)targetPresenter.RenderTransform;

            // Create a more realistic flip animation using a continuous scale transformation
            var storyboard = new Storyboard();
            
            // Animation for current presenter: scale from 1 to -1 (through 0)
            var currentAnimation = new DoubleAnimation
            {
                From = 1,
                To = -1,
                Duration = new Duration(FlipDuration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            // Animation for target presenter: scale from -1 to 1 (delayed to start at midpoint)
            var targetAnimation = new DoubleAnimation
            {
                From = -1,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(FlipDuration.TotalMilliseconds / 2)),
                BeginTime = TimeSpan.FromMilliseconds(FlipDuration.TotalMilliseconds / 2),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Set up the property paths and targets
            var scaleProperty = Direction == FlipDirection.Horizontal ? "RenderTransform.ScaleX" : "RenderTransform.ScaleY";
            
            Storyboard.SetTarget(currentAnimation, currentPresenter);
            Storyboard.SetTargetProperty(currentAnimation, new PropertyPath(scaleProperty));
            
            Storyboard.SetTarget(targetAnimation, targetPresenter);
            Storyboard.SetTargetProperty(targetAnimation, new PropertyPath(scaleProperty));

            storyboard.Children.Add(currentAnimation);
            storyboard.Children.Add(targetAnimation);

            // Set initial state - target is hidden and flipped
            if (Direction == FlipDirection.Horizontal)
            {
                targetTransform.ScaleX = -1;
            }
            else
            {
                targetTransform.ScaleY = -1;
            }

            targetPresenter.Visibility = Visibility.Hidden;

            // At midpoint (when current side reaches scale 0), switch visibility
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(FlipDuration.TotalMilliseconds / 2)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                currentPresenter.Visibility = Visibility.Hidden;
                targetPresenter.Visibility = Visibility.Visible;
            };
            timer.Start();

            // Handle animation completion
            storyboard.Completed += (s, e) =>
            {
                // Create a new transform to reset the current presenter for next flip
                // This avoids the issue of trying to modify a frozen transform
                currentPresenter.RenderTransform = new ScaleTransform(1, 1);

                IsFlipped = !IsFlipped;
                _isAnimating = false;
            };

            storyboard.Begin();
        }
    }
}