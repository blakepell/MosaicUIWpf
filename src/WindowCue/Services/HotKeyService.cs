/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.IO.HotKey;
using System.Windows;
using System.Windows.Input;
using WindowCue.ViewModels;

namespace WindowCue.Services
{
    /// <summary>
    /// Registers system-wide hotkeys for WindowCue and dispatches the corresponding
    /// ViewModel commands on the UI thread when they fire.
    /// Dispose this service on application exit to unregister all hotkeys.
    /// </summary>
    public class HotKeyService : IDisposable
    {
        private readonly HotKeyManager _hotKeyManager;
        private readonly MainWindowViewModel _mainVm;

        /// <summary>
        /// Initializes and registers the application hotkeys.
        /// </summary>
        public HotKeyService(MainWindowViewModel mainVm)
        {
            _mainVm = mainVm;
            _hotKeyManager = new HotKeyManager();
            _hotKeyManager.Register(Key.F14, ModifierKeys.None);
            _hotKeyManager.KeyPressed += OnKeyPressed;
        }

        private void OnKeyPressed(object? sender, KeyPressedEventArgs e)
        {
            if (e.HotKey.Key == Key.F14 && e.HotKey.Modifiers == ModifierKeys.None)
            {
                Application.Current.Dispatcher.InvokeAsync(
                    () => _mainVm.PinForegroundWindowCommand.Execute(null));
            }
        }

        /// <summary>
        /// Unregisters all hotkeys and releases the underlying Win32 resources.
        /// </summary>
        public void Dispose()
        {
            _hotKeyManager.KeyPressed -= OnKeyPressed;
            _hotKeyManager.Dispose();
        }
    }
}
