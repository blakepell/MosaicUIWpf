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
        ChangeExtension,

        /// <summary>Trim whitespace around the file name and extension.</summary>
        TrimWhitespace,

        /// <summary>Truncate the file name to a maximum length without counting the extension.</summary>
        TruncateAtLength
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
            new RenameOperationOption(RenameOperationType.TrimWhitespace, "Trim Whitespace"),
            new RenameOperationOption(RenameOperationType.TruncateAtLength, "Truncate at Length"),
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

                if (value == RenameOperationType.TruncateAtLength)
                {
                    this.Field1Value = "256";
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(Field1Label));
                OnPropertyChanged(nameof(Field2Label));
                OnPropertyChanged(nameof(Field2ToolTip));
                OnPropertyChanged(nameof(IsFirstFieldVisible));
                OnPropertyChanged(nameof(IsTextFieldVisible));
                OnPropertyChanged(nameof(IsNumericFieldVisible));
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
            RenameOperationType.TruncateAtLength => "Maximum Length",
            _ => "Value"
        };

        /// <summary>Whether the operation requires a primary input field.</summary>
        public bool IsFirstFieldVisible => this.OperationType != RenameOperationType.TrimWhitespace;

        /// <summary>Whether the primary input should accept free-form text.</summary>
        public bool IsTextFieldVisible =>
            this.OperationType is not RenameOperationType.TrimWhitespace and not RenameOperationType.TruncateAtLength;

        /// <summary>Whether the primary input should accept a whole number.</summary>
        public bool IsNumericFieldVisible => this.OperationType == RenameOperationType.TruncateAtLength;

        /// <summary>Label for the second input field (only meaningful when <see cref="IsSecondFieldVisible"/> is true).</summary>
        public string Field2Label => this.OperationType == RenameOperationType.RegexReplacement
            ? "Replacement Text (%1, %{name})"
            : "Replacement Text";

        /// <summary>
        /// Tooltip for the second input field, documenting the capture-group syntax for regex replacements.
        /// </summary>
        public string Field2ToolTip => this.OperationType == RenameOperationType.RegexReplacement
            ? "Insert a captured group with %1, %2, ... (by position) or %{name} / %name (by name). "
              + "%0 is the entire match and %% produces a literal percent sign. All other text is literal."
            : "The text that replaces each match. Leave empty to remove the matched text.";

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
            // Trim Whitespace has no input. All other operations require the primary field.
            if (this.OperationType != RenameOperationType.TrimWhitespace && string.IsNullOrEmpty(this.Field1Value))
            {
                return $"\"{this.Field1Label}\" is required.";
            }

            if (this.OperationType == RenameOperationType.TruncateAtLength
                && (!int.TryParse(this.Field1Value, out int maximumLength) || maximumLength <= 0))
            {
                return "\"Maximum Length\" must be a positive whole number.";
            }

            // Surface invalid regular expressions (and bad group references) during validation rather than mid-rename.
            if (this.OperationType == RenameOperationType.RegexReplacement)
            {
                Regex regex;

                try
                {
                    regex = new Regex(this.Field1Value);
                }
                catch (ArgumentException ex)
                {
                    return $"invalid regular expression ({ex.Message}).";
                }

                try
                {
                    _ = FileRenamer.BuildRegexReplacement(this.Field2Value, regex);
                }
                catch (RenameException ex)
                {
                    return ex.Message;
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
                if (op.OperationType != RenameOperationType.TrimWhitespace && string.IsNullOrEmpty(op.Field1Value))
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
                        {
                            var regex = new Regex(op.Field1Value);
                            name = regex.Replace(name, BuildRegexReplacement(op.Field2Value, regex));
                            break;
                        }

                        case RenameOperationType.AppendText:
                            name += op.Field1Value;
                            break;

                        case RenameOperationType.PrependText:
                            name = op.Field1Value + name;
                            break;

                        case RenameOperationType.ChangeExtension:
                            extension = NormalizeExtension(op.Field1Value);
                            break;

                        case RenameOperationType.TrimWhitespace:
                            name = name.Trim();
                            extension = extension.Trim();
                            break;

                        case RenameOperationType.TruncateAtLength:
                            if (!int.TryParse(op.Field1Value, out int maximumLength) || maximumLength <= 0)
                            {
                                throw new RenameException("\"Maximum Length\" must be a positive whole number.");
                            }

                            if (name.Length > maximumLength)
                            {
                                name = name[..maximumLength];
                            }

                            break;
                    }
                }
                catch (RenameException)
                {
                    // Already a friendly, fully-formed message (for example an unknown group reference).
                    throw;
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
        /// Translates the user's replacement text into a .NET <see cref="Regex"/> substitution pattern, mapping
        /// percent-style capture-group references onto the framework's <c>$</c> syntax.
        /// </summary>
        /// <remarks>
        /// Supported tokens:
        /// <list type="bullet">
        ///   <item><description><c>%1</c>, <c>%2</c>, ... - the capture group at that position (<c>%0</c> is the entire match).</description></item>
        ///   <item><description><c>%{name}</c> or <c>%name</c> - the named capture group (the braced form is required when the
        ///   name butts up against following text, e.g. <c>%{year}archive</c>).</description></item>
        ///   <item><description><c>%%</c> - a literal percent sign.</description></item>
        /// </list>
        /// Everything else is literal, including <c>$</c>, which is escaped so it is never mistaken for a
        /// substitution by the regex engine.
        /// </remarks>
        /// <param name="replacement">The raw replacement text the user typed.</param>
        /// <param name="regex">The compiled pattern, used to verify that every referenced group actually exists.</param>
        /// <exception cref="RenameException">A referenced capture group does not exist in the pattern.</exception>
        public static string BuildRegexReplacement(string? replacement, Regex regex)
        {
            if (string.IsNullOrEmpty(replacement))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(replacement.Length);

            for (int i = 0; i < replacement.Length; i++)
            {
                char c = replacement[i];

                // The user's text is literal apart from '%' tokens, so a '$' has to be escaped ("$$") or the
                // regex engine would treat it as a substitution of its own.
                if (c == '$')
                {
                    sb.Append("$$");
                    continue;
                }

                if (c != '%' || i == replacement.Length - 1)
                {
                    sb.Append(c);
                    continue;
                }

                char next = replacement[i + 1];

                // "%%" escapes a literal percent sign.
                if (next == '%')
                {
                    sb.Append('%');
                    i++;
                    continue;
                }

                // "%{name}" - the braced form, which can be followed immediately by more text.
                if (next == '{')
                {
                    int close = replacement.IndexOf('}', i + 2);

                    if (close > i + 2)
                    {
                        string braced = replacement.Substring(i + 2, close - i - 2);
                        AppendGroupReference(sb, braced, regex, replacement);
                        i = close;
                        continue;
                    }

                    // Unterminated "%{" - treat the percent sign as literal text.
                    sb.Append(c);
                    continue;
                }

                // "%1" (digits) or "%name" (identifier). Scan the longest run of the appropriate character class.
                int end = i + 1;

                if (char.IsDigit(next))
                {
                    while (end < replacement.Length && char.IsDigit(replacement[end]))
                    {
                        end++;
                    }
                }
                else if (char.IsLetter(next) || next == '_')
                {
                    while (end < replacement.Length && (char.IsLetterOrDigit(replacement[end]) || replacement[end] == '_'))
                    {
                        end++;
                    }
                }

                if (end == i + 1)
                {
                    // A '%' followed by something that can't start a group name is just a percent sign.
                    sb.Append(c);
                    continue;
                }

                AppendGroupReference(sb, replacement.Substring(i + 1, end - i - 1), regex, replacement);
                i = end - 1;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Emits a validated <c>${group}</c> substitution for the supplied token.
        /// </summary>
        private static void AppendGroupReference(StringBuilder sb, string token, Regex regex, string replacement)
        {
            // Numbered groups are matched by number, named groups by name. Regex.GroupNumberFromName covers both,
            // returning -1 when the group does not exist in the pattern.
            if (regex.GroupNumberFromName(token) < 0)
            {
                throw new RenameException(
                    $"the replacement text \"{replacement}\" references capture group \"{token}\", which the pattern does not define. "
                    + $"Available groups: {string.Join(", ", regex.GetGroupNames())}.");
            }

            sb.Append("${").Append(token).Append('}');
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
