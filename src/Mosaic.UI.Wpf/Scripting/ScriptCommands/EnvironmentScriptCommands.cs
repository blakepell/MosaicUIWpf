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

namespace Mosaic.UI.Wpf.Scripting.ScriptCommands
{
    /// <summary>
    /// General script commands.
    /// </summary>
    [ScriptModule(Name = "environ")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class EnvironmentScriptCommands
    {
        /// <summary>
        /// Returns the location of the currently logged in users desktop folder.
        /// </summary>
        [ScriptModuleMethod(Description = "Returns the location of the currently logged in users desktop folder.",
                               ParameterCount = 0)]
        public string DesktopFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        /// <summary>
        /// Returns the location of the currently logged in users document folder.
        /// </summary>
        [ScriptModuleMethod(Description = "Returns the location of the currently logged in users document folder.",
            ParameterCount = 0)]
        public string DocumentsFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        /// <summary>
        /// Returns the location of the application data folder.
        /// </summary>
        [ScriptModuleMethod(Description = "Returns the location of the application data folder.",
            ParameterCount = 0)]
        public string ApplicationDataFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        /// <summary>
        /// Returns the location of the local application data folder.
        /// </summary>
        [ScriptModuleMethod(Description = "Returns the location of the local application data folder.",
            ParameterCount = 0)]
        public string LocalApplicationDataFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        /// <summary>
        /// Returns the location of the common application data folder.
        /// </summary>
        [ScriptModuleMethod(Description = "Returns the location of the common application data folder.",
            ParameterCount = 0)]
        public string CommonApplicationDataFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        }

        /// <summary>
        /// Returns the directory the current application is scoped to.
        /// </summary>
        [ScriptModuleMethod(Description = "Returns the directory the current application is scoped to.",
            ParameterCount = 0)]
        public string CurrentDirectory()
        {
            return Environment.CurrentDirectory;
        }

        /// <summary>
        /// The current computer/servers name.
        /// </summary>
        [ScriptModuleMethod(Description = "The current computer/servers name.",
            ParameterCount = 0)]
        public string MachineName()
        {
            return Environment.MachineName;
        }

        /// <summary>
        /// The username of the account currently logged into the system.
        /// </summary>
        [ScriptModuleMethod(Description = "The username of the account currently logged into the system.",
            ParameterCount = 0)]
        public string Username()
        {
            return Environment.UserName;
        }

        /// <summary>
        /// The domain name of the account that is currently logged in is associated with.
        /// </summary>
        [ScriptModuleMethod(Description = "The domain name of the account that is currently logged in is associated with.",
            ParameterCount = 0)]
        public string UserDomainName()
        {
            return Environment.UserDomainName;
        }

        /// <summary>
        /// Returns the environment variable for the specified name.  If no value exists a blank string is returned.
        /// </summary>
        [ScriptModuleMethod(Description = "Returns the environment variable for the specified name.  If no value exists a blank string is returned.",
            ParameterCount = 1)]
        public string GetEnvironmentVariable(string variableName)
        {
            return Environment.GetEnvironmentVariable(variableName) ?? "";
        }

        /// <summary>
        /// Sets the value of a specified environment variable.
        /// </summary>
        [ScriptModuleMethod(Description = "Sets the value of a specified environment variable.",
            ParameterCount = 2)]
        public void SetEnvironmentVariable(string variableName, string value)
        {
            Environment.SetEnvironmentVariable(variableName, value);
        }

        /// <summary>
        /// Returns an array of the logical drives attached to a computer.
        /// </summary>
        [ScriptModuleMethod(Description = "Returns an array of the logical drives attached to a computer.",
            ParameterCount = 0)]
        public string[] LogicalDriveList()
        {
            return Environment.GetLogicalDrives();
        }

        /// <summary>
        /// Returns the number of ticks since the system started.
        /// </summary>
        [ScriptModuleMethod(Description = "Returns the number of ticks since the system started.",
            ParameterCount = 0)]
        public int TickCount()
        {
            return Environment.TickCount;
        }

        /// <summary>
        /// Whether a system shutdown has started or not.
        /// </summary>
        [ScriptModuleMethod(Description = "Whether a system shutdown has started or not.",
            ParameterCount = 0)]
        public bool HasShutdownStarted()
        {
            return Environment.HasShutdownStarted;
        }

        /// <summary>
        /// The process ID or PID for the current instance of this application.
        /// </summary>
        [ScriptModuleMethod(Description = "The process ID or PID for the current instance of this application.",
            ParameterCount = 0)]
        public int ProcessId()
        {
            return Environment.ProcessId;
        }

        /// <summary>
        /// Combines two paths or path and a file.
        /// </summary>
        [ScriptModuleMethod(Description = "Combines two paths or path and a file.",
            ParameterCount = 2)]
        public string PathCombine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        /// <summary>
        /// Combines two paths or path and a file.
        /// </summary>
        [ScriptModuleMethod(Description = "Combines two paths or path and a file.",
            ParameterCount = 2)]
        public string PathCombine(string path1, string path2, string path3)
        {
            return Path.Combine(path1, path2, path3);
        }

    }
}