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
using System.Windows;
using System.Windows.Threading;
using Mosaic.UI.Wpf.Controls.Scripting;
using Mosaic.UI.Wpf.Controls.Scripting.DataList;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Docks terminal info panels and other content, such as a <see cref="DataList"/>, by id. One
    /// instance lives for the life of the main window, so a panel created by one script run or script
    /// engine can be retrieved by any other.
    /// </summary>
    /// <remarks>
    /// Ids are trimmed and compared case-insensitively. A panel stays registered until its document
    /// or tool window is closed. Every member is safe to call from a script's worker thread.
    /// </remarks>
    [ScriptModule(Name = "panels", Description = "Docks terminal panels and other content (like a DataList) by id.")]
    public sealed class PanelScriptCommands
    {
        private const string ContentIdPrefix = "panel:";
        private readonly DockingManager _dock;
        private readonly Dictionary<string, PanelEntry> _panels = new(StringComparer.OrdinalIgnoreCase);

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
        /// Opens a terminal panel as a document tab, or returns the panel already open under <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id used to retrieve the panel later.</param>
        /// <param name="title">The tab title.</param>
        /// <returns>The panel.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="id"/> is open with content that is not a terminal panel.</exception>
        [ScriptModuleMethod(Description = "Opens a terminal panel as a document tab, or returns the open panel with this id.", ParameterCount = 2)]
        public TerminalPanel Create(string id, string title) => OpenTerminal(id, title, strategy: null);

        /// <summary>
        /// Opens a terminal panel as a tool window docked on the right, or returns the panel already open under <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id used to retrieve the panel later.</param>
        /// <param name="title">The tool window title.</param>
        /// <returns>The panel.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="id"/> is open with content that is not a terminal panel.</exception>
        [ScriptModuleMethod(Description = "Opens a terminal panel as a tool window on the right, or returns the open panel with this id.", ParameterCount = 2)]
        public TerminalPanel CreateTool(string id, string title) => OpenTerminal(id, title, AnchorableShowStrategy.Right);

        /// <summary>
        /// Opens a terminal panel as a tool window docked on a side, or returns the panel already open under <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id used to retrieve the panel later.</param>
        /// <param name="title">The tool window title.</param>
        /// <param name="side"><c>left</c>, <c>right</c>, <c>top</c>, or <c>bottom</c>.</param>
        /// <returns>The panel.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="id"/> is open with content that is not a terminal panel.</exception>
        [ScriptModuleMethod(Description = "Opens a terminal panel as a tool window on a side (left, right, top, bottom), or returns the open panel with this id.", ParameterCount = 3)]
        public TerminalPanel CreateTool(string id, string title, string side)
        {
            var strategy = ParseSide(side) ?? throw new ArgumentException("The side must be left, right, top, or bottom.", nameof(side));
            return OpenTerminal(id, title, strategy);
        }

        /// <summary>
        /// Docks <paramref name="content"/> in a tool window on the right. When a panel is already open
        /// under <paramref name="id"/> it keeps its place and selection, takes the new title, and shows
        /// <paramref name="content"/> in place of what it held. Keyboard focus is not moved, so this is
        /// safe to call from triggers; call <see cref="Show"/> to focus the panel.
        /// </summary>
        /// <param name="id">The id used to retrieve the panel later.</param>
        /// <param name="title">The tool window title.</param>
        /// <param name="content">A <see cref="DataList"/> or a WPF element created on the UI thread.</param>
        /// <returns><paramref name="content"/>, so a script can create and dock in one statement.</returns>
        /// <exception cref="ArgumentException"><paramref name="content"/> cannot be docked.</exception>
        [ScriptModuleMethod(Description = "Docks content (a DataList or a WPF element) in a tool window on the right without taking focus. Returns the content.",
            ParameterCount = 3, ReturnType = typeof(object))]
        public object Dock(string id, string title, object content) => DockContent(id, title, content, AnchorableShowStrategy.Right);

        /// <summary>
        /// Docks <paramref name="content"/> in a tool window on a side, or as a document tab. When a panel
        /// is already open under <paramref name="id"/> it keeps its place and selection, takes the new title, and shows
        /// <paramref name="content"/> in place of what it held. Keyboard focus is not moved, so this is
        /// safe to call from triggers; call <see cref="Show"/> to focus the panel.
        /// </summary>
        /// <param name="id">The id used to retrieve the panel later.</param>
        /// <param name="title">The tool window or tab title.</param>
        /// <param name="content">A <see cref="DataList"/> or a WPF element created on the UI thread.</param>
        /// <param name="side"><c>left</c>, <c>right</c>, <c>top</c>, <c>bottom</c>, or <c>document</c>.</param>
        /// <returns><paramref name="content"/>, so a script can create and dock in one statement.</returns>
        /// <exception cref="ArgumentException"><paramref name="content"/> cannot be docked, or <paramref name="side"/> is not recognized.</exception>
        [ScriptModuleMethod(Description = "Docks content on a side (left, right, top, bottom) or as a document without taking focus. Returns the content.",
            ParameterCount = 4, ReturnType = typeof(object))]
        public object Dock(string id, string title, object content, string side)
        {
            bool isDocument = string.Equals(side?.Trim(), "document", StringComparison.OrdinalIgnoreCase);
            var strategy = ParseSide(side);
            if (strategy == null && !isDocument)
            {
                throw new ArgumentException("The side must be left, right, top, bottom, or document.", nameof(side));
            }

            return DockContent(id, title, content, strategy);
        }

        /// <summary>
        /// Gets an open terminal panel.
        /// </summary>
        /// <param name="id">The id the panel was created with.</param>
        /// <returns>The panel, or <see langword="null"/> when no terminal panel is open under <paramref name="id"/>.</returns>
        [ScriptModuleMethod(Description = "Gets the open terminal panel with this id, or null.", ParameterCount = 1)]
        public TerminalPanel? Get(string id) => GetContent(id) as TerminalPanel;

        /// <summary>
        /// Gets the content of an open panel: the object passed to <see cref="Dock(string, string, object)"/>,
        /// or the terminal panel.
        /// </summary>
        /// <param name="id">The id the panel was created with.</param>
        /// <returns>The content, or <see langword="null"/> when no panel is open under <paramref name="id"/>.</returns>
        [ScriptModuleMethod(Description = "Gets the content of the open panel with this id (eg: the DataList), or null.",
            ParameterCount = 1, ReturnType = typeof(object))]
        public object? GetContent(string id) => Invoke(() => _panels.GetValueOrDefault(NormalizeId(id))?.Content);

        /// <summary>
        /// Gets whether a panel is open under <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id the panel was created with.</param>
        /// <returns><see langword="true"/> when the panel is open.</returns>
        [ScriptModuleMethod(Description = "Gets whether a panel with this id is open.", ParameterCount = 1)]
        public bool Exists(string id) => GetContent(id) != null;

        /// <summary>
        /// Brings the panel open under <paramref name="id"/> forward and gives it focus.
        /// </summary>
        /// <param name="id">The id the panel was created with.</param>
        /// <returns><see langword="true"/> when a panel was shown.</returns>
        [ScriptModuleMethod(Description = "Brings the panel with this id forward and focuses it. Returns false when none is open.", ParameterCount = 1)]
        public bool Show(string id) => Invoke(() =>
            _panels.TryGetValue(NormalizeId(id), out var entry) && _dock.ShowContent(entry.Host.ContentId));

        /// <summary>
        /// Closes the panel open under <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id the panel was created with.</param>
        /// <returns><see langword="true"/> when a panel was closed.</returns>
        [ScriptModuleMethod(Description = "Closes the panel with this id. Returns false when none is open.", ParameterCount = 1)]
        public bool Close(string id) => Invoke(() =>
        {
            if (!_panels.TryGetValue(NormalizeId(id), out var entry))
            {
                return false;
            }

            entry.Host.Close();
            return true;
        });

        /// <summary>
        /// Closes every open panel.
        /// </summary>
        [ScriptModuleMethod(Description = "Closes every open panel.")]
        public void CloseAll() => Invoke(() =>
        {
            foreach (var entry in _panels.Values.ToArray())
            {
                entry.Host.Close();
            }
        });

        private TerminalPanel OpenTerminal(string id, string title, AnchorableShowStrategy? strategy) => Invoke(() =>
        {
            id = NormalizeId(id);
            if (_panels.TryGetValue(id, out var existing))
            {
                if (existing.Content is not TerminalPanel existingPanel)
                {
                    throw new InvalidOperationException($"The panel '{id}' holds a {existing.Content.GetType().Name}, not a terminal panel.");
                }

                existingPanel.Title = title;
                existingPanel.Activate();
                return existingPanel;
            }

            var panel = new TerminalPanel(id);
            var host = Register(id, title, panel, panel, strategy, activate: true);
            panel.AttachHost(host);
            return panel;
        });

        private object DockContent(string id, string title, object content, AnchorableShowStrategy? strategy)
        {
            ArgumentNullException.ThrowIfNull(content);
            return Invoke(() =>
            {
                id = NormalizeId(id);
                if (!_panels.TryGetValue(id, out var existing))
                {
                    var element = ToElement(content);
                    var host = Register(id, title, content, element, strategy, activate: false);
                    (content as TerminalPanel)?.AttachHost(host);
                    return content;
                }

                existing.Host.Title = title;
                if (!IsShowing(existing, content))
                {
                    existing.Host.Content = ToElement(content);
                    existing.Content = content;
                    (content as TerminalPanel)?.AttachHost(existing.Host);
                }

                return content;
            });
        }

        private LayoutContent Register(string id, string title, object content, FrameworkElement element, AnchorableShowStrategy? strategy, bool activate)
        {
            string contentId = ContentIdPrefix + id;
            LayoutContent host = strategy is { } side
                ? _dock.GetOrAddToolWindow(contentId, () => element, title, side, activate: activate)
                : _dock.GetOrAdd(contentId, () => element, title, activate: activate);

            var entry = new PanelEntry(host, content);
            host.Closed += (_, _) =>
            {
                if (_panels.TryGetValue(id, out var current) && ReferenceEquals(current, entry))
                {
                    _panels.Remove(id);
                }
            };
            _panels[id] = entry;
            return host;
        }

        /// <summary>
        /// Gets whether <paramref name="entry"/> already shows <paramref name="content"/>. A <see cref="DataList"/>
        /// shown in its own window since it was docked needs to be moved back.
        /// </summary>
        private static bool IsShowing(PanelEntry entry, object content)
        {
            if (!ReferenceEquals(entry.Content, content))
            {
                return false;
            }

            return content is not DataList list || ReferenceEquals(list.ListView.Parent, entry.Host.Content);
        }

        private FrameworkElement ToElement(object content) => content switch
        {
            DataList list => list.CreateHostElement(),
            FrameworkElement element when element.Dispatcher == Dispatcher => element,
            FrameworkElement => throw new ArgumentException("The element was created on another thread. Create it on the UI thread, or dock a DataList.", nameof(content)),
            _ => throw new ArgumentException($"A {content.GetType().Name} cannot be docked. Dock a DataList or a WPF element.", nameof(content))
        };

        private static AnchorableShowStrategy? ParseSide(string? side) => side?.Trim().ToLowerInvariant() switch
        {
            "left" => AnchorableShowStrategy.Left,
            "right" => AnchorableShowStrategy.Right,
            "top" => AnchorableShowStrategy.Top,
            "bottom" => AnchorableShowStrategy.Bottom,
            _ => null
        };

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

        /// <summary>
        /// An open panel: the layout item that hosts it and the object a script docked into it.
        /// </summary>
        private sealed class PanelEntry(LayoutContent host, object content)
        {
            public LayoutContent Host { get; } = host;

            public object Content { get; set; } = content;
        }
    }
}
