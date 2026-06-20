/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class AudioPlayerExample
    {
        public AudioPlayerExample()
        {
            InitializeComponent();

            // Load the bundled Sample.mp3 that is copied next to the executable as Content. We build a
            // playlist (with the single sample) so the Previous / Next buttons have something to navigate.
            var samplePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Audio", "Sample.mp3");

            if (File.Exists(samplePath))
            {
                var sample = new Uri(samplePath);
                Player.Playlist.Add(sample);
                Player.CurrentIndex = 0;
            }
        }

        /// <summary>
        /// Prompts the user to pick a media file the <see cref="Mosaic.UI.Wpf.Controls.AudioPlayer"/> supports
        /// (handled by Windows Media Foundation) and loads it into the player.
        /// </summary>
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a Media File",
                CheckFileExists = true,
                Filter = "Audio Files|*.mp3;*.wav;*.wma;*.aac;*.m4a;*.flac;*.ogg|All Files|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var file = new Uri(dialog.FileName);

            // Reset to -1 first so re-selecting index 0 still raises the change that swaps the active track.
            Player.CurrentIndex = -1;
            Player.Playlist.Clear();
            Player.Playlist.Add(file);
            Player.CurrentIndex = 0;

            NowPlayingText.Text = $"Now Playing: {Path.GetFileName(dialog.FileName)}";

            Player.Play();
        }
    }
}
