/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls.VT52Terminal;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class VT52TerminalExample
    {
        public static readonly DependencyProperty PhosphorEffectEnabledProperty = DependencyProperty.Register(nameof(PhosphorEffectEnabled), typeof(bool), typeof(VT52TerminalExample), new PropertyMetadata(false));
        public static readonly DependencyProperty LocalEchoEnabledProperty = DependencyProperty.Register(nameof(LocalEchoEnabled), typeof(bool), typeof(VT52TerminalExample), new PropertyMetadata(false));

        public bool PhosphorEffectEnabled
        {
            get => (bool)GetValue(PhosphorEffectEnabledProperty);
            set => SetValue(PhosphorEffectEnabledProperty, value);
        }

        public bool LocalEchoEnabled
        {
            get => (bool)GetValue(LocalEchoEnabledProperty);
            set => SetValue(LocalEchoEnabledProperty, value);
        }

        private bool _firstLoad = true;

        public VT52TerminalExample()
        {
            this.DataContext = this;
            InitializeComponent();
        }

        private async void VT52TerminalExample_OnLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Terminal.Focus();
                Keyboard.Focus(Terminal);
            }, DispatcherPriority.ContextIdle);

            if (!_firstLoad)
            {
                return;
            }

            _firstLoad = false;

            // Commented out: Ssh Example
            //var ssh = new SshConnection()
            //{
            //    BufferSize = 8192,
            //    Host = "",
            //    Username = "",
            //    KeyFile = @""
            //};

            //Terminal.Connection = ssh;
            //await Terminal.ConnectAsync();

            // We'll use a telnet example for now to a public MUD (multi-user dimension game).
            var telnet = new TelnetConnection()
            {
                Host = "dsl-mud.org",
                Port = 4000
            };

            Terminal.Connection = telnet;
            await Terminal.ConnectAsync();
        }
    }
}
