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
using System.Windows.Controls.Primitives;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Behavior to open a new window when attached to a ButtonBase or MenuItem.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    ///     <b:Interaction.Behaviors>
    ///         <i:OpenWindowBehavior IsDialog = "True" WindowType="{x:Type dialog:AboutDialog}" />
    ///     </b:Interaction.Behaviors>
    /// ]]>
    /// </code>
    /// </example>
    public class OpenWindowBehavior : Behavior<FrameworkElement>
    {
        /// <summary>
        /// Gets or sets the type of the window to open.
        /// </summary>
        public Type WindowType
        {
            get => (Type)GetValue(WindowTypeProperty);
            set => SetValue(WindowTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets the type of the window to open.
        /// </summary>
        public static readonly DependencyProperty WindowTypeProperty =
            DependencyProperty.Register(nameof(WindowType), typeof(Type), typeof(OpenWindowBehavior), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets a value indicating whether the window should be opened as a dialog.
        /// </summary>
        /// <value>
        /// <c>true</c> if the window should be opened as a dialog; otherwise, <c>false</c>.
        /// </value>
        public bool IsDialog
        {
            get => (bool)GetValue(IsDialogProperty);
            set => SetValue(IsDialogProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the window should be opened as a dialog.
        /// </summary>
        /// <value>
        /// <c>true</c> if the window should be opened as a dialog; otherwise, <c>false</c>.
        /// </value>
        public static readonly DependencyProperty IsDialogProperty =
            DependencyProperty.Register(nameof(IsDialog), typeof(bool), typeof(OpenWindowBehavior), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets the startup location of the window.
        /// </summary>
        public static readonly DependencyProperty WindowStartupLocationProperty =
            DependencyProperty.Register(nameof(WindowStartupLocation), typeof(WindowStartupLocation), typeof(OpenWindowBehavior), new PropertyMetadata(System.Windows.WindowStartupLocation.CenterOwner));

        /// <summary>
        /// Gets or sets the startup location of the window.
        /// </summary>
        public WindowStartupLocation WindowStartupLocation
        {
            get => (WindowStartupLocation)GetValue(WindowStartupLocationProperty);
            set => SetValue(WindowStartupLocationProperty, value);
        }

        /// <summary>
        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the behavior is attached to an object that is not a ButtonBase or MenuItem.
        /// </exception>
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject is ButtonBase buttonBase)
            {
                buttonBase.Click += OpenWindow;
            }
            else if (AssociatedObject is MenuItem menuItem)
            {
                menuItem.Click += OpenWindow;
            }
            else
            {
                throw new InvalidOperationException("OpenWindowBehavior can only be attached to ButtonBase or MenuItem.");
            }
        }

        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (AssociatedObject is ButtonBase buttonBase)
            {
                buttonBase.Click -= OpenWindow;
            }
            else if (AssociatedObject is MenuItem menuItem)
            {
                menuItem.Click -= OpenWindow;
            }
        }

        /// <summary>
        /// <summary>
        /// Opens the window specified by the <see cref="WindowType"/> property.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="WindowType"/> is not set to a type that derives from <see cref="Window"/>.
        /// </exception>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void OpenWindow(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WindowType == null || !typeof(Window).IsAssignableFrom(WindowType))
                {
                    throw new InvalidOperationException("WindowType must be set to a type that derives from Window.");
                }

                var window = Activator.CreateInstance(WindowType) as Window;

                // No window was found of the type requested.
                if (window == null)
                {
                    return;
                }

                window.WindowStartupLocation = this.WindowStartupLocation;

                // Get the parent window when the behavior is attached
                var parentWindow = Window.GetWindow(AssociatedObject);

                // Optionally set the owner to the ParentWindow if it exists
                if (parentWindow != null && IsDialog)
                {
                    window.Owner = parentWindow;
                }

                if (IsDialog)
                {
                    window.ShowDialog();
                }
                else
                {
                    window.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}