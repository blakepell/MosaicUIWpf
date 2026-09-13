/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Extensions;
using Mosaic.UI.Wpf.Scripting;
using Clipboard = System.Windows.Clipboard;

namespace Mosaic.UI.Wpf.Scripting.ScriptCommands
{
    /// <summary>
    /// Clipboard Script Commands
    /// </summary>
    [ScriptModule(Name = "clipboard")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class ClipboardScriptCommands
    {
        /// <summary>
        /// Sets plain text onto the clipboard.
        /// </summary>
        /// <param name="text"></param>
        [ScriptModuleMethod(Name = nameof(SetText),
            Description = "Sets plain text onto the clipboard.",
            ParameterCount = 1)]
        public void SetText(string text)
        {
            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                Clipboard.SetText(text, TextDataFormat.Text);
            });
        }

        /// <summary>
        /// Sets Unicode text onto the clipboard.
        /// </summary>
        /// <param name="text"></param>
        [ScriptModuleMethod(Name = nameof(SetUnicodeText),
            Description = "Sets Unicode text onto the clipboard.",
            ParameterCount = 1)]
        public void SetUnicodeText(string text)
        {
            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
            });
        }

        /// <summary>
        /// Sets HTML onto the clipboard.
        /// </summary>
        /// <param name="text"></param>
        [ScriptModuleMethod(Name = nameof(SetHtml),
            Description = "Sets HTML onto the clipboard.",
            ParameterCount = 1)]
        public void SetHtml(string text)
        {
            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                Clipboard.SetText(text, TextDataFormat.Html);
            });
        }

        /// <summary>
        /// Retrieves text from the clipboard.
        /// </summary>
        /// <returns>Text from the clipboard.</returns>
        [ScriptModuleMethod(Name = nameof(GetText),
            Description = "Returns text from the clipboard.",
            ParameterCount = 0)]
        public string GetText()
        {
            string text = "";

            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                text = Clipboard.GetText();
            });

            return text;
        }

        /// <summary>
        /// If the clipboard currently contains text.
        /// </summary>
        [ScriptModuleMethod(Name = nameof(ContainsText),
            Description = "If the clipboard currently contains text.",
            ParameterCount = 0)]
        public bool ContainsText()
        {
            bool containsText = false;

            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                containsText = Clipboard.ContainsText();
            });

            return containsText;
        }

        /// <summary>
        /// Permanently adds data to the clipboard so it is available after the program that set it ends.
        /// </summary>
        [ScriptModuleMethod(Name = nameof(Flush),
            Description = " Permanently adds data to the clipboard so it is available after the program that set it ends.",
            ParameterCount = 0)]
        public void Flush()
        {
            Application.Current.Dispatcher.InvokeIfRequired(Clipboard.Flush);
        }
    }
}