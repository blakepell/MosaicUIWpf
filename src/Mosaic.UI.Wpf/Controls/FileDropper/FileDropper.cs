/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Collections.Specialized;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Identifies the current drag-and-drop validation state of a <see cref="FileDropper"/>.
    /// </summary>
    public enum FileDropState
    {
        /// <summary>
        /// No drag operation is in progress over the control.
        /// </summary>
        None,

        /// <summary>
        /// A drag operation is in progress and the dragged files are accepted.
        /// </summary>
        Valid,

        /// <summary>
        /// A drag operation is in progress and one or more dragged files are not accepted.
        /// </summary>
        Invalid
    }

    /// <summary>
    /// A drop target that accepts files dragged from the operating system. Displays a prompt,
    /// an upload icon, and the list of accepted file types. The border turns green while valid
    /// files are dragged over it and red while invalid files are dragged over it. When files are
    /// dropped, the <see cref="FileDrop"/> event is raised (and the optional <see cref="FileDropCommand"/> executed).
    /// </summary>
    [DefaultEvent(nameof(FileDrop))]
    [DefaultProperty(nameof(AcceptedFileTypes))]
    [TemplatePart(Name = PartBorder, Type = typeof(Border))]
    public class FileDropper : Control
    {
        private const string PartBorder = "PART_Border";

        /// <summary>
        /// Initializes static metadata for the <see cref="FileDropper"/> class.
        /// </summary>
        static FileDropper()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FileDropper), new FrameworkPropertyMetadata(typeof(FileDropper)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDropper"/> class.
        /// </summary>
        public FileDropper()
        {
            this.AllowDrop = true;

            // Start with an empty collection instance so XAML property-element content and
            // code-behind Add() calls populate this list directly (rather than appending to a
            // pre-seeded "*.*"). An empty list is treated as "accept all" and displays as "*.*".
            this.SetCurrentValue(AcceptedFileTypesProperty, new ObservableCollection<string>());
        }

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Prompt"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PromptProperty = DependencyProperty.Register(
            nameof(Prompt), typeof(string), typeof(FileDropper), new FrameworkPropertyMetadata("Drop files here"));

        /// <summary>
        /// Gets or sets the prompt text shown on the top line of the control.
        /// </summary>
        [Category("Common")]
        [Description("The prompt text shown on the top line of the control.")]
        public string Prompt
        {
            get => (string)GetValue(PromptProperty);
            set => SetValue(PromptProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AcceptedFileTypes"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AcceptedFileTypesProperty = DependencyProperty.Register(
            nameof(AcceptedFileTypes), typeof(ObservableCollection<string>), typeof(FileDropper),
            new FrameworkPropertyMetadata(null, OnAcceptedFileTypesChanged));

        /// <summary>
        /// Gets or sets the collection of accepted file-type patterns (for example <c>*.png</c>, <c>*.txt</c>).
        /// Defaults to an empty collection, which accepts all files and displays as <c>*.*</c>. Add one or
        /// more patterns to restrict the accepted file types.
        /// </summary>
        [Category("Common")]
        [Description("The collection of accepted file-type patterns (for example *.png, *.txt). Defaults to *.* (all files).")]
        public ObservableCollection<string> AcceptedFileTypes
        {
            get => (ObservableCollection<string>)GetValue(AcceptedFileTypesProperty);
            set => SetValue(AcceptedFileTypesProperty, value);
        }

        private static readonly DependencyPropertyKey AcceptedFileTypesDisplayPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(AcceptedFileTypesDisplay), typeof(string), typeof(FileDropper), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="AcceptedFileTypesDisplay"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AcceptedFileTypesDisplayProperty = AcceptedFileTypesDisplayPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a comma-separated string of the accepted file-type patterns, suitable for display on the bottom line of the control.
        /// </summary>
        public string AcceptedFileTypesDisplay
        {
            get => (string)GetValue(AcceptedFileTypesDisplayProperty);
            private set => SetValue(AcceptedFileTypesDisplayPropertyKey, value);
        }

        private static readonly DependencyPropertyKey DragStatePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(DragState), typeof(FileDropState), typeof(FileDropper), new FrameworkPropertyMetadata(FileDropState.None));

        /// <summary>
        /// Identifies the <see cref="DragState"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DragStateProperty = DragStatePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the current drag-and-drop validation state. Used by the control template to color the border.
        /// </summary>
        public FileDropState DragState
        {
            get => (FileDropState)GetValue(DragStateProperty);
            private set => SetValue(DragStatePropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(FileDropper), new FrameworkPropertyMetadata(new CornerRadius(6)));

        /// <summary>
        /// Gets or sets the radius of the corners of the drop target border.
        /// </summary>
        [Category("Appearance")]
        [Description("The radius of the corners of the drop target border.")]
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FileDropCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FileDropCommandProperty = DependencyProperty.Register(
            nameof(FileDropCommand), typeof(ICommand), typeof(FileDropper), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets a command that is executed when files are dropped. The command parameter is the
        /// array of dropped file paths (<c>string[]</c>).
        /// </summary>
        [Category("Common")]
        [Description("A command executed when files are dropped. The command parameter is the array of dropped file paths.")]
        public ICommand? FileDropCommand
        {
            get => (ICommand?)GetValue(FileDropCommandProperty);
            set => SetValue(FileDropCommandProperty, value);
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="FileDrop"/> routed event.
        /// </summary>
        public static readonly RoutedEvent FileDropEvent = EventManager.RegisterRoutedEvent(
            nameof(FileDrop), RoutingStrategy.Bubble, typeof(FileDropEventHandler), typeof(FileDropper));

        /// <summary>
        /// Occurs when one or more files are dropped onto the control.
        /// </summary>
        public event FileDropEventHandler FileDrop
        {
            add => AddHandler(FileDropEvent, value);
            remove => RemoveHandler(FileDropEvent, value);
        }

        #endregion

        #region Drag and Drop

        /// <inheritdoc />
        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            this.UpdateDragState(e);
        }

        /// <inheritdoc />
        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            this.UpdateDragState(e);
        }

        /// <inheritdoc />
        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);
            this.DragState = FileDropState.None;
        }

        /// <inheritdoc />
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            this.DragState = FileDropState.None;

            var files = GetFiles(e);

            if (files.Length == 0 || !this.AreFilesAccepted(files))
            {
                return;
            }

            e.Handled = true;

            this.RaiseEvent(new FileDropEventArgs(FileDropEvent, this, files));

            if (this.FileDropCommand?.CanExecute(files) == true)
            {
                this.FileDropCommand.Execute(files);
            }
        }

        /// <summary>
        /// Evaluates the dragged payload and updates <see cref="DragState"/> and the drag effect accordingly.
        /// </summary>
        private void UpdateDragState(DragEventArgs e)
        {
            var files = GetFiles(e);

            if (files.Length > 0 && this.AreFilesAccepted(files))
            {
                this.DragState = FileDropState.Valid;
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                this.DragState = FileDropState.Invalid;
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Extracts the list of file paths from a drag payload, returning an empty array when the payload is not a file drop.
        /// </summary>
        private static string[] GetFiles(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                return files;
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Determines whether every file in the payload matches one of the accepted file-type patterns.
        /// </summary>
        private bool AreFilesAccepted(IReadOnlyList<string> files)
        {
            var patterns = this.AcceptedFileTypes;

            // No patterns configured means everything is accepted.
            if (patterns == null || patterns.Count == 0)
            {
                return true;
            }

            foreach (var file in files)
            {
                if (!patterns.Any(pattern => MatchesPattern(file, pattern)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tests a single file path against a single wildcard pattern (for example <c>*.*</c>, <c>*.png</c>, or <c>.png</c>).
        /// </summary>
        private static bool MatchesPattern(string filePath, string? pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            pattern = pattern.Trim();

            // Common "accept everything" patterns.
            if (pattern == "*" || pattern == "*.*")
            {
                return true;
            }

            string fileName = Path.GetFileName(filePath);

            // Allow a bare extension such as ".png" or "png".
            if (!pattern.Contains('*') && !pattern.Contains('?'))
            {
                string ext = pattern.StartsWith('.') ? pattern : "." + pattern;
                return fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
            }

            string regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(fileName, regex, RegexOptions.IgnoreCase);
        }

        #endregion

        #region Accepted File Types Maintenance

        private static void OnAcceptedFileTypesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dropper = (FileDropper)d;

            if (e.OldValue is ObservableCollection<string> oldCollection)
            {
                oldCollection.CollectionChanged -= dropper.OnAcceptedFileTypesCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<string> newCollection)
            {
                newCollection.CollectionChanged += dropper.OnAcceptedFileTypesCollectionChanged;
            }

            dropper.UpdateAcceptedFileTypesDisplay();
        }

        private void OnAcceptedFileTypesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateAcceptedFileTypesDisplay();
        }

        /// <summary>
        /// Rebuilds the comma-separated <see cref="AcceptedFileTypesDisplay"/> string from the current patterns.
        /// </summary>
        private void UpdateAcceptedFileTypesDisplay()
        {
            var patterns = this.AcceptedFileTypes;

            this.AcceptedFileTypesDisplay = patterns == null || patterns.Count == 0
                ? "*.*"
                : string.Join(", ", patterns);
        }

        #endregion
    }
}
