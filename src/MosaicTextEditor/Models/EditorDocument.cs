/*
 * MosaicTextEditor
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf.Controls;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MosaicTextEditor.Models
{
    /// <summary>
    /// Represents one open editor document and the WPF editor control that hosts its text.
    /// </summary>
    public partial class EditorDocument : ObservableObject
    {
        private readonly MarkdownEditor? _markdownEditor;
        private readonly SyntaxEditor? _syntaxEditor;
        private bool _suppressModified;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorDocument"/> class.
        /// </summary>
        /// <param name="kind">The editor surface kind.</param>
        /// <param name="fileName">The initial display file name.</param>
        private EditorDocument(EditorDocumentKind kind, string fileName)
        {
            this.Kind = kind;
            this.FileName = fileName;

            if (kind == EditorDocumentKind.Markdown)
            {
                _markdownEditor = new MarkdownEditor
                {
                    FileName = fileName,
                    StatusBarVisibility = System.Windows.Visibility.Visible
                };
                DependencyPropertyDescriptor.FromProperty(MarkdownEditor.DocumentTitleProperty, typeof(MarkdownEditor))
                    ?.AddValueChanged(_markdownEditor, this.MarkdownEditor_OnDependencyPropertyChanged);
                DependencyPropertyDescriptor.FromProperty(MarkdownEditor.IsModifiedProperty, typeof(MarkdownEditor))
                    ?.AddValueChanged(_markdownEditor, this.MarkdownEditor_OnDependencyPropertyChanged);

                this.EditorControl = _markdownEditor;
            }
            else
            {
                _syntaxEditor = new SyntaxEditor
                {
                    Language = SyntaxLanguage.None
                };
                _syntaxEditor.TextChanged += this.SyntaxEditor_OnTextChanged;
                this.EditorControl = _syntaxEditor;
            }

            this.UpdateTitle();
        }

        /// <summary>
        /// Gets the editor surface kind.
        /// </summary>
        public EditorDocumentKind Kind { get; }

        /// <summary>
        /// Gets the WPF control hosted in AvalonDock.
        /// </summary>
        public Control EditorControl { get; }

        /// <summary>
        /// Gets the current syntax language for syntax editor documents.
        /// </summary>
        public SyntaxLanguage Language => _syntaxEditor?.Language ?? SyntaxLanguage.Markdown;

        /// <summary>
        /// Gets or sets the full path this document is saved to.
        /// </summary>
        [ObservableProperty]
        private string? _filePath;

        /// <summary>
        /// Gets or sets the display name used before the document has a file path.
        /// </summary>
        [ObservableProperty]
        private string _fileName = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this document has unsaved changes.
        /// </summary>
        [ObservableProperty]
        private bool _isModified;

        /// <summary>
        /// Gets the tab title, including an unsaved-change marker when needed.
        /// </summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>
        /// Creates a blank syntax editor document.
        /// </summary>
        public static EditorDocument CreateSyntax(string fileName) => new(EditorDocumentKind.Syntax, fileName);

        /// <summary>
        /// Creates a blank markdown editor document.
        /// </summary>
        public static EditorDocument CreateMarkdown(string fileName) => new(EditorDocumentKind.Markdown, fileName);

        /// <summary>
        /// Loads the specified file into the editor surface resolved for its type (e.g. markdown files
        /// open in the markdown editor); files without a specialized editor open in the syntax editor.
        /// </summary>
        /// <param name="path">The file path to load.</param>
        public static async Task<EditorDocument> LoadFileAsync(string path)
        {
            string text = await File.ReadAllTextAsync(path);
            var kind = EditorRegistry.ResolveKind(path);
            var document = new EditorDocument(kind, Path.GetFileName(path));
            document.SetText(text, markModified: false);
            document.SetFilePath(path);
            document.IsModified = false;
            return document;
        }

        /// <summary>
        /// Gets the current editor text.
        /// </summary>
        public string GetText()
        {
            if (_markdownEditor != null)
            {
                return _markdownEditor.Text;
            }

            return _syntaxEditor?.Text ?? string.Empty;
        }

        /// <summary>
        /// Sets the current editor text.
        /// </summary>
        /// <param name="text">The new text.</param>
        /// <param name="markModified">Whether to mark the document modified.</param>
        public void SetText(string text, bool markModified)
        {
            _suppressModified = true;
            try
            {
                if (_markdownEditor != null)
                {
                    _markdownEditor.Text = text;
                    _markdownEditor.IsModified = markModified;
                }
                else if (_syntaxEditor != null)
                {
                    _syntaxEditor.Text = text;
                }
            }
            finally
            {
                _suppressModified = false;
            }

            this.IsModified = markModified;
        }

        /// <summary>
        /// Saves the document to its current path.
        /// </summary>
        public async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(this.FilePath))
            {
                throw new InvalidOperationException("The document does not have a file path.");
            }

            await this.SaveToPathAsync(this.FilePath);
        }

        /// <summary>
        /// Saves the document to the specified path and updates document metadata after the write succeeds.
        /// </summary>
        /// <param name="path">The destination path.</param>
        public async Task SaveToPathAsync(string path)
        {
            string fullPath = Path.GetFullPath(path);
            await File.WriteAllTextAsync(fullPath, this.GetText());
            this.SetFilePath(fullPath);
            this.IsModified = false;

            if (_markdownEditor != null)
            {
                _markdownEditor.FilePath = this.FilePath;
                _markdownEditor.FileName = Path.GetFileName(this.FilePath);
                _markdownEditor.IsModified = false;
            }
        }

        /// <summary>
        /// Sets the saved file path and refreshes name and syntax metadata.
        /// </summary>
        /// <param name="path">The saved file path.</param>
        public void SetFilePath(string path)
        {
            this.FilePath = Path.GetFullPath(path);
            this.FileName = Path.GetFileName(path);

            if (_syntaxEditor != null)
            {
                _syntaxEditor.Language = SyntaxLanguageMap.FromExtension(path);
            }

            if (_markdownEditor != null)
            {
                _markdownEditor.FilePath = this.FilePath;
                _markdownEditor.FileName = this.FileName;
            }
        }

        /// <summary>
        /// Moves keyboard focus into the editor surface.
        /// </summary>
        public void FocusEditor()
        {
            if (_syntaxEditor != null)
            {
                _syntaxEditor.Focus();
                Keyboard.Focus(_syntaxEditor.TextArea);
                return;
            }

            if (FindVisualChild<SyntaxEditor>(this.EditorControl) is { } nestedEditor)
            {
                nestedEditor.Focus();
                Keyboard.Focus(nestedEditor.TextArea);
                return;
            }

            this.EditorControl.Focus();
            Keyboard.Focus(this.EditorControl);
        }

        partial void OnFilePathChanged(string? value) => this.UpdateTitle();

        partial void OnFileNameChanged(string value) => this.UpdateTitle();

        partial void OnIsModifiedChanged(bool value)
        {
            if (_markdownEditor != null && _markdownEditor.IsModified != value)
            {
                _markdownEditor.IsModified = value;
            }

            this.UpdateTitle();
        }

        private void SyntaxEditor_OnTextChanged(object? sender, EventArgs e)
        {
            if (!_suppressModified)
            {
                this.IsModified = true;
            }
        }

        private void MarkdownEditor_OnDependencyPropertyChanged(object? sender, EventArgs e) => this.SyncFromMarkdownEditor();

        private void SyncFromMarkdownEditor()
        {
            if (_markdownEditor == null)
            {
                return;
            }

            if (!_suppressModified)
            {
                this.IsModified = _markdownEditor.IsModified;
            }

            if (!string.IsNullOrWhiteSpace(_markdownEditor.FilePath))
            {
                this.FilePath = _markdownEditor.FilePath;
            }

            if (!string.IsNullOrWhiteSpace(_markdownEditor.FileName))
            {
                this.FileName = _markdownEditor.FileName;
            }

            this.UpdateTitle();
        }

        private void UpdateTitle()
        {
            string baseName = !string.IsNullOrWhiteSpace(this.FilePath)
                ? Path.GetFileName(this.FilePath)
                : this.FileName;

            this.Title = this.IsModified ? baseName + "*" : baseName;
        }

        private static T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                {
                    return match;
                }

                var nestedMatch = FindVisualChild<T>(child);
                if (nestedMatch != null)
                {
                    return nestedMatch;
                }
            }

            return null;
        }
    }
}
