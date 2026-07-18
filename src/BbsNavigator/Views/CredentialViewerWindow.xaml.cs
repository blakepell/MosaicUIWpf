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
    /// Displays a BBS credential record after the application-wide passphrase is unlocked.
    /// </summary>
    public partial class CredentialViewerWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CredentialViewerWindow"/> class.
        /// </summary>
        /// <param name="profile">The BBS profile whose credentials are viewed.</param>
        /// <param name="credentials">The decrypted credentials to display.</param>
        internal CredentialViewerWindow(BbsProfile profile, BbsCredentials credentials)
        {
            InitializeComponent();
            HeadingTextBlock.Text = $"Credentials for {profile.Name}";
            UserNameTextBox.Text = credentials.UserName;
            PasswordTextBox.Text = credentials.Password;
            Loaded += (_, _) => UserNameTextBox.Focus();
        }

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            UserNameTextBox.Clear();
            PasswordTextBox.Clear();
            base.OnClosed(e);
        }
    }
}
