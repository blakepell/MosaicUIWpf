/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Views;
using Mosaic.UI.Wpf.AvalonDock;
using Mosaic.UI.Wpf.AvalonDock.Layout;
using Mosaic.UI.Wpf.Scripting;
using System.Windows.Threading;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Creates and finds terminal info panels by id. One instance lives for the life of the main
    /// window, so a panel created by one script run or script engine can be retrieved by any other.
    /// </summary>
    /// <remarks>
    /// Ids are trimmed and compared case-insensitively. A panel stays registered until its document
    /// or tool window is closed. Every member is safe to call from a script's worker thread.
    /// </remarks>
    [ScriptModule(Name = "panels", Description = "Creates and finds terminal info panels by id.")]
    public sealed class PanelScriptCommands
    {
        private readonly DockingManager _dock;
        private readonly Dictionary<string, TerminalPanel> _panels = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes the registry for panels hosted in <paramref name="dock"/>.
        /// </summary>
        /// <param name="dock">The docking manager that hosts panel documents and tool windows.</param>
        public PanelScriptCommands(DockingManager dock)
        {
            ArgumentNullException.ThrowIfNull(dock);
            _dock = dock;
        }

        private Dispatcher Dispatcher => _dock.Dispatcher;

        /// <summary>
        /// Gets the number of open panels.
        /// </summary>
        [ScriptModuleMethod(Description = "Gets the number of open panels.")]
        public int Count => Invoke(() => _panels.Count);

        /// <summary>
        /// Gets the ids of the open panels.
        /// </summary>
        /// <returns>The ids, in no particular order.</returns>
        [ScriptModuleMethod(Description = "Gets the ids of the open panels.")]
        public string[] GetIds() => Invoke(() => _panels.Keys.ToArray());

        /// <summary>
        /// Opens a panel as a document tab, or returns the panel already open under <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id used to retrieve the panel later.</param>
        /// <param name="title">The tab title.</param>
        /// <returns>The panel.</returns>
        [ScriptModuleMethod(Description = "Opens a panel as a document tab, or returns the open panel with this id.", ParameterCount = 2)]
        public TerminalPanel Create(string id, string title) => Open(id, title, toolWindow: false, AnchorableShowStrategy.Right);

        /// <summary>
        /// Opens a panel as a tool window docked on the right, or returns the panel already open under <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id used to retrieve the panel later.</param>
        /// <param name="title">The tool window title.</param>
        /// <returns>The panel.</returns>
        [ScriptModuleMethod(Description = "Opens a panel as a tool window on the right, or returns the open panel with this id.", ParameterCount = 2)]
        public TerminalPanel CreateTool(string id, string title) => Open(id, title, toolWindow: true, AnchorableShowStrategy.Right);

        /// <summary>
        /// Opens a panel as a tool window docked on a side, or returns the panel already open under <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id used to retrieve the panel later.</param>
        /// <param name="title">The tool window title.</param>
        /// <param name="side"><c>left</c>, <c>right</c>, <c>top</c>, or <c>bottom</c>.</param>
        /// <returns>The panel.</returns>
        [ScriptModuleMethod(Description = "Opens a panel as a tool window on a side (left, right, top, bottom), or returns the open panel with this id.", ParameterCount = 3)]
        public TerminalPanel CreateTool(string id, string title, string side)
        {
            var strategy = side?.Trim().ToLowerInvariant() switch
            {
                "left" => AnchorableShowStrategy.Left,
                "right" => AnchorableShowStrategy.Right,
                "top" => AnchorableShowStrategy.Top,
                "bottom" => AnchorableShowStrategy.Bottom,
                _ => throw new ArgumentException("The side must be left, right, top, or bottom.", nameof(side))
            };

            return Open(id, title, toolWindow: true, strategy);
        }

        /// <summary>
        /// Gets an open panel.
        /// </summary>
        /// <param name="id">The id the panel was created with.</param>
        /// <returns>The panel, or <see langword="null"/> when none is open under <paramref name="id"/>.</returns>
        [ScriptModuleMethod(Description = "Gets the open panel with this id, or null.", ParameterCount = 1)]
        public TerminalPanel? Get(string id) => Invoke(() => _panels.GetValueOrDefault(NormalizeId(id)));

        /// <summary>
        /// Gets whether a panel is open under <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id the panel was created with.</param>
        /// <returns><see langword="true"/> when the panel is open.</returns>
        [ScriptModuleMethod(Description = "Gets whether a panel with this id is open.", ParameterCount = 1)]
        public bool Exists(string id) => Get(id) != null;

        /// <summary>
        /// Closes the panel open under <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id the panel was created with.</param>
        /// <returns><see langword="true"/> when a panel was closed.</returns>
        [ScriptModuleMethod(Description = "Closes the panel with this id. Returns false when none is open.", ParameterCount = 1)]
        public bool Close(string id)
        {
            var panel = Get(id);
            panel?.Close();
            return panel != null;
        }

        /// <summary>
        /// Closes every open panel.
        /// </summary>
        [ScriptModuleMethod(Description = "Closes every open panel.")]
        public void CloseAll() => Invoke(() =>
        {
            foreach (var panel in _panels.Values.ToArray())
            {
                panel.Close();
            }
        });

        private TerminalPanel Open(string id, string title, bool toolWindow, AnchorableShowStrategy strategy) => Invoke(() =>
        {
            id = NormalizeId(id);
            if (_panels.TryGetValue(id, out var existing))
            {
                existing.Title = title;
                existing.Activate();
                return existing;
            }

            var panel = new TerminalPanel(id);
            LayoutContent host = toolWindow
                ? _dock.AddToolWindow(panel, title, strategy)
                : _dock.Add(panel, title);
            host.ContentId = "panel:" + id;
            panel.AttachHost(host);
            panel.Closed += (_, _) => _panels.Remove(id);
            _panels[id] = panel;
            return panel;
        });

        private static string NormalizeId(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return id.Trim();
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
