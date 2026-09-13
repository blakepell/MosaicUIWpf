/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Scripting;
using Mosaic.UI.Wpf.Logging;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1822

namespace Mosaic.UI.Wpf.Scripting.ScriptCommands
{
    /// <summary>
    /// Logging Script Commands
    /// </summary>
    [ScriptModule(Name = "log")]
    public partial class LogScriptCommands
    {
        /// <summary>
        /// Begins a logging bulk load.
        /// </summary>
        [ScriptModuleMethod(Description = "Begins logging bulk update.", ParameterCount = 0)]
        public void BeginUpdate()
        {
            Logger.BeginUpdate();
        }

        /// <summary>
        /// Ends a logging bulk load.
        /// </summary>
        [ScriptModuleMethod(Description = "Ends a logging bulk update.", ParameterCount = 0)]
        public void EndUpdate()
        {
            Logger.EndUpdate();
        }

        /// <summary>
        /// Writes an debug log message.
        /// </summary>
        /// <param name="msg"></param>
        [ScriptModuleMethod(Description = "Writes a debug level log message.", ParameterCount = 1)]
        public void Debug(string msg)
        {
            Logger.LogDebug(msg);
        }

        /// <summary>
        /// Writes an informational log message.
        /// </summary>
        /// <param name="msg"></param>
        [ScriptModuleMethod(Description = "Writes a info level log message.", ParameterCount = 1)]
        public void Info(string msg)
        {
            Logger.LogInfo(msg);
        }

        /// <summary>
        /// Writes a warning log message.
        /// </summary>
        /// <param name="msg"></param>
        [ScriptModuleMethod(Description = "Writes a warning level log message.", ParameterCount = 1)]
        public void Warning(string msg)
        {
            Logger.LogWarning(msg);
        }

        /// <summary>
        /// Writes an error log message.
        /// </summary>
        /// <param name="msg"></param>
        [ScriptModuleMethod(Description = "Writes an error level log message.", ParameterCount = 1)]
        public void Error(string msg)
        {
            Logger.LogError(msg);
        }

        /// <summary>
        /// Writes a success log message.
        /// </summary>
        /// <param name="msg"></param>
        [ScriptModuleMethod(Description = "Writes a success level log message.", ParameterCount = 1)]
        public void Success(string msg)
        {
            Logger.LogSuccess(msg);
        }
    }
}