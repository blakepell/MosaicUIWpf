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
using MessageBox = Mosaic.UI.Wpf.Controls.MessageBox;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// A behavior that prompts the user with a confirmation message when attempting to close a window.
    /// </summary>
    /// <example>
    /// The following example demonstrates how to use the WindowCloseConfirmation behavior in XAML:
    /// <![CDATA[
    /// <Window x:Class="YourNamespace.YourWindow"
    ///         xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    ///         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    ///         xmlns:i="http://schemas.microsoft.com/expression/2010/interactivity"
    ///         xmlns:b="clr-namespace:ApexGate.UI.Behaviors;assembly=YourAssemblyName">
    ///     <i:Interaction.Behaviors>
    ///         <b:WindowCloseConfirmationBehavior Message="Are you sure you want to exit?" />
    ///     </i:Interaction.Behaviors>
    /// </Window>
    /// ]]>
    /// </example>
    public sealed class WindowCloseConfirmationBehavior : Behavior<Window>
    {
        /// <summary>
        /// Identifies the Message dependency property.
        /// </summary>
        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
            nameof(Message), typeof(string), typeof(WindowCloseConfirmationBehavior), new PropertyMetadata("Are you sure you want to close this window?"));

        /// <summary>
        /// Gets or sets the confirmation message displayed to the user.
        /// </summary>
        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        /// <summary>
        /// Gets or sets if the confirmation is currently enabled.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
            nameof(IsEnabled), typeof(bool), typeof(WindowCloseConfirmationBehavior), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets if the confirmation is currently enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => (bool)GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        /// <summary>
        /// Attaches the behavior to the window and subscribes to the Closing event.
        /// </summary>
        protected override void OnAttached()
        {
            AssociatedObject.Closing += OnClosing;
            base.OnAttached();
        }

        /// <summary>
        /// Detaches the behavior from the window and unsubscribes from the Closing event.
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.Closing -= OnClosing;
            base.OnDetaching();
        }

        /// <summary>
        /// Handles the Closing event of the window and displays a confirmation message.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnClosing(object? sender, CancelEventArgs e)
        {
            if (!IsEnabled)
            {
                e.Cancel = false;
                return;
            }

            var result = MessageBox.Show(this.Message, "Confirm", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            e.Cancel = result != MessageBoxResult.Yes;
        }
    }
}
