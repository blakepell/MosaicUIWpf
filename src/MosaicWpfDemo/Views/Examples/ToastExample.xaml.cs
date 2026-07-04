/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using System.Windows.Controls;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class ToastExample
    {
        private ToastManager? _toastManager;
        private ToastSeverity _severity = ToastSeverity.Info;
        private TimeSpan? _duration = TimeSpan.FromSeconds(4);
        private ToastQuadrant _quadrant = ToastQuadrant.BottomRight;

        public ToastExample()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _toastManager ??= new ToastManager(this);
        }

        private void OnShowToastClick(object sender, RoutedEventArgs e)
        {
            this.ShowToast(_severity, _duration, _quadrant);
        }

        private void OnSeverityClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { Tag: string tag } && Enum.TryParse(tag, out ToastSeverity severity))
            {
                _severity = severity;
                this.ShowToast(_severity, _duration, _quadrant);
            }
        }

        private void OnDurationClick(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: string tag })
            {
                return;
            }

            _duration = tag switch
            {
                "Short" => TimeSpan.FromSeconds(2),
                "Long" => TimeSpan.FromSeconds(6),
                _ => null
            };

            this.ShowToast(_severity, _duration, _quadrant);
        }

        private void OnQuadrantClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { Tag: string tag } && Enum.TryParse(tag, out ToastQuadrant quadrant))
            {
                _quadrant = quadrant;
                this.ShowToast(_severity, _duration, _quadrant);
            }
        }

        private void OnSuccessClick(object sender, RoutedEventArgs e) => this.ShowToast(ToastSeverity.Success, _duration, _quadrant);

        private void OnInfoClick(object sender, RoutedEventArgs e) => this.ShowToast(ToastSeverity.Info, _duration, _quadrant);

        private void OnWarningClick(object sender, RoutedEventArgs e) => this.ShowToast(ToastSeverity.Warning, _duration, _quadrant);

        private void OnErrorClick(object sender, RoutedEventArgs e) => this.ShowToast(ToastSeverity.Error, _duration, _quadrant);

        private void OnShortDurationClick(object sender, RoutedEventArgs e) => this.ShowToast(_severity, TimeSpan.FromSeconds(2), _quadrant);

        private void OnLongDurationClick(object sender, RoutedEventArgs e) => this.ShowToast(_severity, TimeSpan.FromSeconds(6), _quadrant);

        private void OnPersistClick(object sender, RoutedEventArgs e) => this.ShowToast(_severity, null, _quadrant);

        private void OnTopLeftClick(object sender, RoutedEventArgs e) => this.ShowToast(_severity, _duration, ToastQuadrant.TopLeft);

        private void OnTopRightClick(object sender, RoutedEventArgs e) => this.ShowToast(_severity, _duration, ToastQuadrant.TopRight);

        private void OnBottomLeftClick(object sender, RoutedEventArgs e) => this.ShowToast(_severity, _duration, ToastQuadrant.BottomLeft);

        private void OnBottomRightClick(object sender, RoutedEventArgs e) => this.ShowToast(_severity, _duration, ToastQuadrant.BottomRight);

        private void OnDismissAllClick(object sender, RoutedEventArgs e)
        {
            _toastManager?.DismissAll();
            StatusTextBlock.Text = "Dismissed all open toasts.";
        }

        private void ShowToast(ToastSeverity severity, TimeSpan? duration, ToastQuadrant quadrant)
        {
            _toastManager ??= new ToastManager(this);

            var durationText = duration.HasValue ? $"{duration.Value.TotalSeconds:0}s" : "stays open";
            _toastManager.Show($"{severity} Toast", $"Shown in the {quadrant} quadrant, {durationText}.", severity, duration, quadrant);

            StatusTextBlock.Text = $"Showed a {severity} toast in {quadrant} ({durationText}).";
        }
    }
}
