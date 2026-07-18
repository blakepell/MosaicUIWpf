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
    /// Encrypts, replaces, or removes the credentials associated with one BBS profile.
    /// </summary>
    public partial class CredentialEditorWindow : Window
    {
        private readonly BbsProfile _profile;
        private string _encryptionPassphrase;

        /// <summary>
        /// Initializes a new instance of the <see cref="CredentialEditorWindow"/> class.
        /// </summary>
        /// <param name="profile">The BBS profile whose credentials are edited.</param>
        /// <param name="encryptionPassphrase">The unlocked application-wide encryption passphrase.</param>
        public CredentialEditorWindow(BbsProfile profile, string encryptionPassphrase)
        {
            InitializeComponent();
            _profile = profile;
            _encryptionPassphrase = encryptionPassphrase;
            HeadingTextBlock.Text = $"Credentials for {profile.Name}";
            ExistingCredentialsTextBlock.Visibility = profile.HasCredentials ? Visibility.Visible : Visibility.Collapsed;
            RemoveButton.IsEnabled = profile.HasCredentials;
            Loaded += (_, _) => UserNameTextBox.Focus();
        }

        private async void Save_OnClick(object sender, RoutedEventArgs e)
        {
            string userName = UserNameTextBox.Text.Trim();
            string password = BbsPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
            {
                ShowWarning("A username and BBS password are required.");
                return;
            }

            try
            {
                var credentials = new BbsCredentials(userName, password);
                string encryptionPassphrase = _encryptionPassphrase;
                _profile.EncryptedCredentials = await Task.Run(
                    () => CredentialProtector.Protect(credentials, encryptionPassphrase));
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowWarning($"The credentials could not be encrypted.\n\n{ex.Message}");
            }
        }

        private void Remove_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = Mosaic.UI.Wpf.Controls.MessageBox.Show(
                $"Remove the saved credentials for ‘{_profile.Name}’?",
                "BBS Navigator",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _profile.EncryptedCredentials = null;
                DialogResult = true;
            }
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
            BbsPasswordBox.Password = string.Empty;
            _encryptionPassphrase = string.Empty;
            base.OnClosed(e);
        }
    }
}
