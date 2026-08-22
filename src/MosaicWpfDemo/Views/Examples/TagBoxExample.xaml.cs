/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System;
using System.Collections.ObjectModel;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class TagBoxExample
    {
        /// <summary>
        /// The tags bound to the <see cref="TagBox"/>. New tags are appended here automatically.
        /// </summary>
        public ObservableCollection<string> Tags { get; } = new()
        {
            "WPF", "C#", "Tailwind", "Mosaic"
        };

        /// <summary>
        /// The tags bound to the auto-complete enabled <see cref="TagBox"/>.
        /// </summary>
        public ObservableCollection<string> AutoCompleteTags { get; } = new()
        {
            "WPF"
        };

        /// <summary>
        /// The pool of candidate tags offered by the auto-complete drop-down. Because it is an
        /// <see cref="ObservableCollection{T}"/>, adding to it at runtime immediately affects the suggestions.
        /// </summary>
        public ObservableCollection<string> SuggestedTags { get; } = new()
        {
            "WPF", "WinForms", "WinUI", "MAUI", "Avalonia", "Blazor",
            "C#", "F#", "VB.NET", "TypeScript", "SQL",
            "Mosaic", "MVVM", "XAML", "Styles", "Templates", "Theming",
            "SQLite", "Serialization", "Sockets", "Async", "Performance"
        };

        /// <summary>
        /// The filter modes offered by the demo's matching-mode selector.
        /// </summary>
        public AutoCompleteBoxFilterMode[] FilterModes { get; } =
        {
            AutoCompleteBoxFilterMode.Contains,
            AutoCompleteBoxFilterMode.StartsWith
        };

        /// <summary>
        /// A running log of the TagChanging / TagChanged events for demonstration purposes.
        /// </summary>
        public ObservableCollection<string> EventLog { get; } = new();

        public TagBoxExample()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        /// <summary>
        /// Fires before a tag is added or removed. Demonstrates vetoing a change by setting <c>Cancel</c>.
        /// </summary>
        private void DemoTagBox_TagChanging(object? sender, TagChangingEventArgs e)
        {
            if (e.Action == TagChangeAction.Add && string.Equals(e.Tag, "blocked", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                this.Log($"TagChanging: '{e.Tag}' ({e.Action}) was VETOED.");
                return;
            }

            this.Log($"TagChanging: '{e.Tag}' ({e.Action}).");
        }

        /// <summary>
        /// Fires after a tag has been added or removed.
        /// </summary>
        private void DemoTagBox_TagChanged(object? sender, TagChangedEventArgs e)
        {
            this.Log($"TagChanged:  '{e.Tag}' ({e.Action}). Count = {this.Tags.Count}");
        }

        private void Log(string message)
        {
            this.EventLog.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        }
    }
}
