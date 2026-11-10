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
using System.Windows.Automation;
using WindowCue.Interop;

namespace WindowCue.Services
{
    /// <summary>
    /// Discovers and selects browser tab strip items, including sleeping/discarded pages.
    /// Renderer process liveness is deliberately not used as tab liveness.
    /// </summary>
    public class BrowserTabService
    {
        internal BackgroundOperationRunner Work { get; } = new();
        internal BackgroundOperationRunner PickerWork { get; } = new();
        // Presentation clicks must not be rejected because the periodic inspector is busy.
        internal BackgroundOperationRunner ActivationWork { get; } = new();

        /// <summary>Returns discoverable tabs from every top-level browser window.</summary>
        public List<BrowserTabInfo> GetOpenTabs(string processName = "msedge") =>
            GetOpenTabs(processName, out _);

        /// <summary>
        /// Returns tab strip snapshots. A false completeness flag means automation could not
        /// inspect every window; a missing match in that case is not evidence of closure.
        /// </summary>
        public List<BrowserTabInfo> GetOpenTabs(string processName, out bool complete, IntPtr onlyWindow = default)
        {
            var result = new List<BrowserTabInfo>();
            if (string.IsNullOrWhiteSpace(processName))
            {
                complete = false;
                return result;
            }
            bool inspectedAll = true;
            var browserPids = new HashSet<int>();
            foreach (var browser in Process.GetProcessesByName(processName))
            {
                using (browser)
                    browserPids.Add(browser.Id);
            }
            bool enumerated = NativeMethods.EnumWindows((handle, _) =>
            {
                if (onlyWindow != IntPtr.Zero && handle != onlyWindow)
                    return true;
                if (!NativeMethods.IsWindowVisible(handle))
                {
                    return true;
                }

                NativeMethods.GetWindowThreadProcessId(handle, out int pid);
                if (!browserPids.Contains(pid))
                {
                    return true;
                }

                try
                {
                    using var process = Process.GetProcessById(pid);
                    if (!string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // Ignore browser utility/popup windows; include minimized browser windows.
                    var className = new System.Text.StringBuilder(256);
                    NativeMethods.GetClassName(handle, className, className.Capacity);
                    if (!className.ToString().StartsWith("Chrome_WidgetWin_", StringComparison.Ordinal))
                    {
                        return true;
                    }

                    try
                    {
                        var root = AutomationElement.FromHandle(handle);
                        var chrome = ReadBrowserChrome(root, out bool chromeComplete);
                        inspectedAll &= chromeComplete;
                        var tabs = chrome.Where(e => e.Current.ControlType == ControlType.TabItem).ToList();
                        if (tabs.Count == 0)
                        {
                            inspectedAll = false;
                            return true;
                        }
                        string? url = TryGetActiveUrl(chrome);
                        string? path = null;
                        try { path = process.MainModule?.FileName; } catch { }
                        foreach (AutomationElement tab in tabs)
                        {
                            try
                            {
                                string title = tab.Current.Name;
                                if (string.IsNullOrWhiteSpace(title))
                                {
                                    inspectedAll = false;
                                    continue;
                                }
                                bool selected = tab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern)
                                    && pattern is SelectionItemPattern selection && selection.Current.IsSelected;
                                result.Add(new BrowserTabInfo
                                {
                                    Title = title, Url = selected ? url : null,
                                    WindowHandle = handle, ProcessId = pid,
                                    BrowserProcessName = processName, ExecutablePath = path,
                                    IsActiveTab = selected, RuntimeId = tab.GetRuntimeId(),
                                    ProcessStartTimeUtcTicks = process.StartTime.ToUniversalTime().Ticks
                                });
                            }
                            catch { inspectedAll = false; }
                        }
                    }
                    catch { inspectedAll = false; }
                }
                catch
                {
                    // The process may have exited or access may be denied. Do not infer closure.
                    inspectedAll = false;
                }
                return true;
            }, IntPtr.Zero);
            complete = inspectedAll && enumerated;
            return result.OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Matches only the original tab strip identity in the original browser process lifetime.
        /// Page titles, URLs and window handles are hints, never replacement identities.
        /// </summary>
        public BrowserTabInfo? MatchTab(IEnumerable<BrowserTabInfo> tabs, string title, string? url,
            int[]? runtimeId = null, IntPtr windowHandle = default, long processStartTimeUtcTicks = 0)
        {
            if (runtimeId == null || runtimeId.Length == 0)
                return null;
            return tabs.FirstOrDefault(t => t.RuntimeId != null && t.RuntimeId.SequenceEqual(runtimeId) &&
                (processStartTimeUtcTicks == 0 || t.ProcessStartTimeUtcTicks == processStartTimeUtcTicks));
        }

        /// <summary>
        /// Maximizes the browser and selects its tab strip item to wake a dormant page.
        /// Failure to activate does not mean the tab is closed.
        /// </summary>
        public bool FocusTab(BrowserTabInfo tab)
        {
            if (!NativeMethods.IsWindow(tab.WindowHandle))
            {
                return false;
            }

            NativeMethods.ShowWindowAsync(tab.WindowHandle, NativeMethods.SW_MAXIMIZE);
            NativeMethods.BringWindowToTop(tab.WindowHandle);
            NativeMethods.SetForegroundWindow(tab.WindowHandle);
            try
            {
                var root = AutomationElement.FromHandle(tab.WindowHandle);
                var elements = ReadBrowserChrome(root, out _)
                    .Where(e => e.Current.ControlType == ControlType.TabItem);
                foreach (AutomationElement element in elements)
                {
                    if (tab.RuntimeId == null || !element.GetRuntimeId().SequenceEqual(tab.RuntimeId))
                    {
                        continue;
                    }

                    // Selection wakes sleeping tabs without depending on their renderer process.
                    bool selected = false;
                    try
                    {
                        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var value) &&
                            value is SelectionItemPattern selection)
                        {
                            selection.Select();
                            selected = selection.Current.IsSelected;
                        }
                    }
                    catch { /* Try Invoke independently if selection is unavailable. */ }
                    if (!selected && element.TryGetCurrentPattern(InvokePattern.Pattern, out var invokeValue) &&
                        invokeValue is InvokePattern invoke)
                    {
                        invoke.Invoke();
                        selected = true;
                    }
                    NativeMethods.SetForegroundWindow(tab.WindowHandle);
                    return selected && NativeMethods.GetForegroundWindow() == tab.WindowHandle;
                }
            }
            catch { /* The browser may be rebuilding its automation tree. Allow retry. */ }
            return false;
        }

        /// <summary>
        /// Reads the URL from the browser's address bar (omnibox). This only reflects
        /// the active tab's URL; background tab URLs are not exposed via UI Automation.
        /// </summary>
        private static string? TryGetActiveUrl(IEnumerable<AutomationElement> chrome)
        {
            try
            {
                var addressBar = chrome.FirstOrDefault(e =>
                    e.Current.AutomationId == "addressEditBox" || e.Current.ClassName == "OmniboxViewViews");
                if (addressBar != null &&
                    addressBar.TryGetCurrentPattern(ValuePattern.Pattern, out var value) &&
                    value is ValuePattern pattern)
                    return string.IsNullOrWhiteSpace(pattern.Current.Value) ? null : pattern.Current.Value;
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Walks browser controls only. Never descends into page documents, whose potentially
        /// enormous accessibility trees are unrelated to tab discovery. Budget exhaustion is
        /// an incomplete inspection, not evidence that a pinned tab has closed.
        /// </summary>
        private static List<AutomationElement> ReadBrowserChrome(AutomationElement root, out bool complete)
        {
            var found = new List<AutomationElement>();
            var pending = new Queue<AutomationElement>();
            pending.Enqueue(root);
            var clock = Stopwatch.StartNew();
            int visited = 0;
            complete = true;
            while (pending.Count > 0)
            {
                if (++visited > 512 || clock.Elapsed > TimeSpan.FromSeconds(1))
                {
                    complete = false;
                    break;
                }
                var element = pending.Dequeue();
                var type = element.Current.ControlType;
                if (type == ControlType.Document)
                    continue;
                if (type == ControlType.TabItem || type == ControlType.Edit)
                {
                    found.Add(element);
                    continue;
                }
                foreach (AutomationElement child in element.FindAll(TreeScope.Children, Condition.TrueCondition))
                    pending.Enqueue(child);
            }
            return found;
        }
    }
}
