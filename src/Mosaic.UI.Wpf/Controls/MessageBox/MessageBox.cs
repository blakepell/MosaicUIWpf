/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A themed, drop-in replacement for <see cref="System.Windows.MessageBox"/>.  It mirrors the
    /// full set of <c>Show</c> overloads and reuses the standard WPF dialog enums
    /// (<see cref="MessageBoxButton"/>, <see cref="MessageBoxImage"/>, <see cref="MessageBoxResult"/>
    /// and <see cref="MessageBoxOptions"/>) so that existing code can switch to it with a single
    /// <c>using</c> alias:
    /// <code>
    /// using MessageBox = Mosaic.UI.Wpf.Controls.MessageBox;
    /// </code>
    /// The dialog honors the active Mosaic light/dark/high-contrast theme.
    /// </summary>
    public static class MessageBox
    {
        /// <summary>
        /// Displays a message box with the specified text.
        /// </summary>
        /// <param name="messageBoxText">The text to display.</param>
        public static MessageBoxResult Show(string messageBoxText)
        {
            return ShowCore(null, messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        /// <summary>
        /// Displays a message box with the specified text and caption.
        /// </summary>
        /// <param name="messageBoxText">The text to display.</param>
        /// <param name="caption">The title bar caption.</param>
        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            return ShowCore(null, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        /// <summary>
        /// Displays a message box with the specified text, caption, and buttons.
        /// </summary>
        /// <param name="messageBoxText">The text to display.</param>
        /// <param name="caption">The title bar caption.</param>
        /// <param name="button">The buttons to display.</param>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return ShowCore(null, messageBoxText, caption, button, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        /// <summary>
        /// Displays a message box with the specified text, caption, buttons, and icon.
        /// </summary>
        /// <param name="messageBoxText">The text to display.</param>
        /// <param name="caption">The title bar caption.</param>
        /// <param name="button">The buttons to display.</param>
        /// <param name="icon">The icon to display.</param>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return ShowCore(null, messageBoxText, caption, button, icon, MessageBoxResult.None, MessageBoxOptions.None);
        }

        /// <summary>
        /// Displays a message box with the specified text, caption, buttons, icon, and default result.
        /// </summary>
        /// <param name="messageBoxText">The text to display.</param>
        /// <param name="caption">The title bar caption.</param>
        /// <param name="button">The buttons to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="defaultResult">The default (focused) button.</param>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return ShowCore(null, messageBoxText, caption, button, icon, defaultResult, MessageBoxOptions.None);
        }

        /// <summary>
        /// Displays a message box with the specified text, caption, buttons, icon, default result, and options.
        /// </summary>
        /// <param name="messageBoxText">The text to display.</param>
        /// <param name="caption">The title bar caption.</param>
        /// <param name="button">The buttons to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="defaultResult">The default (focused) button.</param>
        /// <param name="options">Display and reading-order options.</param>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            return ShowCore(null, messageBoxText, caption, button, icon, defaultResult, options);
        }

        /// <summary>
        /// Displays a message box owned by the specified window.
        /// </summary>
        /// <param name="owner">The window that owns the message box.</param>
        /// <param name="messageBoxText">The text to display.</param>
        public static MessageBoxResult Show(Window owner, string messageBoxText)
        {
            return ShowCore(owner, messageBoxText, string.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        /// <summary>
        /// Displays a message box owned by the specified window with the specified text and caption.
        /// </summary>
        /// <param name="owner">The window that owns the message box.</param>
        /// <param name="messageBoxText">The text to display.</param>
        /// <param name="caption">The title bar caption.</param>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption)
        {
            return ShowCore(owner, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        /// <summary>
        /// Displays a message box owned by the specified window with the specified text, caption, and buttons.
        /// </summary>
        /// <param name="owner">The window that owns the message box.</param>
        /// <param name="messageBoxText">The text to display.</param>
        /// <param name="caption">The title bar caption.</param>
        /// <param name="button">The buttons to display.</param>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button)
        {
            return ShowCore(owner, messageBoxText, caption, button, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
        }

        /// <summary>
        /// Displays a message box owned by the specified window with the specified text, caption, buttons, and icon.
        /// </summary>
        /// <param name="owner">The window that owns the message box.</param>
        /// <param name="messageBoxText">The text to display.</param>
        /// <param name="caption">The title bar caption.</param>
        /// <param name="button">The buttons to display.</param>
        /// <param name="icon">The icon to display.</param>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return ShowCore(owner, messageBoxText, caption, button, icon, MessageBoxResult.None, MessageBoxOptions.None);
        }

        /// <summary>
        /// Displays a message box owned by the specified window with the specified text, caption, buttons, icon, and default result.
        /// </summary>
        /// <param name="owner">The window that owns the message box.</param>
        /// <param name="messageBoxText">The text to display.</param>
        /// <param name="caption">The title bar caption.</param>
        /// <param name="button">The buttons to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="defaultResult">The default (focused) button.</param>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return ShowCore(owner, messageBoxText, caption, button, icon, defaultResult, MessageBoxOptions.None);
        }

        /// <summary>
        /// Displays a message box owned by the specified window with the specified text, caption, buttons, icon, default result, and options.
        /// </summary>
        /// <param name="owner">The window that owns the message box.</param>
        /// <param name="messageBoxText">The text to display.</param>
        /// <param name="caption">The title bar caption.</param>
        /// <param name="button">The buttons to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="defaultResult">The default (focused) button.</param>
        /// <param name="options">Display and reading-order options.</param>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            return ShowCore(owner, messageBoxText, caption, button, icon, defaultResult, options);
        }

        /// <summary>
        /// Creates, positions, and displays the dialog, returning the user's choice.
        /// </summary>
        private static MessageBoxResult ShowCore(Window? owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            var window = new MessageBoxWindow(messageBoxText, caption, button, icon, defaultResult, options);

            owner ??= ResolveOwner();

            if (owner is not null && !ReferenceEquals(owner, window))
            {
                window.Owner = owner;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            window.ShowDialog();

            return window.Result;
        }

        /// <summary>
        /// Attempts to locate a sensible owner window (the active window, otherwise the application's
        /// main window) so the dialog is centered like the native message box.
        /// </summary>
        private static Window? ResolveOwner()
        {
            var app = Application.Current;

            if (app is null)
            {
                return null;
            }

            foreach (Window window in app.Windows)
            {
                if (window.IsActive)
                {
                    return window;
                }
            }

            return app.MainWindow;
        }
    }
}
