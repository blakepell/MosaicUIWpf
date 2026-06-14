/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;

namespace Mosaic.UI.Wpf.Controls.WaveformVisualizer
{
    /// <summary>
    /// Displays a waveform captured from a selectable Windows audio input device.
    /// </summary>
    /// <remarks>
    /// The visualizer uses WASAPI shared-mode capture so other applications can use the selected input device concurrently.
    /// </remarks>
    /// <example>
    /// <code language="xml">
    /// &lt;ComboBox
    ///     ItemsSource="{Binding InputDevices, ElementName=Visualizer}"
    ///     SelectedItem="{Binding SelectedInputDevice, ElementName=Visualizer}" /&gt;
    /// &lt;mosaic:InputWaveformVisualizer x:Name="Visualizer" /&gt;
    /// </code>
    /// </example>
    public sealed class InputWaveformVisualizer : WaveformVisualizerBase
    {
        /// <summary>
        /// Identifies the <see cref="SelectedInputDevice"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedInputDeviceProperty = DependencyProperty.Register(
            nameof(SelectedInputDevice),
            typeof(AudioInputDevice),
            typeof(InputWaveformVisualizer),
            new FrameworkPropertyMetadata(null, OnSelectedInputDeviceChanged));

        private readonly ObservableCollection<AudioInputDevice> inputDevices = [];
        private readonly SemaphoreSlim refreshLock = new(1, 1);
        private CancellationTokenSource? refreshCancellation;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputWaveformVisualizer"/> class.
        /// </summary>
        public InputWaveformVisualizer()
        {
            InputDevices = new ReadOnlyObservableCollection<AudioInputDevice>(inputDevices);

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                RefreshInputDevices();
            }

            Unloaded += (_, _) => CancelInputDeviceRefresh();
        }

        /// <summary>
        /// Gets the observable collection of active audio input devices.
        /// </summary>
        /// <value>
        /// The read-only observable collection of selectable input devices.
        /// </value>
        public ReadOnlyObservableCollection<AudioInputDevice> InputDevices { get; }

        /// <summary>
        /// Gets or sets the audio input device captured by the visualizer.
        /// </summary>
        /// <value>
        /// The selected input device. The default is the entry that follows the current Windows default input device.
        /// </value>
        public AudioInputDevice? SelectedInputDevice
        {
            get => (AudioInputDevice?)GetValue(SelectedInputDeviceProperty);
            set => SetValue(SelectedInputDeviceProperty, value);
        }

        /// <summary>
        /// Starts an asynchronous refresh of the available audio input devices.
        /// </summary>
        public void RefreshInputDevices()
        {
            _ = ObserveInputDeviceRefreshAsync();
        }

        /// <summary>
        /// Refreshes the collection of available audio input devices without blocking the UI thread.
        /// </summary>
        /// <returns>A task that represents the asynchronous refresh operation.</returns>
        public async Task RefreshInputDevicesAsync()
        {
            CancellationTokenSource cancellation = new();
            CancellationTokenSource? previousCancellation = Interlocked.Exchange(
                ref refreshCancellation,
                cancellation);
            previousCancellation?.Cancel();

            try
            {
                await refreshLock.WaitAsync(cancellation.Token).ConfigureAwait(false);
                try
                {
                    List<AudioInputDevice> devices = await Task.Run(
                        AudioInputDeviceEnumerator.GetDevices,
                        cancellation.Token).ConfigureAwait(false);
                    cancellation.Token.ThrowIfCancellationRequested();

                    await Dispatcher.InvokeAsync(
                        () => ApplyInputDevices(devices),
                        DispatcherPriority.DataBind,
                        cancellation.Token);
                }
                finally
                {
                    refreshLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                ReportError(exception);
            }
            finally
            {
                Interlocked.CompareExchange(ref refreshCancellation, null, cancellation);
                cancellation.Dispose();
            }
        }

        private protected override IDisposable CreateCaptureSession(AudioCapture capture)
        {
            InputDeviceTracker tracker = new(
                capture,
                SelectedInputDevice?.Id,
                RefreshInputDevices,
                ReportError);
            tracker.Start();
            return tracker;
        }

        private static void OnSelectedInputDeviceChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            ((InputWaveformVisualizer)dependencyObject).RestartListening();
        }

        private void ApplyInputDevices(List<AudioInputDevice> devices)
        {
            string? selectedId = SelectedInputDevice?.Id;

            inputDevices.Clear();
            foreach (AudioInputDevice device in devices)
            {
                inputDevices.Add(device);
            }

            AudioInputDevice selectedDevice = inputDevices.FirstOrDefault(device => device.Id == selectedId)
                ?? inputDevices[0];
            SetCurrentValue(SelectedInputDeviceProperty, selectedDevice);
        }

        private async Task ObserveInputDeviceRefreshAsync()
        {
            await RefreshInputDevicesAsync();
        }

        private void CancelInputDeviceRefresh()
        {
            CancellationTokenSource? cancellation = Interlocked.Exchange(ref refreshCancellation, null);
            cancellation?.Cancel();
        }
    }
}
