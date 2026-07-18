/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Common;
using BbsNavigator.Models;
using System.Windows;

namespace BbsNavigator.Views
{
    /// <summary>
    /// Reveals an encrypted BBS credential record after the user supplies its passphrase.
    /// </summary>
    public partial class CredentialViewerWindow : Window
    {
        private readonly BbsProfile _profile;

        /// <summary>
        /// Initializes a new instance of the <see cref="CredentialViewerWindow"/> class.
        /// </summary>
        /// <param name="profile">The BBS profile whose credentials are viewed.</param>
        public CredentialViewerWindow(BbsProfile profile)
        {
            InitializeComponent();
            _profile = profile;
            HeadingTextBlock.Text = $"Credentials for {profile.Name}";
            Loaded += (_, _) => PassphraseBox.Focus();
        }

        private void Reveal_OnClick(object sender, RoutedEventArgs e)
        {
            string passphrase = PassphraseBox.Password;

            if (string.IsNullOrEmpty(passphrase) ||
                string.IsNullOrWhiteSpace(_profile.EncryptedCredentials) ||
                !CredentialProtector.TryUnprotect(_profile.EncryptedCredentials, passphrase, out BbsCredentials? credentials) ||
                credentials == null)
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    "The credentials could not be decrypted. Check the passphrase and try again.",
                    "BBS Navigator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            UserNameTextBox.Text = credentials.UserName;
            PasswordTextBox.Text = credentials.Password;
            PassphraseBox.Password = string.Empty;
        }

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            PassphraseBox.Password = string.Empty;
            UserNameTextBox.Clear();
            PasswordTextBox.Clear();
            base.OnClosed(e);
        }
    }
}
