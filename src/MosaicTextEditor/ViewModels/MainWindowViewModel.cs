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
using CommunityToolkit.Mvvm.Input;
using MosaicTextEditor.Common;
using MosaicTextEditor.Models;
using MosaicTextEditor.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace MosaicTextEditor.ViewModels
{
    /// <summary>
    /// Main editor shell view model.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private const int RecentLimit = 10;
        private int _untitledCount = 1;
        private int _untitledMarkdownCount = 1;
        private readonly AppSettings _appSettings;
        private readonly IEditorDialogService _dialogService;
        private readonly Dictionary<string, EditorDocument> _openDocumentsByPath = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel(AppSettings appSettings, IEditorDialogService dialogService)
        {
            _appSettings = appSettings;
            _dialogService = dialogService;
            this.CurrentFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            this.RecentFiles = appSettings.RecentFiles;
            this.RecentFolders = appSettings.RecentFolders;
        }

        /// <summary>
        /// Raised when a document should be added to AvalonDock.
        /// </summary>
        public event EventHandler<EditorDocument>? DocumentAdded;

        /// <summary>
        /// Raised when an existing document should be focused.
        /// </summary>
        public event EventHandler<EditorDocument>? DocumentFocusRequested;

        /// <summary>
        /// Raised when a tool window should be shown or focused.
        /// </summary>
        public event EventHandler<string>? ToolWindowRequested;

        /// <summary>
        /// Raised when the main window should close.
        /// </summary>
        public event EventHandler? ExitRequested;

        /// <summary>
        /// Gets the open editor documents.
        /// </summary>
        public ObservableCollection<EditorDocument> OpenDocuments { get; } = new();

        /// <summary>
        /// Gets the recent file list.
        /// </summary>
        public ObservableCollection<string> RecentFiles { get; }

        /// <summary>
        /// Gets the recent folder list.
        /// </summary>
        public ObservableCollection<string> RecentFolders { get; }

        /// <summary>
        /// Gets or sets the current file explorer folder.
        /// </summary>
        [ObservableProperty]
        private string _currentFolder;

        /// <summary>
        /// Gets or sets the active editor document.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveAsCommand))]
        private EditorDocument? _activeDocument;

        /// <summary>
        /// Gets or sets the shell status text.
        /// </summary>
        [ObservableProperty]
        private string _statusText = "Ready";

        /// <summary>
        /// Initializes startup document state.
        /// </summary>
        /// <param name="startupPath">Optional startup path.</param>
        public async Task InitializeAsync(string? startupPath)
        {
            if (!string.IsNullOrWhiteSpace(startupPath) && File.Exists(startupPath))
            {
                await this.OpenFilePathAsync(startupPath);
            }

            if (this.OpenDocuments.Count == 0)
            {
                this.NewFile();
            }
        }

        /// <summary>
        /// Removes a document that was closed by AvalonDock.
        /// </summary>
        /// <param name="document">The closed document.</param>
        public void RemoveDocument(EditorDocument document)
        {
            document.PropertyChanged -= this.EditorDocument_OnPropertyChanged;
            this.OpenDocuments.Remove(document);

            if (!string.IsNullOrWhiteSpace(document.FilePath))
            {
                _openDocumentsByPath.Remove(document.FilePath);
            }

            if (this.ActiveDocument == document)
            {
                this.ActiveDocument = this.OpenDocuments.LastOrDefault();
            }
        }

        /// <summary>
        /// Registers a document as active from AvalonDock focus changes.
        /// </summary>
        /// <param name="document">The active document.</param>
        public void SetActiveDocument(EditorDocument? document)
        {
            this.ActiveDocument = document;
        }

        [RelayCommand]
        private void NewFile()
        {
            var document = EditorDocument.CreateSyntax($"Untitled {_untitledCount++}");
            this.AddDocument(document);
        }

        [RelayCommand]
        private void NewMarkdownFile()
        {
            var document = EditorDocument.CreateMarkdown($"Untitled Markdown {_untitledMarkdownCount++}.md");
            this.AddDocument(document);
        }

        [RelayCommand]
        private async Task OpenFileAsync()
        {
            string? path = _dialogService.ShowOpenFileDialog(this.CurrentFolder);
            if (!string.IsNullOrWhiteSpace(path))
            {
                await this.OpenFilePathAsync(path);
            }
        }

        [RelayCommand]
        private Task OpenActivatedFileAsync(FileInfo? file)
        {
            return file == null ? Task.CompletedTask : this.OpenFilePathAsync(file.FullName);
        }

        [RelayCommand]
        private Task OpenRecentFileAsync(string? path)
        {
            return string.IsNullOrWhiteSpace(path) ? Task.CompletedTask : this.OpenFilePathAsync(path);
        }

        [RelayCommand]
        private void OpenFolder()
        {
            string? folder = _dialogService.ShowOpenFolderDialog(this.CurrentFolder);
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            this.CurrentFolder = folder;
            _appSettings.LastFolder = folder;
            this.AddRecentFolder(folder);
            this.StatusText = $"Opened folder: {folder}";
        }

        [RelayCommand]
        private void OpenRecentFolder(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return;
            }

            this.CurrentFolder = folder;
            _appSettings.LastFolder = folder;
            this.AddRecentFolder(folder);
            this.StatusText = $"Opened folder: {folder}";
        }

        [RelayCommand(CanExecute = nameof(HasActiveDocument))]
        private async Task SaveAsync()
        {
            if (this.ActiveDocument == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(this.ActiveDocument.FilePath))
            {
                await this.SaveDocumentWithPromptAsync(this.ActiveDocument);
                return;
            }

            await this.SaveDocumentToPathAsync(this.ActiveDocument, this.ActiveDocument.FilePath);
        }

        [RelayCommand(CanExecute = nameof(HasActiveDocument))]
        private async Task SaveAsAsync()
        {
            if (this.ActiveDocument == null)
            {
                return;
            }

            string initialDirectory = !string.IsNullOrWhiteSpace(this.ActiveDocument.FilePath)
                ? Path.GetDirectoryName(this.ActiveDocument.FilePath) ?? this.CurrentFolder
                : this.CurrentFolder;

            string? path = _dialogService.ShowSaveFileDialog(this.ActiveDocument, initialDirectory);
            if (!string.IsNullOrWhiteSpace(path))
            {
                await this.SaveDocumentToPathAsync(this.ActiveDocument, path);
            }
        }

        /// <summary>
        /// Saves the specified document and reports whether the save completed.
        /// </summary>
        /// <param name="document">The document to save.</param>
        public async Task<bool> SaveDocumentWithPromptAsync(EditorDocument document)
        {
            if (!string.IsNullOrWhiteSpace(document.FilePath))
            {
                return await this.SaveDocumentToPathAsync(document, document.FilePath);
            }

            string? path = _dialogService.ShowSaveFileDialog(document, this.CurrentFolder);
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return await this.SaveDocumentToPathAsync(document, path);
        }

        [RelayCommand]
        private void Exit() => this.ExitRequested?.Invoke(this, EventArgs.Empty);

        [RelayCommand]
        private void ViewFileExplorer() => this.ToolWindowRequested?.Invoke(this, "Files");

        [RelayCommand]
        private void ViewProperties() => this.ToolWindowRequested?.Invoke(this, "Properties");

        [RelayCommand]
        private void ViewSettings() => this.ToolWindowRequested?.Invoke(this, "Settings");

        [RelayCommand]
        private void ViewOutput() => this.ToolWindowRequested?.Invoke(this, "Output");

        [RelayCommand]
        private void ViewLogViewer() => this.ToolWindowRequested?.Invoke(this, "LogViewer");

        private bool HasActiveDocument() => this.ActiveDocument != null;

        private async Task OpenFilePathAsync(string path)
        {
            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                this.StatusText = $"Invalid file path: {path}";
                return;
            }

            if (_openDocumentsByPath.TryGetValue(fullPath, out var existing))
            {
                this.DocumentFocusRequested?.Invoke(this, existing);
                return;
            }

            if (!File.Exists(fullPath))
            {
                this.StatusText = $"File not found: {fullPath}";
                return;
            }

            try
            {
                var document = await EditorDocument.LoadFileAsync(fullPath);
                this.AddDocument(document);
                this.AddRecentFile(fullPath);
                this.StatusText = $"Opened file: {fullPath}";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show($"The file could not be opened.\r\n\r\n{ex.Message}", "Open File", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.StatusText = $"Could not open file: {fullPath}";
            }
        }

        private async Task<bool> SaveDocumentToPathAsync(EditorDocument document, string path)
        {
            string? oldPath = document.FilePath;

            try
            {
                await document.SaveToPathAsync(path);
                this.UpdateOpenPath(document, oldPath);
                this.AddRecentFile(document.FilePath);
                this.StatusText = $"Saved file: {document.FilePath}";
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                MessageBox.Show($"The file could not be saved.\r\n\r\n{ex.Message}", "Save File", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.StatusText = $"Could not save file: {path}";
                return false;
            }
        }

        private void AddDocument(EditorDocument document)
        {
            this.OpenDocuments.Add(document);
            document.PropertyChanged += this.EditorDocument_OnPropertyChanged;
            this.UpdateOpenPath(document, oldPath: null);
            this.ActiveDocument = document;
            this.DocumentAdded?.Invoke(this, document);
        }

        private void EditorDocument_OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is EditorDocument document && e.PropertyName == nameof(EditorDocument.FilePath))
            {
                this.RemoveOpenPathEntries(document);
                this.UpdateOpenPath(document, oldPath: null);
            }
        }

        private void UpdateOpenPath(EditorDocument document, string? oldPath)
        {
            if (!string.IsNullOrWhiteSpace(oldPath))
            {
                _openDocumentsByPath.Remove(oldPath);
            }

            if (!string.IsNullOrWhiteSpace(document.FilePath))
            {
                _openDocumentsByPath[document.FilePath] = document;
            }
        }

        private void RemoveOpenPathEntries(EditorDocument document)
        {
            foreach (var path in _openDocumentsByPath.Where(x => ReferenceEquals(x.Value, document)).Select(x => x.Key).ToArray())
            {
                _openDocumentsByPath.Remove(path);
            }
        }

        private void AddRecentFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            AddRecent(this.RecentFiles, path);
        }

        private void AddRecentFolder(string? folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            AddRecent(this.RecentFolders, folder);
        }

        private static void AddRecent(ObservableCollection<string> list, string value)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (string.Equals(list[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    list.RemoveAt(i);
                }
            }

            list.Insert(0, value);

            while (list.Count > RecentLimit)
            {
                list.RemoveAt(list.Count - 1);
            }
        }
    }
}
