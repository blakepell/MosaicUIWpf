/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Models;
using System.Windows;

namespace BbsNavigator.Views
{
    /// <summary>
    /// Collects a one-time SSH login for a BBS profile that has no saved credentials.
    /// </summary>
    public partial class SshLoginWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SshLoginWindow"/> class.
        /// </summary>
        /// <param name="profile">The BBS profile the login applies to.</param>
        public SshLoginWindow(BbsProfile profile)
        {
            InitializeComponent();
            DataContext = this;
            HeadingTextBlock.Text = $"Sign in to {profile.Name} ({profile.SshEndpoint})";
            Loaded += (_, _) => UserNameTextBox.Focus();
        }

        /// <summary>
        /// Gets or sets the SSH username.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SSH password.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets the collected login once the dialog is accepted.
        /// </summary>
        public BbsCredentials? Credentials { get; private set; }

        private void Connect_OnClick(object sender, RoutedEventArgs e)
        {
            string userName = UserName.Trim();

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(Password))
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    "A username and password are required to connect over SSH.",
                    "BBS Navigator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Credentials = new BbsCredentials(userName, Password);
            DialogResult = true;
        }

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            SshPasswordBox.Password = string.Empty;
            Password = string.Empty;
            base.OnClosed(e);
        }
    }
}
