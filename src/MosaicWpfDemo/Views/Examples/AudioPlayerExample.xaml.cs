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
    }
}
