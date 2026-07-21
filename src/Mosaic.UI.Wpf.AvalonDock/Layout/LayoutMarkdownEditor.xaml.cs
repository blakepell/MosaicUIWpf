/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Interfaces;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace Mosaic.UI.Wpf.AvalonDock.Layout
{
    /// <summary>
    /// Provides an AvalonDock document that hosts a <see cref="MarkdownEditor"/> and keeps its tab title,
    /// file metadata, and modified state synchronized with the editor.
    /// </summary>
    public partial class LayoutMarkdownEditor : LayoutDocument, ISaveable, ISupportInitialize
    {
        private bool _isInitializing;

        private static readonly DependencyPropertyDescriptor? DocumentTitleDescriptor =
            DependencyPropertyDescriptor.FromProperty(MarkdownEditor.DocumentTitleProperty, typeof(MarkdownEditor));

        private static readonly DependencyPropertyDescriptor? FilePathDescriptor =
            DependencyPropertyDescriptor.FromProperty(MarkdownEditor.FilePathProperty, typeof(MarkdownEditor));

        private static readonly DependencyPropertyDescriptor? FileNameDescriptor =
            DependencyPropertyDescriptor.FromProperty(MarkdownEditor.FileNameProperty, typeof(MarkdownEditor));

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutMarkdownEditor"/> class.
        /// </summary>
        public LayoutMarkdownEditor()
        {
            this.InitializeComponent();
            DocumentTitleDescriptor?.AddValueChanged(this.Editor, this.Editor_OnDocumentMetadataChanged);
            FilePathDescriptor?.AddValueChanged(this.Editor, this.Editor_OnDocumentMetadataChanged);
            FileNameDescriptor?.AddValueChanged(this.Editor, this.Editor_OnDocumentMetadataChanged);
            this.Editor.Saving += this.Editor_OnSaving;
            this.Closed += this.LayoutMarkdownEditor_OnClosed;
            this.UpdateDocumentMetadata();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutMarkdownEditor"/> class with a display file name.
        /// </summary>
        /// <param name="fileName">The display name used until the document has a saved path.</param>
        public LayoutMarkdownEditor(string fileName)
            : this()
        {
            this.FileName = fileName;
        }

        /// <summary>
        /// Gets or sets the markdown text.
        /// </summary>
        public string Text
        {
            get => this.Editor.Text;
            set => this.Editor.Text = value;
        }

        /// <inheritdoc/>
        public bool IsModified
        {
            get => this.Editor.IsModified;
            set => this.Editor.IsModified = value;
        }

        /// <inheritdoc/>
        public string? FilePath
        {
            get => this.Editor.FilePath;
            set => this.Editor.FilePath = value;
        }

        /// <summary>
        /// Gets or sets the display name used when the document has no file path.
        /// </summary>
        public string? FileName
        {
            get => this.Editor.FileName;
            set => this.Editor.FileName = value;
        }

        /// <summary>
        /// Raised before a save operation begins. Set <see cref="CancelEventArgs.Cancel"/> to cancel the save.
        /// </summary>
        public event EventHandler<CancelEventArgs>? Saving;

        /// <summary>
        /// Signals that the XAML loader is beginning to initialize the document.
        /// </summary>
        public void BeginInit() => _isInitializing = true;

        /// <summary>
        /// Signals that XAML initialization is complete and accepts the initial text as unmodified content.
        /// </summary>
        public void EndInit()
        {
            if (!_isInitializing)
            {
                return;
            }

            _isInitializing = false;
            this.Editor.IsModified = false;
            this.UpdateDocumentMetadata();
        }

        /// <summary>
        /// Loads a markdown file and clears the modified state.
        /// </summary>
        /// <param name="path">The file to load.</param>
        public void LoadFile(string path) => this.Editor.LoadFile(path);

        /// <summary>
        /// Resets the document to a new, empty markdown document.
        /// </summary>
        public void NewDocument() => this.Editor.NewDocument();

        /// <inheritdoc/>
        public void Save() => this.Editor.Save();

        /// <inheritdoc/>
        public Task SaveAsync() => this.Editor.SaveAsync();

        /// <inheritdoc/>
        public Task SaveAsAsync() => this.Editor.SaveAsAsync();

        private void Editor_OnSaving(object? sender, CancelEventArgs e) => this.Saving?.Invoke(this, e);

        private void Editor_OnDocumentMetadataChanged(object? sender, EventArgs e) => this.UpdateDocumentMetadata();

        private void UpdateDocumentMetadata()
        {
            this.Title = this.Editor.DocumentTitle;
            this.Description = this.FilePath ?? this.FileName ?? this.Editor.DocumentTitle.TrimEnd('*');

            if (!string.IsNullOrWhiteSpace(this.FilePath))
            {
                this.ContentId = Path.GetFullPath(this.FilePath);
            }
        }

        private void LayoutMarkdownEditor_OnClosed(object? sender, EventArgs e)
        {
            DocumentTitleDescriptor?.RemoveValueChanged(this.Editor, this.Editor_OnDocumentMetadataChanged);
            FilePathDescriptor?.RemoveValueChanged(this.Editor, this.Editor_OnDocumentMetadataChanged);
            FileNameDescriptor?.RemoveValueChanged(this.Editor, this.Editor_OnDocumentMetadataChanged);
            this.Editor.Saving -= this.Editor_OnSaving;
            this.Closed -= this.LayoutMarkdownEditor_OnClosed;
        }
    }
}
