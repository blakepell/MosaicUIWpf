/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Text;
using BbsNavigator.Views;
using Mosaic.UI.Wpf.Scripting;
using System.Windows.Threading;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Lets an alias script read from and write to the terminal session that ran it. Alias
    /// scripts see this object as <c>term</c>.
    /// </summary>
    /// <remarks>
    /// Every member is safe to call from a script's worker thread. Waits end early when the
    /// session closes or its scripts are stopped.
    /// </remarks>
    [ScriptModule(Name = "term", Description = "The terminal session that ran this alias.")]
    public sealed class TerminalScriptCommands
    {
        private readonly BbsTerminalView _view;

        /// <summary>
        /// Initializes a bridge to <paramref name="view"/>.
        /// </summary>
        /// <param name="view">The terminal session scripts act on.</param>
        public TerminalScriptCommands(BbsTerminalView view)
        {
            ArgumentNullException.ThrowIfNull(view);
            _view = view;
        }

        private Dispatcher Dispatcher => _view.Dispatcher;

        /// <summary>
        /// Gets the name of the BBS this session is connected to.
        /// </summary>
        [ScriptModuleMethod(Description = "Gets the name of the BBS this session is connected to.")]
        public string Name => Invoke(() => _view.Profile.Name);

        /// <summary>
        /// Gets the host name of the BBS.
        /// </summary>
        [ScriptModuleMethod(Description = "Gets the host name of the BBS.")]
        public string Host => Invoke(() => _view.Profile.Host);

        /// <summary>
        /// Gets whether the session is connected.
        /// </summary>
        [ScriptModuleMethod(Description = "Gets whether the session is connected.")]
        public bool IsConnected => Invoke(() => _view.IsConnected);

        /// <summary>
        /// Sends text to the BBS exactly as given, without a trailing Enter.
        /// </summary>
        /// <param name="text">The text to send.</param>
        [ScriptModuleMethod(Description = "Sends text to the BBS exactly as given, without pressing Enter.", ParameterCount = 1)]
        public void Send(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Invoke(() => _view.SendScriptTextAsync(text)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sends text to the BBS followed by Enter.
        /// </summary>
        /// <param name="text">The text to send.</param>
        [ScriptModuleMethod(Description = "Sends text to the BBS followed by Enter.", ParameterCount = 1)]
        public void SendLine(string text)
        {
            Send((text ?? string.Empty) + "\r");
        }

        /// <summary>
        /// Writes text to this terminal's screen only; nothing is sent to the BBS.
        /// </summary>
        /// <param name="text">The text to show. ANSI escape sequences are honored.</param>
        [ScriptModuleMethod(Description = "Writes a line to this terminal's screen without sending it to the BBS. ANSI codes are honored.", ParameterCount = 1)]
        public void Echo(string text)
        {
            Invoke(() => _view.EchoScriptText((text ?? string.Empty) + "\r\n"));
        }

        /// <summary>
        /// Shows a message in the terminal's status bar.
        /// </summary>
        /// <param name="message">The message to show.</param>
        [ScriptModuleMethod(Description = "Shows a message in the terminal's status bar.", ParameterCount = 1)]
        public void Status(string message)
        {
            Invoke(() => _view.ShowScriptStatus(message ?? string.Empty));
        }

        /// <summary>
        /// Gets the text currently on the terminal screen.
        /// </summary>
        /// <returns>The screen text, one line per row.</returns>
        [ScriptModuleMethod(Description = "Gets the text currently on the terminal screen.")]
        public string GetScreenText() => Invoke(() => _view.ScreenText);

        /// <summary>
        /// Gets the lines currently on the terminal screen.
        /// </summary>
        /// <returns>The screen lines, top to bottom.</returns>
        [ScriptModuleMethod(Description = "Gets the lines currently on the terminal screen, top to bottom.")]
        public string[] GetLines() => GetScreenText().ReplaceLineEndings("\n").Split('\n');

        /// <summary>
        /// Gets the last line on the screen that contains text, which is usually the current prompt.
        /// </summary>
        /// <returns>The line, or an empty string when the screen is blank.</returns>
        [ScriptModuleMethod(Description = "Gets the last non-blank line on the screen, usually the current prompt.")]
        public string GetLastLine() => GetLines().LastOrDefault(l => !string.IsNullOrWhiteSpace(l))?.TrimEnd() ?? string.Empty;

        /// <summary>
        /// Waits until the BBS sends <paramref name="text"/>, ignoring case and ANSI escape sequences.
        /// </summary>
        /// <param name="text">The text to wait for.</param>
        /// <param name="timeoutMilliseconds">How long to wait.</param>
        /// <returns><see langword="true"/> when the text arrived; <see langword="false"/> on timeout.</returns>
        [ScriptModuleMethod(Description = "Waits until the BBS sends this text (case-insensitive). Returns false if the timeout in milliseconds passes first.", ParameterCount = 2)]
        public bool WaitFor(string text, int timeoutMilliseconds)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            var found = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var received = new StringBuilder();

            void OnTextReceived(string data)
            {
                lock (received)
                {
                    received.Append(data);
                    if (received.ToString().Contains(text, StringComparison.OrdinalIgnoreCase))
                    {
                        found.TrySetResult();
                        return;
                    }

                    // Keep only enough to match text split across two reads.
                    if (received.Length >= text.Length)
                    {
                        received.Remove(0, received.Length - text.Length + 1);
                    }
                }
            }

            _view.TextReceived += OnTextReceived;
            try
            {
                return found.Task.Wait(Math.Max(0, timeoutMilliseconds), _view.ScriptCancellationToken);
            }
            finally
            {
                _view.TextReceived -= OnTextReceived;
            }
        }

        /// <summary>
        /// Pauses the script.
        /// </summary>
        /// <param name="milliseconds">How long to pause.</param>
        [ScriptModuleMethod(Description = "Pauses the script for the given number of milliseconds.", ParameterCount = 1)]
        public void Sleep(int milliseconds)
        {
            Task.Delay(Math.Max(0, milliseconds), _view.ScriptCancellationToken).GetAwaiter().GetResult();
        }

        private T Invoke<T>(Func<T> func) => Dispatcher.CheckAccess() ? func() : Dispatcher.Invoke(func);

        private void Invoke(Action action)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.Invoke(action);
            }
        }
    }
}
