/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Interop;

namespace Mosaic.UI.Wpf.IO.HotKey
{
    /// <summary>
    /// Setups system-wide hot keys and provides possibility to react on their events.
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Example usage:
    /// 
    /// var hotKeyManager = new HotKeyManager();
    /// hotKeyManager.Register(Key.F12, ModifierKeys.Control | ModifierKeys.Shift);
    /// hotKeyManager.KeyPressed += (sender, e) =>
    /// {
    ///     if (e.HotKey.Key == Key.F12 && e.HotKey.Modifiers.HasFlag(ModifierKeys.Control))
    ///     {
    ///         // Handle hotkey press
    ///     }
    /// };
    /// 
    /// // Remember to dispose when done
    /// hotKeyManager.Dispose();
    /// ]]>
    /// </remarks>
    public class HotKeyManager : IDisposable
    {
        /// <summary>
        /// The window handle source used to receive messages.
        /// </summary>
        private readonly HwndSource _windowHandleSource;

        /// <summary>
        /// The registered hot keys.
        /// </summary>
        private readonly Dictionary<HotKey, int> _registered;

        /// <summary>
        /// Occurs when registered system wide hot key is pressed.
        /// </summary>
        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        /// <summary>
        /// Initializes a new instance of the <see cref="HotKeyManager"/> class.
        /// </summary>
        public HotKeyManager()
        {
            _windowHandleSource = new HwndSource(new HwndSourceParameters());
            _windowHandleSource.AddHook(this.MessageHandler);
            _registered = new Dictionary<HotKey, int>();
        }

        /// <summary>
        /// Registers the system-wide hot key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="modifiers">The key modifiers.</param>
        /// <returns>The registered <see cref="HotKey"/>.</returns>
        public HotKey Register(Key key, ModifierKeys modifiers)
        {
            var hotKey = new HotKey(key, modifiers);
            this.Register(hotKey);
            return hotKey;
        }

        /// <summary>
        /// Registers the system-wide hot key.
        /// </summary>
        /// <param name="hotKey">The hot key.</param>
        public void Register(HotKey hotKey)
        {
            // Check if specified hot key is already registered.
            if (_registered.ContainsKey(hotKey))
            {
                throw new ArgumentException("The specified hot key is already registered.");
            }

            // Register new hot key.
            var id = this.NextId();

            if (!WinApi.RegisterHotKey(_windowHandleSource.Handle, id, hotKey.Key, hotKey.Modifiers))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "Hot key registration failed.  The hot key might be in use by another process.");
            }

            _registered.Add(hotKey, id);
        }

        /// <summary>
        /// Register a system-wide hot key converter a char into a <see cref="Key"/>.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="modifiers"></param>
        public void Register(char key, ModifierKeys modifiers)
        {
            this.Register(ConvertCharToKey(key), modifiers);
        }

        /// <summary>
        /// Unregisters previously registered hot key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="modifiers">The key modifiers.</param>
        public void Unregister(Key key, ModifierKeys modifiers)
        {
            var hotKey = new HotKey(key, modifiers);
            this.Unregister(hotKey);
        }

        /// <summary>
        /// Unregisters previously registered hot key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="modifiers"></param>
        public void Unregister(char key, ModifierKeys modifiers)
        {
            var hotKey = new HotKey(ConvertCharToKey(key), modifiers);
            this.Unregister(hotKey);
        }

        /// <summary>
        /// Unregisters previously registered hot key.
        /// </summary>
        /// <param name="hotKey">The registered hot key.</param>
        public void Unregister(HotKey hotKey)
        {
            if (_registered.TryGetValue(hotKey, out int id))
            {
                WinApi.UnregisterHotKey(_windowHandleSource.Handle, id);
                _registered.Remove(hotKey);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="HotKeyManager"/> class.
        /// </summary>
        ~HotKeyManager()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Disposes the resources used by the <see cref="HotKeyManager"/> class.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            // Unregister hot keys.
            foreach (var hotKey in _registered)
            {
                WinApi.UnregisterHotKey(_windowHandleSource.Handle, hotKey.Value);
            }

            _windowHandleSource.RemoveHook(this.MessageHandler);
            _windowHandleSource.Dispose();
        }

        /// <summary>
        /// Converts a character to a <see cref="Key"/>. This method assumes that the character is a valid key.
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static Key ConvertCharToKey(char ch)
        {
            if (char.IsLetter(ch))
            {
                // For letters, convert directly using Enum.Parse
                return (Key)Enum.Parse(typeof(Key), ch.ToString().ToUpper());
            }

            if (char.IsDigit(ch))
            {
                // For numbers, convert directly if it's not on the numeric keypad
                if (ch >= '0' && ch <= '9')
                {
                    return (Key)Enum.Parse(typeof(Key), "D" + ch);
                }
            }

            // Add handling here for special characters
            // This part might require a custom mapping depending on your needs and keyboard layout
            throw new ArgumentException("The provided character is not supported.");
        }

        /// <summary>
        /// Generates the next ID for the hot key registration.
        /// </summary>
        private int NextId()
        {
            return _registered.Any() ? _registered.Values.Max() + 1 : 0;
        }

        /// <summary>
        /// Handles the window messages for hot key events.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="message"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        private IntPtr MessageHandler(IntPtr handle, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == WinApi.WmHotKey)
            {
                // Extract key and modifiers from the message.
                var key = KeyInterop.KeyFromVirtualKey(((int)lParam >> 16) & 0xFFFF);
                var modifiers = (ModifierKeys)((int)lParam & 0xFFFF);

                var hotKey = new HotKey(key, modifiers);
                this.OnKeyPressed(new KeyPressedEventArgs(hotKey));

                handled = true;
                return new IntPtr(1);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Raises the KeyPressed event.
        /// </summary>
        /// <param name="e"></param>
        private void OnKeyPressed(KeyPressedEventArgs e)
        {
            this.KeyPressed?.Invoke(this, e);
        }
    }
}