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
using System.Diagnostics;
using System.Windows.Controls.Primitives;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Behavior to open a folder in Windows Explorer when attached to a ButtonBase or MenuItem.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    ///     <b:Interaction.Behaviors>
    ///         <i:OpenFolderBehavior Folder = "C:\Temp" />
    ///     </b:Interaction.Behaviors>
    /// ]]>
    /// </code>
    /// </example>
    public class OpenFolderBehavior : Behavior<FrameworkElement>
    {
        /// <summary>
        /// Gets or sets the path of the folder to open.
        /// </summary>
        public string? Folder
        {
            get => (string?)GetValue(FolderProperty);
            set => SetValue(FolderProperty, value);
        }

        /// <summary>
        /// Gets or sets the path of the folder to open.
        /// </summary>
        public static readonly DependencyProperty FolderProperty =
            DependencyProperty.Register(nameof(Folder), typeof(string), typeof(OpenFolderBehavior), new PropertyMetadata(null));

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the behavior is attached to an object that is not a ButtonBase or MenuItem.
        /// </exception>
        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject is ButtonBase buttonBase)
            {
                buttonBase.Click += OpenFolder;
            }
            else if (AssociatedObject is MenuItem menuItem)
            {
                menuItem.Click += OpenFolder;
            }
            else
            {
                throw new InvalidOperationException("OpenFolderBehavior can only be attached to ButtonBase or MenuItem.");
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
                buttonBase.Click -= OpenFolder;
            }
            else if (AssociatedObject is MenuItem menuItem)
            {
                menuItem.Click -= OpenFolder;
            }
        }

        /// <summary>
        /// Opens the folder specified by the <see cref="Folder"/> property in Windows Explorer.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            string? folder = this.Folder;

            try
            {
                if (string.IsNullOrWhiteSpace(folder))
                {
                    MessageBox.Show("No folder was specified.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                folder = Environment.ExpandEnvironmentVariables(folder.Trim());

                if (!Directory.Exists(folder))
                {
                    MessageBox.Show($"The folder '{folder}' does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Use the fully qualified path so Explorer receives an unambiguous location.
                var psi = new ProcessStartInfo
                {
                    FileName = Path.GetFullPath(folder),
                    UseShellExecute = true,
                    Verb = "open"
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The folder '{folder}' could not be opened.{Environment.NewLine}{Environment.NewLine}{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
