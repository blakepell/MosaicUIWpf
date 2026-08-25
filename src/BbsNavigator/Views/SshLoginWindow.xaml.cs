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
using Microsoft.Win32;
using System.IO;
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
            KeyFile = profile.SshKeyFile;
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
        /// Gets or sets the private key file used for public key authentication.
        /// </summary>
        /// <value>The full path of the private key, or an empty string to authenticate with a password.</value>
        public string KeyFile { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the passphrase that decrypts <see cref="KeyFile"/>.
        /// </summary>
        /// <value>The private key passphrase, or an empty string when the key is not encrypted.</value>
        public string KeyPassphrase { get; set; } = string.Empty;

        /// <summary>
        /// Gets the collected login once the dialog is accepted.
        /// </summary>
        public BbsCredentials? Credentials { get; private set; }

        private void BrowseCertificate_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select SSH Certificate",
                Filter = "Private key files (*.pem;*.key;id_*)|*.pem;*.key;id_*|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (!string.IsNullOrWhiteSpace(KeyFile))
            {
                string? directory = Path.GetDirectoryName(KeyFile);

                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    dialog.InitialDirectory = directory;
                }
            }

            if (dialog.ShowDialog(this) == true)
            {
                // The text box is bound two-way, so writing it here also updates KeyFile.
                CertificateTextBox.Text = dialog.FileName;
            }
        }

        private void ClearCertificate_OnClick(object sender, RoutedEventArgs e)
        {
            CertificateTextBox.Text = string.Empty;
            KeyPassphraseBox.Password = string.Empty;
        }

        private void Connect_OnClick(object sender, RoutedEventArgs e)
        {
            string userName = UserName.Trim();
            string keyFile = KeyFile.Trim();

            if (string.IsNullOrWhiteSpace(userName))
            {
                ShowWarning("A username is required to connect over SSH.");
                return;
            }

            // Public key authentication replaces the password, so one of the two must be present.
            if (string.IsNullOrEmpty(Password) && string.IsNullOrWhiteSpace(keyFile))
            {
                ShowWarning("A password or a certificate is required to connect over SSH.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(keyFile) && !File.Exists(keyFile))
            {
                ShowWarning("The selected certificate could not be found.");
                return;
            }

            KeyFile = keyFile;
            Credentials = new BbsCredentials(userName, Password)
            {
                KeyFile = string.IsNullOrWhiteSpace(keyFile) ? null : keyFile,
                KeyPassphrase = string.IsNullOrEmpty(KeyPassphrase) ? null : KeyPassphrase
            };
            DialogResult = true;
        }

        private static void ShowWarning(string message)
        {
            Mosaic.UI.Wpf.Controls.MessageBox.Show(
                message,
                "BBS Navigator",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            SshPasswordBox.Password = string.Empty;
            KeyPassphraseBox.Password = string.Empty;
            Password = string.Empty;
            KeyPassphrase = string.Empty;
            base.OnClosed(e);
        }
    }
}
