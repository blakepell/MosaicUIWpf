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
    /// Edits a copy of a BBS connection profile.
    /// </summary>
    public partial class BbsEditorWindow : Window
    {
        /// <summary>
        /// Initializes the connection editor.
        /// </summary>
        public BbsEditorWindow(BbsProfile? profile = null)
        {
            InitializeComponent();
            Profile = new BbsProfile
            {
                Id = profile?.Id ?? Guid.NewGuid(),
                Name = profile?.Name ?? "New BBS",
                Host = profile?.Host ?? string.Empty,
                Port = profile?.Port ?? 23,
                SshPort = profile?.SshPort ?? 0,
                SshKeyFile = profile?.SshKeyFile ?? string.Empty,
                Description = profile?.Description ?? string.Empty,
                AutoReconnect = profile?.AutoReconnect ?? true,
                LocalEcho = profile?.LocalEcho ?? false,
                BackspaceSendsDelete = profile?.BackspaceSendsDelete ?? false,
                TerminalEncoding = profile?.TerminalEncoding ?? BbsEncoding.Cp437,
                TerminalEmulation = profile?.TerminalEmulation ?? BbsTerminalEmulation.AnsiBbs,
                TerminalDisplayMode = profile?.TerminalDisplayMode ?? BbsTerminalDisplayMode.Classic80X25,
                UseCp437Font = profile?.UseCp437Font ?? false,
                TerminalType = profile?.TerminalType ?? string.Empty,
                DoorwayMode = profile?.DoorwayMode ?? false,
                AutoLogin = profile?.AutoLogin ?? false,
                LoginMacro = profile?.LoginMacro ?? "{USERNAME}{ENTER}{PASSWORD}{ENTER}"
            };
            EncodingComboBox.ItemsSource = Enum.GetValues<BbsEncoding>();
            EmulationComboBox.ItemsSource = Enum.GetValues<BbsTerminalEmulation>();
            DisplayModeComboBox.ItemsSource = Enum.GetValues<BbsTerminalDisplayMode>();
            DataContext = Profile;
            Loaded += (_, _) => NameTextBox.SelectAll();
        }

        /// <summary>
        /// Gets the validated profile result.
        /// </summary>
        public BbsProfile Profile { get; }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Profile.Name) || string.IsNullOrWhiteSpace(Profile.Host))
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    "A display name and host name are required.",
                    "BBS Navigator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Zero means the BBS does not offer that transport, so it is the only value
            // permitted outside the port range on either port.
            if (Profile.Port is < 0 or > 65535)
            {
                ShowWarning("The Telnet port must be between 1 and 65535, or 0 when the BBS does not offer Telnet.");
                return;
            }

            if (Profile.SshPort is < 0 or > 65535)
            {
                ShowWarning("The SSH port must be between 1 and 65535, or 0 when the BBS does not offer SSH.");
                return;
            }

            if (Profile.Port == 0 && Profile.SshPort == 0)
            {
                ShowWarning("Enter a Telnet port, an SSH port, or both. A profile with neither cannot connect.");
                return;
            }

            string keyFile = Profile.SshKeyFile.Trim();

            if (keyFile.Length > 0 && !File.Exists(keyFile))
            {
                ShowWarning("The SSH certificate could not be found. Choose an existing private key file or clear the field.");
                return;
            }

            Profile.Name = Profile.Name.Trim();
            Profile.Host = Profile.Host.Trim();
            Profile.SshKeyFile = keyFile;
            DialogResult = true;
        }

        private void BrowseCertificate_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select SSH Certificate",
                Filter = "Private key files (*.pem;*.key;id_*)|*.pem;*.key;id_*|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (!string.IsNullOrWhiteSpace(Profile.SshKeyFile))
            {
                string? directory = Path.GetDirectoryName(Profile.SshKeyFile);

                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    dialog.InitialDirectory = directory;
                }
            }

            if (dialog.ShowDialog(this) == true)
            {
                Profile.SshKeyFile = dialog.FileName;
            }
        }

        private void ClearCertificate_OnClick(object sender, RoutedEventArgs e)
        {
            Profile.SshKeyFile = string.Empty;
        }

        private static void ShowWarning(string message)
        {
            Mosaic.UI.Wpf.Controls.MessageBox.Show(
                message,
                "BBS Navigator",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
