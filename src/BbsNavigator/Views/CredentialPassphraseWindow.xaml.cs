/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;

namespace BbsNavigator.Views
{
    /// <summary>
    /// Configures or unlocks the application-wide credential encryption passphrase.
    /// </summary>
    public partial class CredentialPassphraseWindow : Window
    {
        private readonly bool _isConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="CredentialPassphraseWindow"/> class.
        /// </summary>
        /// <param name="isConfiguration">
        /// <see langword="true"/> to configure a new passphrase; otherwise, <see langword="false"/> to unlock it.
        /// </param>
        public CredentialPassphraseWindow(bool isConfiguration)
        {
            InitializeComponent();
            _isConfiguration = isConfiguration;
            HeadingTextBlock.Text = isConfiguration
                ? "Set credential encryption passphrase"
                : "Unlock credential encryption";
            ExplanationTextBlock.Text = isConfiguration
                ? "Set one passphrase for every BBS credential. You will not need to enter it again during this app session."
                : "Enter the app-wide passphrase once. It will be reused for credential viewing and editing until BBS Navigator closes.";
            AcceptButton.Content = isConfiguration ? "Set Passphrase" : "Unlock";
            ConfirmLabel.Visibility = isConfiguration ? Visibility.Visible : Visibility.Collapsed;
            ConfirmPassphraseBox.Visibility = isConfiguration ? Visibility.Visible : Visibility.Collapsed;
            Loaded += (_, _) => PassphraseBox.Focus();
        }

        /// <summary>
        /// Gets the passphrase entered by the user after the dialog is accepted.
        /// </summary>
        /// <value>The entered application-wide credential passphrase.</value>
        public string Passphrase { get; private set; } = string.Empty;

        private void Accept_OnClick(object sender, RoutedEventArgs e)
        {
            string passphrase = PassphraseBox.Password;

            if (passphrase.Length < 8)
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    "Use an encryption passphrase containing at least 8 characters.",
                    "BBS Navigator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (_isConfiguration &&
                !string.Equals(passphrase, ConfirmPassphraseBox.Password, StringComparison.Ordinal))
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    "The encryption passphrases do not match.",
                    "BBS Navigator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Passphrase = passphrase;
            DialogResult = true;
        }

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            PassphraseBox.Password = string.Empty;
            ConfirmPassphraseBox.Password = string.Empty;
            base.OnClosed(e);
        }
    }
}
