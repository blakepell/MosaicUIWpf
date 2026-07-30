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
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// The states the sample execution engine can be in.
    /// </summary>
    public enum ExecutionRunState
    {
        Stopped,
        Running,
        Paused
    }

    [ObservableObject]
    public partial class ExecutionControlExample
    {
        /// <summary>
        /// The current state of the sample execution engine.  Every command re-evaluates its
        /// CanExecute when this changes, which is what enables and mutes the buttons.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PlayCommand))]
        [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand))]
        private ExecutionRunState _runState = ExecutionRunState.Stopped;

        /// <summary>
        /// A running log of the commands and routed events raised by the control.
        /// </summary>
        public ObservableCollection<string> ActivityLog { get; } = new();

        public ExecutionControlExample()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        /// <summary>
        /// Play is available while stopped or paused, but not while already running.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanPlay))]
        private void Play()
        {
            this.RunState = ExecutionRunState.Running;
            this.Log("Play command executed.");
        }

        private bool CanPlay() => this.RunState != ExecutionRunState.Running;

        /// <summary>
        /// Pause is only available while running.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanPause))]
        private void Pause()
        {
            this.RunState = ExecutionRunState.Paused;
            this.Log("Pause command executed.");
        }

        private bool CanPause() => this.RunState == ExecutionRunState.Running;

        /// <summary>
        /// Stop is available whenever something is in flight, which is exactly when play is not.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            this.RunState = ExecutionRunState.Stopped;
            this.Log("Stop command executed.");
        }

        private bool CanStop() => this.RunState != ExecutionRunState.Stopped;

        private void ExecutionControl_OnPlayClick(object sender, RoutedEventArgs e) => this.Log("PlayClick routed event.");

        private void ExecutionControl_OnPauseClick(object sender, RoutedEventArgs e) => this.Log("PauseClick routed event.");

        private void ExecutionControl_OnStopClick(object sender, RoutedEventArgs e) => this.Log("StopClick routed event.");

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
