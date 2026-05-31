/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Diagnostics;
using System.Text;
using WindowCue.Common;
using WindowCue.Interop;

namespace WindowCue.Services
{
    /// <summary>
    /// Enumerates visible, taskbar-style application windows on the desktop, excluding
    /// WindowCue itself and system/shell windows.
    /// </summary>
    public class WindowEnumerationService
    {
        /// <summary>
        /// Common entries that should be ignored, generally Windows related.
        /// </summary>
        public static HashSet<string> IgnoreList = ["SystemSettings", "TextInputHost", "ApplicationFrameHost"];

        /// <summary>
        /// Returns a sorted list of visible, non-tool windows suitable for presentation use.
        /// WindowCue's own process windows are automatically filtered out.
        /// </summary>
        public List<WindowInfo> GetVisibleWindows()
        {
            var result = new List<WindowInfo>();
            var shellHwnd = NativeMethods.GetShellWindow();
            var desktopHwnd = NativeMethods.GetDesktopWindow();
            int ownPid = Environment.ProcessId;

            NativeMethods.EnumWindows((hWnd, _) =>
            {
                try
                {
                    // Skip shell and desktop
                    if (hWnd == shellHwnd || hWnd == desktopHwnd)
                    {
                        return true;
                    }

                    // Only visible windows
                    if (!NativeMethods.IsWindowVisible(hWnd))
                    {
                        return true;
                    }

                    // Skip child windows (not top-level)
                    if (NativeMethods.GetParent(hWnd) != IntPtr.Zero)
                    {
                        return true;
                    }

                    // Skip tool windows (not in Alt+Tab / taskbar)
                    long exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
                    if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0)
                    {
                        return true;
                    }

                    // Skip windows with no title
                    int len = NativeMethods.GetWindowTextLength(hWnd);
                    if (len == 0)
                    {
                        return true;
                    }

                    var sb = new StringBuilder(len + 1);
                    NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                    string title = sb.ToString();
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        return true;
                    }

                    // Get process info
                    NativeMethods.GetWindowThreadProcessId(hWnd, out int pid);
                    if (pid == 0)
                    {
                        return true;
                    }

                    // Skip our own process
                    if (pid == ownPid)
                    {
                        return true;
                    }

                    string processName = string.Empty;
                    string? executablePath = null;
                    string? commandLine = null;

                    try
                    {
                        using var proc = Process.GetProcessById(pid);
                        processName = proc.ProcessName;
                        try
                        {
                            executablePath = proc.MainModule?.FileName;
                        }
                        catch
                        {
                            /* access denied */
                        }

                        try
                        {
                            commandLine = proc.GetCommandLine();
                        }
                        catch
                        {
                            // Couldn't get the command line.
                        }


                    }
                    catch { return true; /* process already exited */ }

                    if (!IgnoreList.Contains(processName))
                    {
                        result.Add(new WindowInfo
                        {
                            Handle = hWnd,
                            Title = title,
                            ProcessId = pid,
                            ProcessName = processName,
                            ExecutablePath = executablePath,
                            CommandLine = commandLine
                        });
                    }
                }
                catch { /* ignore misbehaving windows */ }

                return true;
            }, IntPtr.Zero);

            return result
                .OrderBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(w => w.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The minimum composite score a candidate window must reach to be considered a
        /// confident re-bind. Scores below this are treated as "no match".
        /// </summary>
        public const double MatchThreshold = 0.45d;

        /// <summary>
        /// Attempts to find the currently-running window that best matches the given saved
        /// item data. Process identity (executable path, then process name) establishes that
        /// a candidate is the same application; the recorded window title then disambiguates
        /// between multiple instances using a fuzzy similarity score. This allows WindowCue
        /// to re-bind to the correct window even after its process ID has changed.
        /// </summary>
        public WindowInfo? TryRebind(string? executablePath, string processName, string windowTitle)
        {
            return FindBestMatch(GetVisibleWindows(), executablePath, processName, windowTitle, out _);
        }

        /// <summary>
        /// Scores every supplied window against the saved identity / title and returns the
        /// highest-scoring candidate, or <see langword="null"/> when none reaches
        /// <see cref="MatchThreshold"/>. The winning score is returned via
        /// <paramref name="confidence"/> (0.0–1.0).
        /// </summary>
        public WindowInfo? FindBestMatch(
            IReadOnlyList<WindowInfo> windows,
            string? executablePath,
            string processName,
            string windowTitle,
            out double confidence)
        {
            confidence = 0d;
            WindowInfo? best = null;

            foreach (var window in windows)
            {
                double score = ScoreCandidate(window, executablePath, processName, windowTitle);
                if (score > confidence)
                {
                    confidence = score;
                    best = window;
                }
            }

            return confidence >= MatchThreshold ? best : null;
        }

        /// <summary>
        /// Produces a 0.0–1.0 composite match score for a single candidate window.
        /// Identity (same executable or process) is weighted equally with title similarity;
        /// a candidate that shares no identity is rejected unless its title is an almost-exact
        /// match (covering apps whose executable path could not be read).
        /// </summary>
        private static double ScoreCandidate(
            WindowInfo candidate,
            string? executablePath,
            string processName,
            string windowTitle)
        {
            bool exeMatch = !string.IsNullOrWhiteSpace(executablePath)
                && string.Equals(candidate.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase);

            bool nameMatch = !string.IsNullOrWhiteSpace(processName)
                && string.Equals(candidate.ProcessName, processName, StringComparison.OrdinalIgnoreCase);

            double identity = exeMatch ? 1.0d : nameMatch ? 0.7d : 0.0d;
            double titleSimilarity = WindowTitleMatcher.Similarity(windowTitle, candidate.Title);

            // Require shared process identity, unless the title is an almost-exact match.
            if (identity <= 0d && titleSimilarity < 0.9d)
            {
                return 0d;
            }

            // With no saved title to compare against, fall back to identity alone so a
            // single running instance still re-binds.
            if (string.IsNullOrWhiteSpace(windowTitle))
            {
                return identity;
            }

            return (identity * 0.5d) + (titleSimilarity * 0.5d);
        }
    }
}
