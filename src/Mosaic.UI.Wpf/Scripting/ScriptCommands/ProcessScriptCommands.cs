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
    /// Scripts to interacting with processes.
    /// </summary>
    [ScriptModule(Name = "process")]
    public partial class ProcessScriptCommands
    {
        /// <summary>
        /// Starts a process.
        /// </summary>
        /// <param name="filePath"></param>
        [ScriptModuleMethod(Description = "Starts a process.",
            ParameterCount = 1)]
        public void Start(string filePath)
        {
            Start(filePath, "", true);
        }

        /// <summary>
        /// Starts a process
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="args"></param>
        [ScriptModuleMethod(Description = "Starts a process with the provided arguments.",
            ParameterCount = 2)]
        public void Start(string filePath, string args)
        {
            Start(filePath, args, true);
        }

        /// <summary>
        /// Starts a process
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="args"></param>
        /// <param name="useShellExecute"></param>
        [ScriptModuleMethod(Description = "Starts a process with the provided arguments.",
            ParameterCount = 3)]
        public void Start(string filePath, string args, bool useShellExecute)
        {
            new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = args,
                    WindowStyle = ProcessWindowStyle.Normal,
                    UseShellExecute = useShellExecute
                }
            }.Start();
        }

        /// <summary>
        /// Starts a process
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="args"></param>
        /// <param name="useShellExecute"></param>
        /// <param name="workingDirectory"></param>
        [ScriptModuleMethod(Description = "Starts a process with the provided arguments.",
            ParameterCount = 3)]
        public void Start(string filePath, string args, bool useShellExecute, string workingDirectory)
        {
            new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = args,
                    WindowStyle = ProcessWindowStyle.Normal,
                    UseShellExecute = useShellExecute,
                    WorkingDirectory = workingDirectory
                }
            }.Start();
        }

        /// <summary>
        /// Starts a process.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        [ScriptModuleMethod(Description = "Starts a process with the provided arguments.",
            ParameterCount = 2)]
        public void Start(string fileName, string[] arguments)
        {
            Process.Start(fileName, arguments);
        }

        /// <summary>
        /// Returns an array of all running processes.
        /// </summary>
        [ScriptModuleMethod(Description = "Returns an array of all running processes.",
            ParameterCount = 0)]
        public Process[] GetProcesses()
        {
            return Process.GetProcesses();
        }

        /// <summary>
        /// Gets the names of all running processes.
        /// </summary>
        [ScriptModuleMethod(Description = "Gets the names of all running processes.",
            ParameterCount = 0)]
        public string[] GetProcessNames()
        {
            return Process.GetProcesses().Select(x => x.ProcessName).ToArray();
        }

        /// <summary>
        /// Gets the Ids of all running processes.
        /// </summary>
        [ScriptModuleMethod(Description = "Gets the names of all running processes.",
                              ParameterCount = 0)]
        public int[] GetProcessIds()
        {
            return Process.GetProcesses().Select(x => x.Id).ToArray();
        }

        /// <summary>
        /// Gets the process ID of a program provided its name.  A return of -1 indicates the process was not found.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="exactMatch"></param>
        [ScriptModuleMethod(Description = "Gets the process ID of a program provided its name.  A return of -1 indicates the process was not found.",
                               ParameterCount = 1)]
        public int GetProcessIdByName(string name, bool exactMatch)
        {
            if (exactMatch)
            {
                return Process.GetProcesses().FirstOrDefault(x => x.ProcessName.Equals(name, StringComparison.Ordinal))?.Id ?? -1;
            }

            return Process.GetProcesses().FirstOrDefault(x => x.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase))?.Id ?? -1;
        }

        /// <summary>
        /// Gets if a specific process by name is running.
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="exactMatch"></param>
        [ScriptModuleMethod(Description = "Gets if a specific process by name is running.",
                               ParameterCount = 0)]
        public bool IsProcessRunning(string processName, bool exactMatch)
        {
            if (exactMatch)
            {
                return Process.GetProcesses().Any(x => x.ProcessName.Equals(processName, StringComparison.Ordinal));
            }

            return Process.GetProcesses().Any(x => x.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase));
        }


        /// <summary>
        /// Sets the focus to the specified process if it has a visible window.
        /// </summary>
        /// <param name="processId"></param>
        [ScriptModuleMethod(Description = "Sets the focus to the specified process if it has a visible window.",
            ParameterCount = 1)]
        public void SetFocus(int processId)
        {
            var proc = Process.GetProcessById(processId);
            _ = Argus.Windows.Window.SetForegroundWindow(proc.MainWindowHandle);
        }

        /// <summary>
        /// Sets the focus to the specified process if it has a visible window.
        /// </summary>
        /// <param name="processName"></param>
        [ScriptModuleMethod(Description = "Sets the focus to the specified process if it has a visible window.",
                               ParameterCount = 1)]
        public void SetFocus(string processName)
        {
            var proc = Process.GetProcessesByName(processName).FirstOrDefault();

            if (proc == null)
            {
                return;
            }

            _ = Argus.Windows.Window.SetForegroundWindow(proc.MainWindowHandle);
        }

        /// <summary>
        /// Gets the active window's title.  If return parent is true the parent window title is returned, otherwise the
        /// focused child window of the app will be returned.
        /// </summary>
        /// <param name="returnParent"></param>
        /// <returns></returns>
        [ScriptModuleMethod(Description = "Gets the active window's title.  If return parent is true the parent window title is returned, otherwise the focused child window of the app will be returned.",
                               ParameterCount = 1)]
        public string WindowTitle(bool returnParent)
        {
            return Argus.Windows.Window.GetActiveWindowTitle(returnParent);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, ref NativeRect rectangle);

        /// <summary>
        /// Returns a Rect for the specified process if it's found.
        /// </summary>
        /// <param name="id"></param>
        [ScriptModuleMethod(Description = "Returns a Rect for the specified process if it's found.",
            ParameterCount = 1)]
        public Rect GetPosition(int id)
        {
            var rect = new NativeRect();
            var teamsProc = Process.GetProcessById(id);
            _ = GetWindowRect(teamsProc.MainWindowHandle, ref rect);
            return new Rect(rect.Left, rect.Top, Math.Max(0, rect.Right - rect.Left), Math.Max(0, rect.Bottom - rect.Top));
        }

        /// <summary>
        /// Returns a Rect for the specified process if it's found.
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        [ScriptModuleMethod(Description = "Returns a Rect for the specified process if it's found.",
            ParameterCount = 1)]
        public Rect GetPosition(string processName)
        {
            var rect = new NativeRect();
            var teamsProc = Process.GetProcessesByName(processName).FirstOrDefault();

            if (teamsProc == null)
            {
                return new Rect(rect.Left, rect.Top, Math.Max(0, rect.Right - rect.Left), Math.Max(0, rect.Bottom - rect.Top));
            }

            _ = GetWindowRect(teamsProc.MainWindowHandle, ref rect);
            return new Rect(rect.Left, rect.Top, Math.Max(0, rect.Right - rect.Left), Math.Max(0, rect.Bottom - rect.Top));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern bool SetWindowPos(System.IntPtr hwnd, int hWndInsertAfter, int x, int Y, int width, int height, int wFlags);

        /// <summary>
        /// Sets the window position for a process.
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        [ScriptModuleMethod(Description = "Sets the window position for a process.",
            ParameterCount = 5)]
        public static void SetPosition(int processId, int x, int y, int width = 0, int height = 0)
        {
            var proc = Process.GetProcessById(processId);
            SetWindowPos(proc.MainWindowHandle, 0, x, y, width, height, width * height == 0 ? 1 : 0);
        }
    }
}