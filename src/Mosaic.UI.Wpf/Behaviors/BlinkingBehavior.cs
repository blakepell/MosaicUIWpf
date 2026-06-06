/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;
using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// A behavior that makes any <see cref="FrameworkElement"/> blink.
    /// </summary>
    /// <example>
    /// Example usage in XAML:
    /// <code>
    /// <![CDATA[
    ///     <Canvas
    ///         Width="10"
    ///         Height="20"
    ///         Background="White">
    ///         <b:Interaction.Behaviors>
    ///             <behaviors:BlinkingBehavior BlinkDuration = "0.5" IsBlinking="True" />
    ///         </b:Interaction.Behaviors>
    ///      </Canvas>
    /// ]]>
    /// </code>
    /// </example>    
    public class BlinkingBehavior : Behavior<FrameworkElement>
    {
        private Storyboard? _blinkStoryboard;

        /// <summary>
        /// The duration of the blink.  Note that because the blink auto reverses the duration for a
        /// full cycle is double the value set here.
        /// </summary>
        public static readonly DependencyProperty BlinkDurationProperty =
            DependencyProperty.Register(nameof(BlinkDuration), typeof(double), typeof(BlinkingBehavior), new PropertyMetadata(0.5));

        /// <summary>
        /// The duration of the blink.  Note that because the blink auto reverses the duration for a
        /// full cycle is double the value set here.
        /// </summary>
        public double BlinkDuration
        {
            get => (double)GetValue(BlinkDurationProperty);
            set => SetValue(BlinkDurationProperty, value);
        }

        /// <summary>
        /// If the <see cref="FrameworkElement"/> is currently blinking.
        /// </summary>
        public static readonly DependencyProperty IsBlinkingProperty =
            DependencyProperty.Register(nameof(IsBlinking), typeof(bool), typeof(BlinkingBehavior), new PropertyMetadata(false, OnIsBlinkingChanged));

        /// <summary>
        /// If the <see cref="FrameworkElement"/> is currently blinking.
        /// </summary>
        public bool IsBlinking
        {
            get => (bool)GetValue(IsBlinkingProperty);
            set => SetValue(IsBlinkingProperty, value);
        }

        /// <summary>
        /// Fired when the <see cref="IsBlinkingProperty"/> changes.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnIsBlinkingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (BlinkingBehavior)d;

            if ((bool)e.NewValue)
            {
                behavior.StartBlinking();
            }
            else
            {
                behavior.StopBlinking();
            }
        }

        /// <summary>
        /// When this behavior is attached to a <see cref="FrameworkElement"/>.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            CreateBlinkingAnimation();

            // Handle control unloading (when it goes out of scope)
            AssociatedObject.Unloaded += AssociatedObjectOnUnloaded;
            AssociatedObject.Loaded += AssociatedObjectOnLoaded;

            // Start blinking if IsBlinking is true when attached
            if (IsBlinking)
            {
                StartBlinking();
            }
        }

        /// <summary>
        /// When a behavior is detaching from a <see cref="FrameworkElement"/>
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            StopBlinking();
        }

        /// <summary>
        /// When the associated <see cref="FrameworkElement"/> OnLoaded event fires.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs e)
        {
            StartBlinking();
        }

        /// <summary>
        /// When the associated <see cref="FrameworkElement"/> OnUnloaded event fires.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssociatedObjectOnUnloaded(object sender, RoutedEventArgs e)
        {
            StopBlinking();
        }

        /// <summary>
        /// Creates the blinking <see cref="DoubleAnimation"/> and <see cref="Storyboard"/>.
        /// </summary>
        private void CreateBlinkingAnimation()
        {
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(BlinkDuration),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            _blinkStoryboard = new Storyboard();
            _blinkStoryboard.Children.Add(animation);
            Storyboard.SetTarget(animation, AssociatedObject);
            Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
        }

        /// <summary>
        /// Starts the blinking animation.
        /// </summary>
        private void StartBlinking()
        {
            if (_blinkStoryboard != null && AssociatedObject != null)
            {
                _blinkStoryboard.Begin();
            }
        }

        /// <summary>
        /// Stops the blinking animation.
        /// </summary>
        private void StopBlinking()
        {
            if (_blinkStoryboard != null && AssociatedObject != null)
            {
                _blinkStoryboard.Stop();
                AssociatedObject.Opacity = 1.0; // Reset opacity to fully visible
            }
        }
    }
}
