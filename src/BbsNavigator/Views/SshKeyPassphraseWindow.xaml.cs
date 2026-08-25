/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.IO;
using System.Windows;

namespace BbsNavigator.Views
{
    /// <summary>
    /// Collects the passphrase that decrypts a passphrase-protected SSH private key.
    /// </summary>
    /// <remarks>
    /// The passphrase is used for the session only; it is never written to the profile.
    /// </remarks>
    public partial class SshKeyPassphraseWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SshKeyPassphraseWindow"/> class.
        /// </summary>
        /// <param name="keyFile">The path of the encrypted private key file.</param>
        public SshKeyPassphraseWindow(string keyFile)
        {
            InitializeComponent();
            ExplanationTextBlock.Text =
                $"‘{Path.GetFileName(keyFile)}’ is protected by a passphrase. It is used for this session only and is not saved.";
            Loaded += (_, _) => PassphraseBox.Focus();
        }

        /// <summary>
        /// Gets the passphrase entered by the user after the dialog is accepted.
        /// </summary>
        /// <value>The private key passphrase.</value>
        public string Passphrase { get; private set; } = string.Empty;

        private void Accept_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PassphraseBox.Password))
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    "Enter the passphrase that unlocks the certificate.",
                    "BBS Navigator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Passphrase = PassphraseBox.Password;
            DialogResult = true;
        }

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            PassphraseBox.Password = string.Empty;
            base.OnClosed(e);
        }
    }
}
