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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    [ObservableObject]
    public partial class WebcamExample
    {
        private readonly DispatcherTimer _statsTimer;
        private int _framesThisSecond;
        private long _lastFrameNumber;

        /// <summary>
        /// The cameras found by <see cref="WebcamDeviceManager.GetDevices"/>.
        /// </summary>
        [ObservableProperty]
        private IReadOnlyList<WebcamDevice> _devices = [];

        /// <summary>
        /// The frame frozen on screen while paused, or null.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
        [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand))]
        private BitmapSource? _pausedFrame;

        [ObservableProperty]
        private int _displayedFramesPerSecond;

        [ObservableProperty]
        private long _droppedFrames;

        public ObservableCollection<BitmapSource> Snapshots { get; } = new();

        public ObservableCollection<string> ActivityLog { get; } = new();

        public WebcamExample()
        {
            InitializeComponent();
            this.DataContext = this;

            // FrameReceived counts drive a simple once-a-second fps readout.
            _statsTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) =>
            {
                this.DisplayedFramesPerSecond = _framesThisSecond;
                _framesThisSecond = 0;
            }, this.Dispatcher);

            this.RefreshDevices();
        }

        /// <summary>
        /// Starts the camera, or resumes it after a pause.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanPlay))]
        private async Task Play()
        {
            this.PausedFrame = null;

            try
            {
                await this.Camera.StartAsync();
            }
            catch (WebcamException)
            {
                // Already logged by the Error handler below.
            }
        }

        private bool CanPlay() => this.Camera.Device != null && (!this.Camera.IsRunning || this.PausedFrame != null);

        /// <summary>
        /// Freezes the current frame on screen and releases the camera so other apps can use it.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanPause))]
        private async Task Pause()
        {
            this.PausedFrame = await this.Camera.CaptureFrameAsync();
            await this.Camera.StopAsync();
        }

        private bool CanPause() => this.Camera.IsRunning && this.PausedFrame == null;

        [RelayCommand(CanExecute = nameof(CanStop))]
        private async Task Stop()
        {
            this.PausedFrame = null;
            await this.Camera.StopAsync();
        }

        private bool CanStop() => this.Camera.State is WebcamState.Starting or WebcamState.Running || this.PausedFrame != null;

        /// <summary>
        /// Captures a still without stopping the camera.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCapture))]
        private async Task Capture()
        {
            BitmapSource? snapshot = await this.Camera.CaptureFrameAsync();

            if (snapshot != null)
            {
                this.Snapshots.Insert(0, snapshot);
                this.Log($"Captured {snapshot.PixelWidth} × {snapshot.PixelHeight} snapshot.");
            }
        }

        private bool CanCapture() => this.Camera.IsRunning;

        [RelayCommand]
        private void RefreshDevices()
        {
            try
            {
                this.Devices = WebcamDeviceManager.GetDevices();
                this.Log($"Found {this.Devices.Count} camera(s).");

                // Keep the current selection if it is still connected (WebcamDevice equality uses the symbolic link).
                if (this.Camera.Device == null || !this.Devices.Contains(this.Camera.Device))
                {
                    this.Camera.Device = this.Devices.FirstOrDefault();
                }
            }
            catch (WebcamException ex)
            {
                this.Log($"Device enumeration failed: {ex.Message}");
            }

            this.RefreshCommands();
        }

        [RelayCommand]
        private void AutomaticResolution()
        {
            this.Camera.Resolution = null;
        }

        private void Camera_OnStateChanged(object sender, RoutedPropertyChangedEventArgs<WebcamState> e)
        {
            if (e.NewValue == WebcamState.Running)
            {
                _lastFrameNumber = 0;
                _framesThisSecond = 0;
                this.DroppedFrames = 0;
                _statsTimer.Start();
            }
            else
            {
                _statsTimer.Stop();
                this.DisplayedFramesPerSecond = 0;
            }

            this.RefreshCommands();
        }

        private void Camera_OnStarted(object? sender, EventArgs e) => this.Log($"Started: {this.Camera.Device?.Name} at {this.Camera.ActualResolution}.");

        private void Camera_OnStopped(object? sender, EventArgs e) => this.Log("Stopped; camera released.");

        private void Camera_OnError(object? sender, WebcamErrorEventArgs e) => this.Log($"Error ({e.Kind}): {e.Exception.Message}");

        private void Camera_OnFrameReceived(object? sender, WebcamFrameEventArgs e)
        {
            // Frame numbers are assigned by the capture thread, so gaps are frames the UI skipped to stay current.
            if (_lastFrameNumber != 0)
            {
                this.DroppedFrames += e.FrameNumber - _lastFrameNumber - 1;
            }

            _lastFrameNumber = e.FrameNumber;
            _framesThisSecond++;
        }

        private void RefreshCommands()
        {
            this.PlayCommand.NotifyCanExecuteChanged();
            this.PauseCommand.NotifyCanExecuteChanged();
            this.StopCommand.NotifyCanExecuteChanged();
            this.CaptureCommand.NotifyCanExecuteChanged();
        }

        private void Log(string message)
        {
            this.ActivityLog.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");

            if (this.ActivityLog.Count > 50)
            {
                this.ActivityLog.RemoveAt(this.ActivityLog.Count - 1);
            }
        }
    }
}
