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

            if (Profile.Port is < 1 or > 65535)
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    "The port must be between 1 and 65535.",
                    "BBS Navigator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Zero means the BBS does not offer SSH, so it is the only value permitted outside the port range.
            if (Profile.SshPort is < 0 or > 65535)
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    "The SSH port must be between 1 and 65535, or 0 when the BBS does not offer SSH.",
                    "BBS Navigator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Profile.Name = Profile.Name.Trim();
            Profile.Host = Profile.Host.Trim();
            DialogResult = true;
        }
    }
}
