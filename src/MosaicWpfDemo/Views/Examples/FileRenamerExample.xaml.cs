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
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// Demonstrates combining a <see cref="FileDropper"/> with a repeater-style list of configurable
    /// rename operations. Files are dropped, a sequence of operations is built, a live preview shows the
    /// resulting names, and "Execute Renames" performs the rename on disk.
    /// </summary>
    public partial class FileRenamerExample
    {
        /// <summary>
        /// The files that have been dropped onto the <see cref="FileDropper"/> and are queued for renaming.
        /// </summary>
        public ObservableCollection<DroppedFile> DroppedFiles { get; } = new();

        /// <summary>
        /// The ordered list of rename operations applied to each dropped file.
        /// </summary>
        public ObservableCollection<RenameOperation> Operations { get; } = new();

        /// <summary>
        /// The computed before/after preview, refreshed whenever the files or operations change.
        /// </summary>
        public ObservableCollection<PreviewItem> PreviewItems { get; } = new();

        /// <summary>
        /// The set of operation types offered in each row's dropdown.
        /// </summary>
        public IReadOnlyList<RenameOperationOption> OperationOptions { get; } = RenameOperationOption.All;

        public FileRenamerExample()
        {
            InitializeComponent();
            this.DataContext = this;

            // Keep the preview in sync as the user edits operations or drops/clears files.
            this.Operations.CollectionChanged += OnOperationsCollectionChanged;
            this.DroppedFiles.CollectionChanged += (_, _) => this.RefreshPreview();

            // Seed one operation so the repeater isn't empty on first view.
            this.Operations.Add(new RenameOperation());
        }

        /// <summary>
        /// Handles files dropped onto the <see cref="FileDropper"/>. Works for a single file or many.
        /// </summary>
        private void FileDropper_OnFileDrop(object sender, FileDropEventArgs e)
        {
            foreach (string path in e.Files)
            {
                // Only individual files are renamed here; ignore dropped directories.
                if (!File.Exists(path))
                {
                    continue;
                }

                // Avoid adding the same file twice.
                if (this.DroppedFiles.Any(f => string.Equals(f.FullPath, path, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                this.DroppedFiles.Add(new DroppedFile(path));
            }
        }

        private void ClearFiles_OnClick(object sender, RoutedEventArgs e)
        {
            this.DroppedFiles.Clear();
        }

        private void AddOperation_OnClick(object sender, RoutedEventArgs e)
        {
            this.Operations.Add(new RenameOperation());
        }

        private void RemoveOperation_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: RenameOperation op })
            {
                this.Operations.Remove(op);
            }
        }

        /// <summary>
        /// Validates input, applies the rename operations to every dropped file, and renames the files on disk.
        /// </summary>
        private void ExecuteRenames_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.DroppedFiles.Count == 0)
            {
                ShowError("Drop one or more files before executing renames.");
                return;
            }

            if (this.Operations.Count == 0)
            {
                ShowError("Add at least one rename operation before executing.");
                return;
            }

            // Validate required fields up front so we don't rename half the batch before discovering a problem.
            var validationErrors = new List<string>();
            for (int i = 0; i < this.Operations.Count; i++)
            {
                string? error = this.Operations[i].Validate();
                if (error != null)
                {
                    validationErrors.Add($"Operation {i + 1}: {error}");
                }
            }

            if (validationErrors.Count > 0)
            {
                ShowError("Please fix the following before executing:\n\n" + string.Join("\n", validationErrors));
                return;
            }

            int renamed = 0;
            var errors = new List<string>();

            // Snapshot the list so updates to DroppedFiles during the loop don't disturb iteration.
            foreach (var file in this.DroppedFiles.ToList())
            {
                string originalPath = file.FullPath;

                if (!File.Exists(originalPath))
                {
                    errors.Add($"{file.FileName}: the source file no longer exists.");
                    continue;
                }

                string newFileName;
                try
                {
                    newFileName = FileRenamer.Apply(file.FileName, this.Operations);
                }
                catch (RenameException ex)
                {
                    errors.Add($"{file.FileName}: {ex.Message}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(newFileName))
                {
                    errors.Add($"{file.FileName}: the operations produced an empty file name.");
                    continue;
                }

                string directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
                string newPath = Path.Combine(directory, newFileName);

                // Nothing to do if the name is unchanged.
                if (string.Equals(originalPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Collision handling: never overwrite an existing file.
                if (File.Exists(newPath))
                {
                    errors.Add($"{file.FileName}: destination already exists, skipped ({newPath}).");
                    continue;
                }

                try
                {
                    File.Move(originalPath, newPath);

                    // Point the dropped file at its new location so a follow-up execute works on the renamed file.
                    file.UpdatePath(newPath);
                    renamed++;
                }
                catch (UnauthorizedAccessException)
                {
                    errors.Add($"{file.FileName}: access denied (the file may be read-only or in use).");
                }
                catch (IOException ex)
                {
                    errors.Add($"{file.FileName}: {ex.Message}");
                }
            }

            this.RefreshPreview();

            // Report the outcome.
            if (errors.Count == 0)
            {
                MessageBox.Show(
                    $"Successfully renamed {renamed} file{(renamed == 1 ? string.Empty : "s")}.",
                    "Rename Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Renamed {renamed} file{(renamed == 1 ? string.Empty : "s")}. {errors.Count} issue(s) occurred:");
                sb.AppendLine();
                sb.Append(string.Join("\n", errors));

                MessageBox.Show(sb.ToString(), "Rename Finished With Errors", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnOperationsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Subscribe/unsubscribe so live edits to any operation refresh the preview.
            if (e.OldItems != null)
            {
                foreach (RenameOperation op in e.OldItems)
                {
                    op.PropertyChanged -= OnOperationChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (RenameOperation op in e.NewItems)
                {
                    op.PropertyChanged += OnOperationChanged;
                }
            }

            this.RefreshPreview();
        }

        private void OnOperationChanged(object? sender, PropertyChangedEventArgs e)
        {
            this.RefreshPreview();
        }

        /// <summary>
        /// Recomputes the before/after preview list from the current files and operations.
        /// </summary>
        private void RefreshPreview()
        {
            this.PreviewItems.Clear();

            foreach (var file in this.DroppedFiles)
            {
                string newName;
                try
                {
                    newName = FileRenamer.Apply(file.FileName, this.Operations);
                }
                catch (Exception ex)
                {
                    // Never let a preview computation crash the UI; show the problem inline instead.
                    newName = $"⚠ {ex.Message}";
                }

                this.PreviewItems.Add(new PreviewItem(file.FileName, newName));
            }
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "File Renamer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// The kinds of rename operations supported by the demo.
    /// </summary>
    public enum RenameOperationType
    {
        /// <summary>Plain text search-and-replace against the file name (extension untouched).</summary>
        TextReplacement,

        /// <summary>Regular-expression replacement against the file name (extension untouched).</summary>
        RegexReplacement,

        /// <summary>Append text to the end of the file name (before the extension).</summary>
        AppendText,

        /// <summary>Prepend text to the start of the file name.</summary>
        PrependText,

        /// <summary>Change the file extension only.</summary>
        ChangeExtension
    }

    /// <summary>
    /// A friendly display option for a <see cref="RenameOperationType"/>, used to populate the dropdown.
    /// </summary>
    public sealed class RenameOperationOption
    {
        public RenameOperationType Type { get; }

        public string DisplayName { get; }

        private RenameOperationOption(RenameOperationType type, string displayName)
        {
            this.Type = type;
            this.DisplayName = displayName;
        }

        public static IReadOnlyList<RenameOperationOption> All { get; } = new[]
        {
            new RenameOperationOption(RenameOperationType.TextReplacement, "Text Replacement"),
            new RenameOperationOption(RenameOperationType.RegexReplacement, "RegEx Replacement"),
            new RenameOperationOption(RenameOperationType.AppendText, "Append Text"),
            new RenameOperationOption(RenameOperationType.PrependText, "Prepend Text"),
            new RenameOperationOption(RenameOperationType.ChangeExtension, "Change Extension"),
        };
    }

    /// <summary>
    /// A single dropped file queued for renaming.
    /// </summary>
    public sealed class DroppedFile : INotifyPropertyChanged
    {
        public DroppedFile(string fullPath)
        {
            this.FullPath = fullPath;
        }

        private string _fullPath = string.Empty;

        /// <summary>
        /// The full path to the file on disk.
        /// </summary>
        public string FullPath
        {
            get => _fullPath;
            private set
            {
                _fullPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FileName));
            }
        }

        /// <summary>
        /// The file name including extension (the portion the operations act on).
        /// </summary>
        public string FileName => Path.GetFileName(this.FullPath);

        /// <summary>
        /// Repoints this entry at a new path after a successful rename.
        /// </summary>
        public void UpdatePath(string newPath) => this.FullPath = newPath;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// A configurable rename operation row. Exposes up to two input fields whose labels and visibility
    /// change with the selected <see cref="OperationType"/>.
    /// </summary>
    public sealed class RenameOperation : INotifyPropertyChanged
    {
        private RenameOperationType _operationType = RenameOperationType.TextReplacement;
        private string _field1Value = string.Empty;
        private string _field2Value = string.Empty;

        /// <summary>
        /// The selected operation type. Changing this updates the field labels and visibility.
        /// </summary>
        public RenameOperationType OperationType
        {
            get => _operationType;
            set
            {
                if (_operationType == value)
                {
                    return;
                }

                _operationType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Field1Label));
                OnPropertyChanged(nameof(Field2Label));
                OnPropertyChanged(nameof(IsSecondFieldVisible));
            }
        }

        /// <summary>
        /// The primary input value (search text, pattern, text to append/prepend, or new extension).
        /// </summary>
        public string Field1Value
        {
            get => _field1Value;
            set { _field1Value = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The secondary input value (replacement text). Only used by replacement operations.
        /// </summary>
        public string Field2Value
        {
            get => _field2Value;
            set { _field2Value = value; OnPropertyChanged(); }
        }

        /// <summary>Label for the first input field, driven by <see cref="OperationType"/>.</summary>
        public string Field1Label => this.OperationType switch
        {
            RenameOperationType.TextReplacement => "Search Text",
            RenameOperationType.RegexReplacement => "RegEx Pattern",
            RenameOperationType.AppendText => "Text to Append",
            RenameOperationType.PrependText => "Text to Prepend",
            RenameOperationType.ChangeExtension => "New Extension",
            _ => "Value"
        };

        /// <summary>Label for the second input field (only meaningful when <see cref="IsSecondFieldVisible"/> is true).</summary>
        public string Field2Label => "Replacement Text";

        /// <summary>
        /// Whether the second input field is shown. Only the two replacement operations use a second field.
        /// </summary>
        public bool IsSecondFieldVisible =>
            this.OperationType is RenameOperationType.TextReplacement or RenameOperationType.RegexReplacement;

        /// <summary>
        /// Validates that the required field(s) for this operation are populated.
        /// </summary>
        /// <returns>An error message if invalid; otherwise <c>null</c>.</returns>
        public string? Validate()
        {
            // Field1 (primary input) is required for every operation. The replacement text is allowed to be empty.
            if (string.IsNullOrEmpty(this.Field1Value))
            {
                return $"\"{this.Field1Label}\" is required.";
            }

            // Surface invalid regular expressions during validation rather than mid-rename.
            if (this.OperationType == RenameOperationType.RegexReplacement)
            {
                try
                {
                    _ = new Regex(this.Field1Value);
                }
                catch (ArgumentException ex)
                {
                    return $"invalid regular expression ({ex.Message}).";
                }
            }

            return null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// A single before/after row in the preview list.
    /// </summary>
    public sealed class PreviewItem
    {
        public PreviewItem(string originalName, string newName)
        {
            this.OriginalName = originalName;
            this.NewName = newName;
        }

        public string OriginalName { get; }

        public string NewName { get; }
    }

    /// <summary>
    /// Raised when a rename operation cannot be applied (for example, an invalid regular expression).
    /// </summary>
    public sealed class RenameException : Exception
    {
        public RenameException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Applies an ordered list of <see cref="RenameOperation"/> instances to a file name.
    /// </summary>
    public static class FileRenamer
    {
        /// <summary>
        /// Computes the resulting file name after applying every operation in order. Filename operations affect
        /// only the name portion (not the extension); <see cref="RenameOperationType.ChangeExtension"/> affects
        /// only the extension.
        /// </summary>
        /// <param name="fileName">The original file name including its extension (for example <c>apple.png</c>).</param>
        /// <param name="operations">The operations to apply, in order.</param>
        /// <returns>The new file name including extension.</returns>
        public static string Apply(string fileName, IEnumerable<RenameOperation> operations)
        {
            // Split into the name (without extension) and the extension (which includes the leading dot, e.g. ".png").
            // Operations work on "name" unless they explicitly change the extension, so the extension is preserved
            // through replacements/appends/prepends.
            string name = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);

            foreach (var op in operations)
            {
                // Treat an empty primary field as a no-op. This keeps the live preview working while the user is
                // still filling in a freshly added operation, and avoids exceptions such as String.Replace
                // throwing on an empty search string. Required fields are still enforced by Validate() on execute.
                if (string.IsNullOrEmpty(op.Field1Value))
                {
                    continue;
                }

                try
                {
                    switch (op.OperationType)
                    {
                        case RenameOperationType.TextReplacement:
                            name = name.Replace(op.Field1Value, op.Field2Value);
                            break;

                        case RenameOperationType.RegexReplacement:
                            name = Regex.Replace(name, op.Field1Value, op.Field2Value);
                            break;

                        case RenameOperationType.AppendText:
                            name += op.Field1Value;
                            break;

                        case RenameOperationType.PrependText:
                            name = op.Field1Value + name;
                            break;

                        case RenameOperationType.ChangeExtension:
                            extension = NormalizeExtension(op.Field1Value);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Surface any failure (invalid regex, replacement errors, etc.) as a friendly RenameException
                    // so callers can show it in the preview or a message box rather than crashing.
                    throw new RenameException($"{op.OperationType} failed: {ex.Message}");
                }
            }

            return name + extension;
        }

        /// <summary>
        /// Normalizes a user-entered extension so it always carries exactly one leading dot. Both <c>jpg</c>
        /// and <c>.jpg</c> are accepted; an empty value yields no extension.
        /// </summary>
        private static string NormalizeExtension(string extension)
        {
            extension = (extension ?? string.Empty).Trim();

            if (extension.Length == 0)
            {
                return string.Empty;
            }

            // Infer whether the user already included the dot.
            return extension.StartsWith('.') ? extension : "." + extension;
        }
    }
}
