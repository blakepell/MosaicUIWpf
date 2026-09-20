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
                LocalEcho = profile?.LocalEcho,
                BackspaceSendsDelete = profile?.BackspaceSendsDelete ?? false,
                TerminalEncoding = profile?.TerminalEncoding ?? BbsEncoding.Cp437,
                TerminalEmulation = profile?.TerminalEmulation ?? BbsTerminalEmulation.AnsiBbs,
                TerminalDisplayMode = profile?.TerminalDisplayMode ?? BbsTerminalDisplayMode.Classic80X25,
                UseCp437Font = profile?.UseCp437Font ?? false,
                TerminalType = profile?.TerminalType ?? string.Empty,
                DoorwayMode = profile?.DoorwayMode ?? false,
                NumericKeypadNavigation = profile?.NumericKeypadNavigation ?? false,
                CaptureSession = profile?.CaptureSession ?? false,
                AutoLogin = profile?.AutoLogin ?? false,
                LoginMacro = profile?.LoginMacro ?? "{USERNAME}{ENTER}{PASSWORD}{ENTER}",
                UseLoginSequence = profile?.UseLoginSequence ?? false,
                LoginSteps = profile?.LoginSteps.Select(s => new LoginStep { Action = s.Action, Text = s.Text, Seconds = s.Seconds }).ToList() ?? new(),
                PasteCharacterDelayOverride = profile?.PasteCharacterDelayOverride ?? -1,
                PasteLineDelayMilliseconds = profile?.PasteLineDelayMilliseconds ?? 100
            };
            LocalEchoComboBox.ItemsSource = Enum.GetValues<BbsLocalEchoMode>();
            EncodingComboBox.ItemsSource = Enum.GetValues<BbsEncoding>();
            EmulationComboBox.ItemsSource = Enum.GetValues<BbsTerminalEmulation>();
            DisplayModeComboBox.ItemsSource = Enum.GetValues<BbsTerminalDisplayMode>();
            DataContext = Profile;
            TelnetPortTextBox.Text = Profile.Port == 0 ? string.Empty : Profile.Port.ToString();
            SshPortTextBox.Text = Profile.SshPort == 0 ? string.Empty : Profile.SshPort.ToString();
            Loaded += (_, _) => NameTextBox.SelectAll();
        }

        /// <summary>
        /// Gets the validated profile result.
        /// </summary>
        public BbsProfile Profile { get; }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (Profile.UseLoginSequence)
            {
                try { Common.LoginSequenceRunner.Validate(Profile.LoginSteps); }
                catch (ArgumentException ex) { ShowWarning(ex.Message); return; }
            }
            if (string.IsNullOrWhiteSpace(Profile.Name) || string.IsNullOrWhiteSpace(Profile.Host))
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    "A display name and host name are required.",
                    "BBS Navigator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Blank fields and zero both mean that the BBS does not offer that transport.
            string telnetText = TelnetPortTextBox.Text.Trim();
            string sshText = SshPortTextBox.Text.Trim();
            if (!int.TryParse(telnetText.Length == 0 ? "0" : telnetText, out int telnetPort)
                || telnetPort is < 0 or > 65535)
            {
                ShowWarning("The Telnet port must be between 1 and 65535. Leave blank or enter 0 when the BBS does not offer Telnet.");
                return;
            }

            if (!int.TryParse(sshText.Length == 0 ? "0" : sshText, out int sshPort)
                || sshPort is < 0 or > 65535)
            {
                ShowWarning("The SSH port must be between 1 and 65535. Leave blank or enter 0 when the BBS does not offer SSH.");
                return;
            }

            if (telnetPort == 0 && sshPort == 0)
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
            Profile.Port = telnetPort;
            Profile.SshPort = sshPort;
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

        private void EditLoginSteps_OnClick(object sender, RoutedEventArgs e)
        {
            var editor = new LoginSequenceWindow(Profile.LoginSteps) { Owner = this };
            if (editor.ShowDialog() == true)
            {
                Profile.LoginSteps = editor.Steps.ToList();
                Profile.UseLoginSequence = true;
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
