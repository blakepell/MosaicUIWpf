/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls;
using System.Windows;
using System.Windows.Controls;

namespace BbsNavigator.Views
{
    /// <summary>
    /// Displays the embedded Markdown user guide with back/contents navigation and support
    /// for <c>app:</c> links that open pages within the application.
    /// </summary>
    public partial class UserGuideView : UserControl
    {
        /// <summary>
        /// The pack path of the guide's table of contents.
        /// </summary>
        private static readonly Uri HomeUri = new("/BbsNavigator;component/Docs/index.md", UriKind.Relative);

        /// <summary>
        /// Initializes the user guide view.
        /// </summary>
        public UserGuideView()
        {
            InitializeComponent();
        }

        private void Back_OnClick(object sender, RoutedEventArgs e)
        {
            Viewer.GoBack();
        }

        private void Home_OnClick(object sender, RoutedEventArgs e)
        {
            Viewer.Source = HomeUri;
        }

        /// <summary>
        /// Routes <c>app:</c> links in the documentation to the corresponding application windows.
        /// All other links keep the viewer's default behavior (in-guide navigation for Markdown
        /// resources, the default browser for web links).
        /// </summary>
        private void Viewer_OnLinkClicked(object sender, MarkdownLinkClickedEventArgs e)
        {
            if (!e.Uri.IsAbsoluteUri || !string.Equals(e.Uri.Scheme, "app", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            e.Handled = true;

            // The guide can be floated into its own window, so resolve the application's main
            // window rather than the window hosting this control.
            if (Application.Current.MainWindow is not MainWindow mainWindow)
            {
                return;
            }

            switch (e.Uri.AbsolutePath.Trim('/').ToLowerInvariant())
            {
                case "options":
                    mainWindow.ShowOptions();
                    break;
                case "add-bbs":
                    mainWindow.ShowAddBbs();
                    break;
            }
        }
    }
}
