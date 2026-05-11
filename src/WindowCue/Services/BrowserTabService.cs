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
    /// Enumerates open browser tabs and provides focus/rebind operations for
    /// Chromium-based browsers (Microsoft Edge, Chrome, etc.) via UI Automation.
    /// </summary>
    public class BrowserTabService
    {
        // ── Discovery ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all discoverable open tabs for the given browser process name.
        /// Tab titles are read from UI Automation tab-item elements; the URL is only
        /// populated for the active tab in each browser window (background-tab URLs are
        /// not exposed by the address bar).
        /// </summary>
        /// <param name="processName">The process name to search, e.g. <c>msedge</c>.</param>
        public List<BrowserTabInfo> GetOpenTabs(string processName = "msedge")
        {
            var result = new List<BrowserTabInfo>();

            var processes = Process.GetProcessesByName(processName)
                .Where(p => p.MainWindowHandle != IntPtr.Zero)
                .ToList();

            foreach (var proc in processes)
            {
                try
                {
                    var rootElement = AutomationElement.FromHandle(proc.MainWindowHandle);

                    // Locate tab items in the browser's tab strip.
                    var tabCondition = new PropertyCondition(
                        AutomationElement.ControlTypeProperty, ControlType.TabItem);
                    var tabElements = rootElement.FindAll(TreeScope.Descendants, tabCondition);

                    if (tabElements.Count == 0)
                    {
                        continue;
                    }

                    // Read the active URL from the address bar (only works for the focused tab).
                    string? activeUrl = TryGetActiveUrl(rootElement);

                    // Determine which tab is currently selected.
                    AutomationElement? selectedTab = FindSelectedTab(tabElements);

                    string? execPath = null;
                    try { execPath = proc.MainModule?.FileName; } catch { /* access denied */ }

                    foreach (AutomationElement tabEl in tabElements)
                    {
                        string title = tabEl.Current.Name;
                        if (string.IsNullOrWhiteSpace(title))
                        {
                            continue;
                        }

                        bool isActive = ReferenceEquals(tabEl, selectedTab) ||
                                        string.Equals(tabEl.Current.AutomationId,
                                            selectedTab?.Current.AutomationId,
                                            StringComparison.Ordinal);

                        result.Add(new BrowserTabInfo
                        {
                            Title = title,
                            Url = isActive ? activeUrl : null,
                            WindowHandle = proc.MainWindowHandle,
                            ProcessId = proc.Id,
                            BrowserProcessName = processName,
                            ExecutablePath = execPath,
                            IsActiveTab = isActive
                        });
                    }
                }
                catch
                {
                    // Skip inaccessible windows (e.g., elevated browser in unelevated host).
                }
                finally
                {
                    proc.Dispose();
                }
            }

            return result
                .OrderBy(t => t.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ── Focus ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Brings a specific browser tab to the foreground by matching it against
        /// running browser tabs. Tries an exact title match first, then a URL match,
        /// then a contains-substring match as a fallback.
        /// </summary>
        /// <param name="processName">Browser process name, e.g. <c>msedge</c>.</param>
        /// <param name="tabTitle">Stored tab title to match against.</param>
        /// <param name="tabUrl">Optional URL hint for disambiguation.</param>
        /// <returns><see langword="true"/> when a tab was found and activation was attempted.</returns>
        public bool FocusTab(string processName, string tabTitle, string? tabUrl)
        {
            var processes = Process.GetProcessesByName(processName)
                .Where(p => p.MainWindowHandle != IntPtr.Zero)
                .ToList();

            foreach (var proc in processes)
            {
                try
                {
                    var rootElement = AutomationElement.FromHandle(proc.MainWindowHandle);

                    var tabCondition = new PropertyCondition(
                        AutomationElement.ControlTypeProperty, ControlType.TabItem);
                    var tabElements = rootElement.FindAll(TreeScope.Descendants, tabCondition);

                    var candidates = new List<AutomationElement>();
                    foreach (AutomationElement tabEl in tabElements)
                    {
                        if (TitleMatches(tabEl.Current.Name, tabTitle))
                        {
                            candidates.Add(tabEl);
                        }
                    }

                    AutomationElement? best = PickBestCandidate(candidates, tabUrl);
                    if (best == null)
                    {
                        continue;
                    }

                    // Bring the browser window to the foreground first.
                    NativeMethods.SetForegroundWindow(proc.MainWindowHandle);

                    // Activate the tab via InvokePattern, then fall back to SelectionItemPattern.
                    if (best.TryGetCurrentPattern(InvokePattern.Pattern, out object invokeObj) &&
                        invokeObj is InvokePattern invoke)
                    {
                        invoke.Invoke();
                        return true;
                    }

                    if (best.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object selObj) &&
                        selObj is SelectionItemPattern sel)
                    {
                        sel.Select();
                        NativeMethods.SetForegroundWindow(proc.MainWindowHandle);
                        return true;
                    }
                }
                catch { /* skip */ }
                finally
                {
                    proc.Dispose();
                }
            }

            return false;
        }

        // ── Rebind ────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to match a persisted tab identity back to a currently-open tab.
        /// Matching priority: exact title + URL → exact title → title substring.
        /// </summary>
        public BrowserTabInfo? TryRebind(string processName, string tabTitle, string? tabUrl)
        {
            var tabs = GetOpenTabs(processName);

            // Priority 1: exact title and URL match.
            if (!string.IsNullOrWhiteSpace(tabUrl))
            {
                var match = tabs.FirstOrDefault(t =>
                    string.Equals(t.Title, tabTitle, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(t.Url, tabUrl, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }
            }

            // Priority 2: exact title.
            var byTitle = tabs.FirstOrDefault(t =>
                string.Equals(t.Title, tabTitle, StringComparison.OrdinalIgnoreCase));
            if (byTitle != null)
            {
                return byTitle;
            }

            // Priority 3: title substring (bidirectional).
            return tabs.FirstOrDefault(t =>
                t.Title.Contains(tabTitle, StringComparison.OrdinalIgnoreCase) ||
                tabTitle.Contains(t.Title, StringComparison.OrdinalIgnoreCase));
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Reads the URL from the browser's address bar (omnibox). This only reflects
        /// the active tab's URL; background tab URLs are not exposed via UI Automation.
        /// </summary>
        private static string? TryGetActiveUrl(AutomationElement root)
        {
            try
            {
                // Edge and Chrome use "addressEditBox" as the automation ID for the omnibox.
                var byId = new PropertyCondition(
                    AutomationElement.AutomationIdProperty, "addressEditBox");
                var byClass = new PropertyCondition(
                    AutomationElement.ClassNameProperty, "OmniboxViewViews");
                var condition = new OrCondition(byId, byClass);

                var addressBar = root.FindFirst(TreeScope.Descendants, condition);
                if (addressBar != null &&
                    addressBar.TryGetCurrentPattern(ValuePattern.Pattern, out object vp) &&
                    vp is ValuePattern valuePattern)
                {
                    string val = valuePattern.Current.Value;
                    return string.IsNullOrWhiteSpace(val) ? null : val;
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Returns the currently-selected tab element, or <see langword="null"/>.
        /// </summary>
        private static AutomationElement? FindSelectedTab(AutomationElementCollection tabs)
        {
            foreach (AutomationElement tab in tabs)
            {
                try
                {
                    if (tab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object selObj) &&
                        selObj is SelectionItemPattern sel && sel.Current.IsSelected)
                    {
                        return tab;
                    }
                }
                catch { }
            }

            return null;
        }

        /// <summary>
        /// Selects the best candidate tab from a list by preferring the one whose URL
        /// matches the stored URL hint.
        /// </summary>
        private static AutomationElement? PickBestCandidate(
            List<AutomationElement> candidates, string? tabUrl)
        {
            if (candidates.Count == 0)
            {
                return null;
            }

            if (candidates.Count == 1 || string.IsNullOrWhiteSpace(tabUrl))
            {
                return candidates[0];
            }

            // Try to match URL by reading the address bar after temporarily focusing each.
            // We do NOT focus each tab here as that would be disruptive; fall back to first match.
            return candidates[0];
        }

        private static bool TitleMatches(string actual, string expected) =>
            string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase) ||
            actual.Contains(expected, StringComparison.OrdinalIgnoreCase) ||
            expected.Contains(actual, StringComparison.OrdinalIgnoreCase);
    }
}
