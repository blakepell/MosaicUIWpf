/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Win32;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Mosaic.UI.Wpf.AvalonDock.Layout
{
    /// <summary>
    /// Provides an AvalonDock document that hosts a <see cref="SyntaxEditor"/> with save support, modified-title
    /// tracking, and a toolbar that can be extended by consumers.
    /// </summary>
    public partial class LayoutSyntaxEditor : LayoutDocument, ISaveable, ISupportInitialize
    {
        private const string DefaultTitle = "Untitled";
        private bool _isInitializing;
        private bool _suppressModified;

        /// <summary>
        /// Identifies the <see cref="IsModified"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register(
            nameof(IsModified),
            typeof(bool),
            typeof(LayoutSyntaxEditor),
            new FrameworkPropertyMetadata(false, OnDocumentMetadataChanged));

        /// <summary>
        /// Identifies the <see cref="FilePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(
            nameof(FilePath),
            typeof(string),
            typeof(LayoutSyntaxEditor),
            new FrameworkPropertyMetadata(null, OnFilePathChanged));

        /// <summary>
        /// Identifies the <see cref="FileName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(
            nameof(FileName),
            typeof(string),
            typeof(LayoutSyntaxEditor),
            new FrameworkPropertyMetadata(null, OnDocumentMetadataChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutSyntaxEditor"/> class.
        /// </summary>
        public LayoutSyntaxEditor()
        {
            this.AdditionalToolBarItems = new ObservableCollection<object>();
            this.AdditionalToolBarItems.CollectionChanged += this.AdditionalToolBarItems_OnCollectionChanged;

            this.InitializeComponent();
            this.Editor.TextChanged += this.Editor_OnTextChanged;
            this.Editor.PreviewKeyDown += this.Editor_OnPreviewKeyDown;
            this.Closed += this.LayoutSyntaxEditor_OnClosed;
            this.UpdateDocumentMetadata();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutSyntaxEditor"/> class with a display file name.
        /// </summary>
        /// <param name="fileName">The display name used until the document has a saved path.</param>
        public LayoutSyntaxEditor(string fileName)
            : this()
        {
            this.FileName = fileName;
        }

        /// <summary>
        /// Gets the items supplied by the consumer that are merged into the toolbar after the save button.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ObservableCollection<object> AdditionalToolBarItems { get; }

        /// <summary>
        /// Gets or sets the editor text.
        /// </summary>
        public string Text
        {
            get => this.Editor.Text;
            set => this.Editor.Text = value;
        }

        /// <inheritdoc/>
        public bool IsModified
        {
            get => (bool)this.GetValue(IsModifiedProperty);
            set => this.SetValue(IsModifiedProperty, value);
        }

        /// <inheritdoc/>
        public string? FilePath
        {
            get => (string?)this.GetValue(FilePathProperty);
            set => this.SetValue(FilePathProperty, value);
        }

        /// <summary>
        /// Gets or sets the display name used when the document has no file path.
        /// </summary>
        public string? FileName
        {
            get => (string?)this.GetValue(FileNameProperty);
            set => this.SetValue(FileNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the file filter used by the Save As dialog.
        /// </summary>
        /// <value>The dialog filter. The default is <c>All Files (*.*)|*.*</c>.</value>
        public string SaveFileFilter { get; set; } = "All Files (*.*)|*.*";

        /// <summary>
        /// Raised before a save operation begins. Set <see cref="CancelEventArgs.Cancel"/> to cancel the save.
        /// </summary>
        public event EventHandler<CancelEventArgs>? Saving;

        /// <summary>
        /// Signals that the XAML loader is beginning to initialize the document.
        /// </summary>
        public void BeginInit()
        {
            _isInitializing = true;
            _suppressModified = true;
        }

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
            _suppressModified = false;
            this.IsModified = false;
            this.UpdateDocumentMetadata();
        }

        /// <summary>
        /// Loads a text file, selects syntax highlighting from its extension, and clears the modified state.
        /// </summary>
        /// <param name="path">The file to load.</param>
        public void LoadFile(string path)
        {
            string fullPath = Path.GetFullPath(path);
            _suppressModified = true;
            try
            {
                this.Editor.Text = File.ReadAllText(fullPath);
            }
            finally
            {
                _suppressModified = false;
            }

            this.FilePath = fullPath;
            this.FileName = Path.GetFileName(fullPath);
            this.IsModified = false;
        }

        /// <summary>
        /// Resets the document to a new, empty text document.
        /// </summary>
        /// <param name="fileName">An optional display file name.</param>
        public void NewDocument(string? fileName = null)
        {
            _suppressModified = true;
            try
            {
                this.Editor.Text = string.Empty;
            }
            finally
            {
                _suppressModified = false;
            }

            this.FilePath = null;
            this.FileName = fileName;
            this.Editor.Language = SyntaxLanguageMap.FromExtension(fileName);
            this.IsModified = false;
            this.Editor.Focus();
        }

        /// <inheritdoc/>
        public void Save()
        {
            if (string.IsNullOrWhiteSpace(this.FilePath) || this.RaiseSavingCancelled())
            {
                return;
            }

            File.WriteAllText(this.FilePath, this.Editor.Text);
            this.IsModified = false;
        }

        /// <inheritdoc/>
        public async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(this.FilePath))
            {
                await this.SaveAsAsync();
                return;
            }

            if (this.RaiseSavingCancelled())
            {
                return;
            }

            await File.WriteAllTextAsync(this.FilePath, this.Editor.Text);
            this.IsModified = false;
        }

        /// <inheritdoc/>
        public async Task SaveAsAsync()
        {
            if (this.RaiseSavingCancelled())
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = this.SaveFileFilter,
                FileName = this.FileName ?? string.Empty
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            string fullPath = Path.GetFullPath(dialog.FileName);
            await File.WriteAllTextAsync(fullPath, this.Editor.Text);
            this.FilePath = fullPath;
            this.FileName = Path.GetFileName(fullPath);
            this.IsModified = false;
        }

        private static void OnDocumentMetadataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((LayoutSyntaxEditor)d).UpdateDocumentMetadata();

        private static void OnFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var document = (LayoutSyntaxEditor)d;
            if (e.NewValue is string path && !string.IsNullOrWhiteSpace(path))
            {
                document.Editor.Language = SyntaxLanguageMap.FromExtension(path);
            }

            document.UpdateDocumentMetadata();
        }

        private bool RaiseSavingCancelled()
        {
            var args = new CancelEventArgs();
            this.Saving?.Invoke(this, args);
            return args.Cancel;
        }

        private void Editor_OnTextChanged(object? sender, EventArgs e)
        {
            if (!_suppressModified)
            {
                this.IsModified = true;
            }
        }

        private async void Editor_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.S || (Keyboard.Modifiers & ModifierKeys.Control) == 0)
            {
                return;
            }

            e.Handled = true;
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                await this.SaveAsAsync();
            }
            else
            {
                await this.SaveAsync();
            }
        }

        private async void SaveButton_OnClick(object sender, RoutedEventArgs e) => await this.SaveAsync();

        private void AdditionalToolBarItems_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            while (this.ToolBar.Items.Count > 2)
            {
                this.ToolBar.Items.RemoveAt(2);
            }

            foreach (object item in this.AdditionalToolBarItems)
            {
                this.ToolBar.Items.Add(item);
            }

            this.AdditionalItemsSeparator.Visibility = this.AdditionalToolBarItems.Count == 0
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void UpdateDocumentMetadata()
        {
            string baseTitle = !string.IsNullOrWhiteSpace(this.FilePath)
                ? Path.GetFileName(this.FilePath)
                : !string.IsNullOrWhiteSpace(this.FileName) ? this.FileName : DefaultTitle;

            this.Title = this.IsModified ? baseTitle + "*" : baseTitle;
            this.Description = this.FilePath ?? this.FileName ?? baseTitle;
            this.SaveButton.IsEnabled = this.IsModified;

            if (!string.IsNullOrWhiteSpace(this.FilePath))
            {
                this.ContentId = Path.GetFullPath(this.FilePath);
            }
        }

        private void LayoutSyntaxEditor_OnClosed(object? sender, EventArgs e)
        {
            this.AdditionalToolBarItems.CollectionChanged -= this.AdditionalToolBarItems_OnCollectionChanged;
            this.Editor.TextChanged -= this.Editor_OnTextChanged;
            this.Editor.PreviewKeyDown -= this.Editor_OnPreviewKeyDown;
            this.Closed -= this.LayoutSyntaxEditor_OnClosed;
        }
    }
}
