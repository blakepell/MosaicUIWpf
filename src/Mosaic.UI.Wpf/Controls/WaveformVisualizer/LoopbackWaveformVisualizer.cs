/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls.WaveformVisualizer
{
    /// <summary>
    /// Displays a waveform captured from the default Windows audio render device.
    /// </summary>
    /// <remarks>
    /// The visualizer uses WASAPI loopback capture and follows changes to the default console render device.
    /// </remarks>
    /// <example>
    /// <code language="xml">
    /// &lt;mosaic:LoopbackWaveformVisualizer IsListening="True" /&gt;
    /// </code>
    /// </example>
    public sealed class LoopbackWaveformVisualizer : WaveformVisualizerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoopbackWaveformVisualizer"/> class.
        /// </summary>
        public LoopbackWaveformVisualizer()
        {
        }

        /// <inheritdoc/>
        private protected override IDisposable CreateCaptureSession(AudioCapture capture)
        {
            DeviceTracker tracker = new(capture, ReportError);
            tracker.Start();
            return tracker;
        }
    }
}
