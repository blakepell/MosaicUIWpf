using System.Collections;

namespace Mosaic.UI.Wpf.Common
{
    /// <summary>
    /// Provides attached properties and helper logic for common element behaviors.
    /// </summary>
    public class ElementHelper : DependencyObject
    {
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached("CornerRadius", typeof(CornerRadius), typeof(ElementHelper),
                new PropertyMetadata(new CornerRadius(0)));

        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.RegisterAttached("Watermark", typeof(string), typeof(ElementHelper),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsStripeProperty =
            DependencyProperty.RegisterAttached("IsStripe", typeof(bool), typeof(ElementHelper),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsRoundProperty =
           DependencyProperty.RegisterAttached("IsRound", typeof(bool), typeof(ElementHelper),
               new PropertyMetadata(false));

        public static readonly DependencyProperty IsClearProperty =
          DependencyProperty.RegisterAttached("IsClear", typeof(bool), typeof(ElementHelper),
              new PropertyMetadata(false, OnIsClearChanged));

        public static readonly DependencyProperty IsAnimationProperty =
           DependencyProperty.RegisterAttached("IsAnimation", typeof(bool), typeof(ElementHelper),
               new PropertyMetadata(true));

        /// <summary>
        /// Gets the corner radius attached to an element.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <returns>The attached corner radius value.</returns>
        public static CornerRadius GetCornerRadius(DependencyObject obj)
        {
            return (CornerRadius)obj.GetValue(CornerRadiusProperty);
        }

        /// <summary>
        /// Sets the corner radius attached to an element.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <param name="value">The corner radius to assign.</param>
        public static void SetCornerRadius(DependencyObject obj, CornerRadius value)
        {
            obj.SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets the watermark attached to an element.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <returns>The attached watermark value, or <see langword="null" />.</returns>
        public static string? GetWatermark(DependencyObject obj)
        {
            return (string?)obj.GetValue(WatermarkProperty);
        }

        /// <summary>
        /// Sets the watermark attached to an element.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <param name="value">The watermark to assign, or <see langword="null" />.</param>
        public static void SetWatermark(DependencyObject obj, string? value)
        {
            obj.SetValue(WatermarkProperty, value);
        }

        /// <summary>
        /// Gets a value that indicates whether alternating stripe styling is enabled.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <returns><see langword="true" /> if stripe styling is enabled; otherwise, <see langword="false" />.</returns>
        public static bool GetIsStripe(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsStripeProperty);
        }

        /// <summary>
        /// Sets a value that indicates whether alternating stripe styling is enabled.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <param name="value"><see langword="true" /> to enable stripe styling; otherwise, <see langword="false" />.</param>
        public static void SetIsStripe(DependencyObject obj, bool value)
        {
            obj.SetValue(IsStripeProperty, value);
        }

        /// <summary>
        /// Gets a value that indicates whether rounded styling is enabled.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <returns><see langword="true" /> if rounded styling is enabled; otherwise, <see langword="false" />.</returns>
        public static bool GetIsRound(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsRoundProperty);
        }

        /// <summary>
        /// Sets a value that indicates whether rounded styling is enabled.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <param name="value"><see langword="true" /> to enable rounded styling; otherwise, <see langword="false" />.</param>
        public static void SetIsRound(DependencyObject obj, bool value)
        {
            obj.SetValue(IsRoundProperty, value);
        }

        /// <summary>
        /// Sets a value that indicates whether the clear action is enabled for a button.
        /// </summary>
        /// <param name="element">The button that stores the attached value.</param>
        /// <param name="value"><see langword="true" /> to enable the clear action; otherwise, <see langword="false" />.</param>
        public static void SetIsClear(UIElement element, bool value)
        {
            element.SetValue(IsClearProperty, value);
        }

        /// <summary>
        /// Gets a value that indicates whether the clear action is enabled for a button.
        /// </summary>
        /// <param name="element">The button that stores the attached value.</param>
        /// <returns><see langword="true" /> if the clear action is enabled; otherwise, <see langword="false" />.</returns>
        public static bool GetIsClear(UIElement element)
        {
            return (bool)element.GetValue(IsClearProperty);
        }

        /// <summary>
        /// Sets a value that indicates whether animation is enabled.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <param name="value"><see langword="true" /> to enable animation; otherwise, <see langword="false" />.</param>
        public static void SetIsAnimation(DependencyObject obj, bool value)
        {
            obj.SetValue(IsAnimationProperty, value);
        }

        /// <summary>
        /// Gets a value that indicates whether animation is enabled.
        /// </summary>
        /// <param name="obj">The element that stores the attached value.</param>
        /// <returns><see langword="true" /> if animation is enabled; otherwise, <see langword="false" />.</returns>
        public static bool GetIsAnimation(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsAnimationProperty);
        }

        private static void OnIsClearChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                if ((bool)e.NewValue)
                {
                    button.Click += OnButtonClear_Click;
                }
                else
                {
                    button.Click -= OnButtonClear_Click;
                }
            }
        }

        private static void OnButtonClear_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.TemplatedParent != null)
            {
                switch (button.TemplatedParent)
                {
                    case TextBox textBox:
                        textBox.Clear();
                        break;
                    case PasswordBox passwordBox:
                        passwordBox.Clear();
                        break;
                    case TabItem tabItem:
                        var tabControl = ControlsHelper.FindParent<TabControl>(tabItem);
                        if (tabControl != null)
                        {
                            if (tabControl.ItemsSource is IList itemsSource)
                            {
                                itemsSource.Remove(tabItem.DataContext);
                            }
                            else
                            {
                                tabControl.Items.Remove(tabItem);
                            }
                        }
                        break;
                }
            }
        }
    }
}