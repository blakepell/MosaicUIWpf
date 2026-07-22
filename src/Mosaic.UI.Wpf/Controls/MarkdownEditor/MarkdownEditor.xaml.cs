/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit.Snippets;
using Markdig;
using Microsoft.Win32;
using Mosaic.UI.Wpf.Interfaces;
using System.Windows.Media.Imaging;

// ReSharper disable MemberCanBePrivate.Global

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A self-contained markdown editor built on the Mosaic <see cref="SyntaxEditor"/> (AvalonEdit).
    /// Provides a formatting toolbar, list/heading helpers, markdown-aware key handling, an extended
    /// context menu (including "Paste Image as Base64"), and document modification tracking.
    /// </summary>
    public partial class MarkdownEditor : UserControl, ISaveable
    {
        /// <summary>
        /// The default title used when the document has no file path or file name.
        /// </summary>
        private const string DefaultTitle = "Untitled";

        /// <summary>
        /// Suppresses the modification flag while text is being loaded programmatically.
        /// </summary>
        private bool _suppressModified;

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="IsModified"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register(
            nameof(IsModified),
            typeof(bool),
            typeof(MarkdownEditor),
            new FrameworkPropertyMetadata(false, OnTitleAffectingPropertyChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the document has unsaved changes. When <c>true</c>,
        /// an asterisk (<c>*</c>) is appended to <see cref="DocumentTitle"/>.
        /// </summary>
        public bool IsModified
        {
            get => (bool)this.GetValue(IsModifiedProperty);
            set => this.SetValue(IsModifiedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FilePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(
            nameof(FilePath),
            typeof(string),
            typeof(MarkdownEditor),
            new FrameworkPropertyMetadata(null, OnTitleAffectingPropertyChanged));

        /// <summary>
        /// Gets or sets the file path of the document. When set, <see cref="Save()"/> writes to this path.
        /// </summary>
        public string? FilePath
        {
            get => (string?)this.GetValue(FilePathProperty);
            set => this.SetValue(FilePathProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FileName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(
            nameof(FileName),
            typeof(string),
            typeof(MarkdownEditor),
            new FrameworkPropertyMetadata(null, OnTitleAffectingPropertyChanged));

        /// <summary>
        /// Gets or sets the display name to use for a virtual document that has no file path.
        /// </summary>
        public string? FileName
        {
            get => (string?)this.GetValue(FileNameProperty);
            set => this.SetValue(FileNameProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DocumentTitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DocumentTitleProperty = DependencyProperty.Register(
            nameof(DocumentTitle),
            typeof(string),
            typeof(MarkdownEditor),
            new FrameworkPropertyMetadata(DefaultTitle));

        /// <summary>
        /// Gets the computed title of the document, including a trailing asterisk when
        /// <see cref="IsModified"/> is <c>true</c>. Intended to be bound to a host's tab or menu caption.
        /// </summary>
        public string DocumentTitle
        {
            get => (string)this.GetValue(DocumentTitleProperty);
            private set => this.SetValue(DocumentTitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StatusBarVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StatusBarVisibilityProperty = DependencyProperty.Register(
            nameof(StatusBarVisibility),
            typeof(Visibility),
            typeof(MarkdownEditor),
            new PropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Gets or sets the visibility of the status bar that displays the caret line, column, and character.
        /// Defaults to <see cref="Visibility.Collapsed"/>.
        /// </summary>
        public Visibility StatusBarVisibility
        {
            get => (Visibility)this.GetValue(StatusBarVisibilityProperty);
            set => this.SetValue(StatusBarVisibilityProperty, value);
        }

        #endregion

        /// <summary>
        /// Gets or sets the markdown text of the document.
        /// </summary>
        public string Text
        {
            get => this.Editor.Text;
            set => this.Editor.Text = value;
        }

        /// <summary>
        /// Raised before a save operation begins. Handlers may set <see cref="CancelEventArgs.Cancel"/>
        /// to <c>true</c> to prevent the default save behavior.
        /// </summary>
        public event EventHandler<CancelEventArgs>? Saving;

        /// <summary>
        /// Raised after the document has been successfully written to disk.
        /// </summary>
        public event EventHandler<DocumentSavedEventArgs>? Saved;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownEditor"/> class.
        /// </summary>
        public MarkdownEditor()
        {
            this.InitializeComponent();

            this.Editor.Options.ConvertTabsToSpaces = true;
            this.Editor.TextChanged += this.Editor_TextChanged;
            this.Editor.PreviewKeyDown += this.Editor_PreviewKeyDown;
            this.Editor.TextArea.Caret.PositionChanged += this.Caret_PositionChanged;
            this.Editor.ContextMenuRequested += this.Editor_ContextMenuRequested;

            this.UpdateDocumentTitle();
            this.UpdateStatusBar();
        }

        #region Document operations

        /// <summary>
        /// Loads a file from disk into the editor.
        /// </summary>
        /// <param name="path">The path of the file to load.</param>
        public void LoadFile(string path)
        {
            string fullPath = Path.GetFullPath(path);
            var fi = new FileInfo(fullPath);

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
            this.FileName = fi.Name;
            this.IsModified = false;
        }

        /// <summary>
        /// Clears the editor for a new, empty document.
        /// </summary>
        public void NewDocument()
        {
            _suppressModified = true;
            this.Editor.Text = string.Empty;
            _suppressModified = false;

            this.FilePath = null;
            this.FileName = null;
            this.IsModified = false;
            this.Editor.Focus();
        }

        /// <summary>
        /// Saves the document to its current <see cref="FilePath"/>. If no path is set, this is a no-op;
        /// use <see cref="SaveAsAsync"/> to prompt the user for a destination.
        /// </summary>
        public void Save()
        {
            if (this.RaiseSavingCancelled())
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(this.FilePath))
            {
                File.WriteAllText(this.FilePath, this.Editor.Text);
                this.IsModified = false;
                this.RaiseSaved(this.FilePath);
            }
        }

        /// <summary>
        /// Asynchronously saves the document to its current <see cref="FilePath"/>, prompting for a
        /// destination when no path has been set.
        /// </summary>
        public async Task SaveAsync()
        {
            if (!string.IsNullOrWhiteSpace(this.FilePath))
            {
                if (this.RaiseSavingCancelled())
                {
                    return;
                }

                await File.WriteAllTextAsync(this.FilePath, this.Editor.Text);
                this.IsModified = false;
                this.RaiseSaved(this.FilePath);
            }
            else
            {
                await this.SaveAsAsync();
            }
        }

        /// <summary>
        /// Prompts the user for a file and saves the document to the chosen location.
        /// </summary>
        public async Task SaveAsAsync()
        {
            if (this.RaiseSavingCancelled())
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Markdown (*.md)|*.md|All Files (*.*)|*.*",
                DefaultExt = ".md"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            await File.WriteAllTextAsync(dialog.FileName, this.Editor.Text);
            this.FilePath = dialog.FileName;
            this.FileName = Path.GetFileName(dialog.FileName);
            this.IsModified = false;
            this.RaiseSaved(dialog.FileName);
        }

        /// <summary>
        /// Raises the <see cref="Saving"/> event and reports whether a handler cancelled the operation.
        /// </summary>
        /// <returns><c>true</c> if the save was cancelled; otherwise, <c>false</c>.</returns>
        private bool RaiseSavingCancelled()
        {
            var args = new CancelEventArgs();
            this.Saving?.Invoke(this, args);
            return args.Cancel;
        }

        /// <summary>
        /// Raises the <see cref="Saved"/> event after content has been written to disk.
        /// </summary>
        /// <param name="filePath">The full path the document was saved to.</param>
        private void RaiseSaved(string filePath) =>
            this.Saved?.Invoke(this, new DocumentSavedEventArgs(filePath));

        #endregion

        #region Editor events

        /// <summary>
        /// Marks the document as modified when the text changes.
        /// </summary>
        private void Editor_TextChanged(object? sender, EventArgs e)
        {
            if (!_suppressModified)
            {
                this.IsModified = true;
            }
        }

        /// <summary>
        /// Updates the status bar when the caret moves.
        /// </summary>
        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            this.UpdateStatusBar();
        }

        /// <summary>
        /// Recomputes the caret line, column, and character displayed in the status bar.
        /// </summary>
        private void UpdateStatusBar()
        {
            var caret = this.Editor.TextArea.Caret;
            this.LineTextBlock.Text = $"Ln {caret.Line}";
            this.ColumnTextBlock.Text = $"Col {caret.Column}";

            int offset = this.Editor.CaretOffset;
            char ch = offset < this.Editor.Document.TextLength ? this.Editor.Document.GetCharAt(offset) : ' ';
            this.CharTextBlock.Text = $"Ch {(int)ch}";
        }

        /// <summary>
        /// Adds markdown-specific items to the editor's context menu before it is shown.
        /// </summary>
        private void Editor_ContextMenuRequested(object? sender, SyntaxEditorContextMenuEventArgs e)
        {
            e.ContextMenu.Items.Add(new Separator());

            var copyAsHtml = new MenuItem { Header = "Copy as HTML" };
            copyAsHtml.Click += (_, _) => this.CopyAsHtml();
            e.ContextMenu.Items.Add(copyAsHtml);

            var pasteImage = new MenuItem { Header = "Paste Image as Base64" };
            pasteImage.Click += (_, _) => this.PasteImage();
            e.ContextMenu.Items.Add(pasteImage);
        }

        #endregion

        #region Title tracking

        /// <summary>
        /// Recomputes <see cref="DocumentTitle"/> when a property that affects it changes.
        /// </summary>
        private static void OnTitleAffectingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MarkdownEditor)?.UpdateDocumentTitle();
        }

        /// <summary>
        /// Rebuilds the document title from the file path, file name, or default title, appending an
        /// asterisk when the document has unsaved changes.
        /// </summary>
        private void UpdateDocumentTitle()
        {
            string title;

            if (!string.IsNullOrWhiteSpace(this.FilePath))
            {
                string? name = Path.GetFileName(this.FilePath);
                title = !string.IsNullOrWhiteSpace(name) ? name : DefaultTitle;
            }
            else if (!string.IsNullOrWhiteSpace(this.FileName))
            {
                title = this.FileName;
            }
            else
            {
                title = DefaultTitle;
            }

            this.DocumentTitle = this.IsModified ? title + "*" : title;
        }

        #endregion

        #region Toolbar handlers

        private void SaveButton_Click(object sender, RoutedEventArgs e) => _ = this.SaveAsync();

        private void PreviewInBrowserButton_Click(object sender, RoutedEventArgs e) => this.PreviewInBrowser();

        private void SurroundWithButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: string marker })
            {
                this.SurroundWith(marker);
            }
        }

        private void HeadingButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: string number })
            {
                this.Heading(number);
            }
        }

        private void InsertSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: string name })
            {
                this.InsertSnippet(name);
            }
        }

        private void BulletListButton_Click(object sender, RoutedEventArgs e) => this.ToggleLinePrefix(BulletListTransform);

        private void OrderedListButton_Click(object sender, RoutedEventArgs e) => this.ToggleLinePrefix(OrderedListTransform);

        private void BlockQuoteButton_Click(object sender, RoutedEventArgs e) => this.ToggleLinePrefix(BlockQuoteTransform);

        private void InsertTableButton_Click(object sender, RoutedEventArgs e) => this.InsertTable();

        #endregion

        #region Markdown commands

        /// <summary>
        /// Surrounds the selection with the supplied marker, or removes the marker when the caret is
        /// already inside a matched pair. When there is no selection, the markers are inserted and the
        /// caret is placed between them.
        /// </summary>
        /// <param name="marker">The marker to wrap the selection with (e.g. <c>**</c>, <c>__</c>).</param>
        public void SurroundWith(string marker)
        {
            int caretOffset = this.Editor.CaretOffset;
            string text = this.Editor.Text;

            // If the caret is within an existing matched pair, remove the markers.
            if (caretOffset >= marker.Length && caretOffset <= text.Length - marker.Length)
            {
                int start = text.LastIndexOf(marker, caretOffset - 1, StringComparison.Ordinal);
                int end = text.IndexOf(marker, caretOffset, StringComparison.Ordinal);

                if (start != -1 && end != -1 && start < end)
                {
                    this.Editor.Document.Remove(end, marker.Length);
                    this.Editor.Document.Remove(start, marker.Length);
                    this.Editor.CaretOffset = caretOffset - marker.Length;
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(this.Editor.SelectedText))
            {
                int pos = this.Editor.CaretOffset;
                this.Editor.Document.Insert(pos, marker + marker);
                this.Editor.CaretOffset = pos + marker.Length;
                return;
            }

            this.Editor.SelectedText = $"{marker}{this.Editor.SelectedText}{marker}";
        }

        /// <summary>
        /// Applies (or replaces) a markdown heading on the current line.
        /// </summary>
        /// <param name="headingNumber">The heading level (1-6) as a string.</param>
        public void Heading(string headingNumber = "1")
        {
            if (!int.TryParse(headingNumber, out int level) || level < 1 || level > 6)
            {
                level = 1;
            }

            var line = this.Editor.Document.GetLineByNumber(this.Editor.TextArea.Caret.Line);
            string lineText = this.Editor.Document.GetText(line.Offset, line.Length);

            var headingMatch = Regex.Match(lineText, @"^#+\s*");

            string newText = headingMatch.Success
                ? $"{new string('#', level)} {lineText.Substring(headingMatch.Length)}"
                : $"{new string('#', level)} {lineText}";

            this.Editor.Document.Replace(line.Offset, line.Length, newText);
        }

        /// <summary>
        /// Toggles a bullet (<c>-</c>) prefix on a single line.
        /// </summary>
        private static string BulletListTransform(string text)
        {
            var match = Regex.Match(text, @"^-\s*");
            return match.Success ? text.Substring(match.Length) : $"- {text}";
        }

        /// <summary>
        /// Toggles an ordered list (<c>1.</c>) prefix on a single line.
        /// </summary>
        private static string OrderedListTransform(string text)
        {
            var match = Regex.Match(text, @"^1\.\s*");
            return match.Success ? text.Substring(match.Length) : $"1. {text}";
        }

        /// <summary>
        /// Toggles a block quote (<c>&gt;</c>) prefix on a single line.
        /// </summary>
        private static string BlockQuoteTransform(string text)
        {
            var match = Regex.Match(text, @"^>\s*");
            return match.Success ? text.Substring(match.Length) : $"> {text}";
        }

        /// <summary>
        /// Applies a line transformation across every line touched by the current selection (or the
        /// current line when there is no selection).
        /// </summary>
        private void ToggleLinePrefix(Func<string, string> transform)
        {
            var document = this.Editor.Document;

            int startOffset = this.Editor.SelectionLength > 0 ? this.Editor.SelectionStart : this.Editor.CaretOffset;
            int endOffset = this.Editor.SelectionLength > 0 ? this.Editor.SelectionStart + this.Editor.SelectionLength : this.Editor.CaretOffset;

            int firstLine = document.GetLineByOffset(Math.Clamp(startOffset, 0, document.TextLength)).LineNumber;
            int lastLine = document.GetLineByOffset(Math.Clamp(endOffset, 0, document.TextLength)).LineNumber;

            document.BeginUpdate();

            try
            {
                for (int i = firstLine; i <= lastLine; i++)
                {
                    var line = document.GetLineByNumber(i);
                    string text = document.GetText(line.Offset, line.Length);
                    string newText = transform(text);

                    if (!string.Equals(text, newText, StringComparison.Ordinal))
                    {
                        document.Replace(line.Offset, line.Length, newText);
                    }
                }
            }
            finally
            {
                document.EndUpdate();
            }
        }

        /// <summary>
        /// Inserts a markdown snippet (link, image, or fenced code block) at the caret.
        /// </summary>
        /// <param name="name">The snippet name: <c>link</c>, <c>image</c>, or <c>code</c>.</param>
        public void InsertSnippet(string name)
        {
            switch (name)
            {
                case "link":
                    this.InsertLinkSnippet("[");
                    break;
                case "image":
                    this.InsertLinkSnippet("![");
                    break;
                case "code":
                    // Insert a fenced code block and position the caret on the blank middle line.
                    int pos = this.Editor.CaretOffset;
                    const string snippet = "```\r\n\r\n```";
                    this.Editor.Document.Insert(pos, snippet);
                    this.Editor.CaretOffset = pos + 5; // after "```\r\n"
                    break;
            }
        }

        /// <summary>
        /// Inserts a markdown link or image as an interactive AvalonEdit snippet. The description and URL are
        /// exposed as replaceable elements so the user can tab between them; each is seeded with indicator
        /// text ("description" / "url") that shows which part is being edited. When there is a selection,
        /// its text is used as the initial description.
        /// </summary>
        /// <param name="prefix">The markdown prefix that precedes the description: <c>[</c> for a link or <c>![</c> for an image.</param>
        private void InsertLinkSnippet(string prefix)
        {
            string description = this.Editor.SelectionLength > 0 ? this.Editor.SelectedText : "description";

            var snippet = new Snippet
            {
                Elements =
                {
                    new SnippetTextElement { Text = prefix },
                    new SnippetReplaceableTextElement { Text = description },
                    new SnippetTextElement { Text = "](" },
                    new SnippetReplaceableTextElement { Text = "url" },
                    new SnippetTextElement { Text = ")" },
                    new SnippetCaretElement()
                }
            };

            snippet.Insert(this.Editor.TextArea);
        }

        /// <summary>
        /// Inserts a default markdown table at the caret.
        /// </summary>
        public void InsertTable()
        {
            const string table =
                "| Column1 | Column2 | Column3 |\r\n" +
                "| --- | --- | --- |\r\n" +
                "| Cell1 | Cell2 | Cell3 |\r\n";

            int pos = this.Editor.CaretOffset;
            this.Editor.Document.Insert(pos, table);
            this.Editor.CaretOffset = pos + table.Length;
        }

        /// <summary>
        /// Converts the selection (or the whole document) to HTML and copies it to the clipboard.
        /// </summary>
        public void CopyAsHtml()
        {
            try
            {
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UsePipeTables().Build();
                string source = this.Editor.SelectionLength > 0 ? this.Editor.SelectedText : this.Editor.Text;
                string html = Markdown.ToHtml(source, pipeline);
                Clipboard.SetText(html, TextDataFormat.Text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Pastes an image from the clipboard into the document as a base64-encoded markdown image.
        /// </summary>
        public void PasteImage()
        {
            if (!Clipboard.ContainsImage())
            {
                MessageBox.Show("There is no available image on the clipboard.", "Clipboard", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BitmapSource? bitmapSource = Clipboard.GetImage();

            if (bitmapSource == null)
            {
                return;
            }

            using var memoryStream = new MemoryStream();
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);

            string base64 = Convert.ToBase64String(memoryStream.ToArray());
            this.Editor.SelectedText = $"![Image](data:image/jpeg;base64,{base64})";
        }

        /// <summary>
        /// Renders the markdown to a temporary HTML file and opens it in the default browser.
        /// </summary>
        public void PreviewInBrowser()
        {
            try
            {
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UsePipeTables().Build();
                string html = Markdown.ToHtml(this.Editor.Text, pipeline);
                string title = Path.GetFileNameWithoutExtension(this.FilePath ?? "Preview");

                var sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html>");
                sb.AppendLine("<head>");
                sb.AppendLine("    <meta charset=\"utf-8\">");
                sb.AppendLine($"    <title>{title}</title>");
                sb.AppendLine("    <style>");
                sb.AppendLine("        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif; max-width: 800px; margin: 40px auto; padding: 0 20px; line-height: 1.6; }");
                sb.AppendLine("        pre { background-color: #f4f4f4; padding: 10px; overflow-x: auto; }");
                sb.AppendLine("        code { background-color: #f4f4f4; padding: 2px 4px; }");
                sb.AppendLine("        table { border-collapse: collapse; width: 100%; }");
                sb.AppendLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                sb.AppendLine("        th { background-color: #f4f4f4; }");
                sb.AppendLine("        blockquote { border-left: 4px solid #ddd; margin: 0; padding-left: 16px; color: #666; }");
                sb.AppendLine("    </style>");
                sb.AppendLine("</head>");
                sb.AppendLine("<body>");
                sb.AppendLine(html);
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                string tempPath = Path.Combine(Path.GetTempPath(), $"markdown_preview_{Guid.NewGuid():N}.html");
                File.WriteAllText(tempPath, sb.ToString());

                Process.Start(new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        #endregion

        #region Markdown-aware key handling

        /// <summary>
        /// Handles Enter (list continuation), Escape (empty list-item removal), and Tab/Shift+Tab
        /// (list indentation) for markdown lists.
        /// </summary>
        private void Editor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+B / Ctrl+I formatting shortcuts.
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.B)
                {
                    this.SurroundWith("**");
                    e.Handled = true;
                }
                else if (e.Key == Key.I)
                {
                    this.SurroundWith("__");
                    e.Handled = true;
                }

                return;
            }

            switch (e.Key)
            {
                case Key.Return:
                    e.Handled = this.HandleEnterKey();
                    break;
                case Key.Escape:
                    e.Handled = this.HandleEscapeKey();
                    break;
                case Key.Tab when Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift):
                    e.Handled = this.HandleShiftTab();
                    break;
                case Key.Tab:
                    e.Handled = this.HandleTab();
                    break;
            }
        }

        /// <summary>
        /// Continues a markdown list on the next line when Enter is pressed inside a list item.
        /// </summary>
        private bool HandleEnterKey()
        {
            try
            {
                var line = this.Editor.Document.GetLineByOffset(this.Editor.CaretOffset);
                string fullText = this.Editor.Document.GetText(line.Offset, line.Length);
                string lineText = fullText.TrimStart();

                // An empty list item ("- " or "1. ") ends the list: clear the marker and stop,
                // mirroring the behavior of the Escape key.
                if (Regex.IsMatch(lineText, @"^-\s*$") || Regex.IsMatch(lineText, @"^\d+\.\s*$"))
                {
                    this.Editor.Document.Remove(line.Offset, line.Length);
                    return true;
                }

                if (Regex.IsMatch(lineText, @"^-\s+"))
                {
                    string leadingWhitespace = fullText.Substring(0, fullText.Length - lineText.Length);
                    string insert = $"\r\n{leadingWhitespace}- ";
                    this.Editor.Document.Insert(line.EndOffset, insert);
                    this.Editor.CaretOffset = line.EndOffset + insert.Length;
                    return true;
                }

                var orderedMatch = Regex.Match(lineText, @"^(\d+)\.\s+");
                if (orderedMatch.Success)
                {
                    string leadingWhitespace = fullText.Substring(0, fullText.Length - lineText.Length);
                    int next = int.TryParse(orderedMatch.Groups[1].Value, out int number) ? number + 1 : 1;
                    string insert = $"\r\n{leadingWhitespace}{next}. ";
                    this.Editor.Document.Insert(line.EndOffset, insert);
                    this.Editor.CaretOffset = line.EndOffset + insert.Length;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return false;
        }

        /// <summary>
        /// Removes an empty list item from the current line when Escape is pressed.
        /// </summary>
        private bool HandleEscapeKey()
        {
            try
            {
                var line = this.Editor.Document.GetLineByOffset(this.Editor.CaretOffset);
                string lineText = this.Editor.Document.GetText(line.Offset, line.Length).TrimStart();

                if (Regex.IsMatch(lineText, @"^(\s*)-\s*$") || Regex.IsMatch(lineText, @"^(\s*)(\d+)\.\s*$"))
                {
                    this.Editor.Document.Remove(line.Offset, line.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return false;
        }

        /// <summary>
        /// Increases the indentation of the current list item when Tab is pressed.
        /// </summary>
        private bool HandleTab()
        {
            try
            {
                var line = this.Editor.Document.GetLineByOffset(this.Editor.CaretOffset);
                string lineText = this.Editor.Document.GetText(line.Offset, line.Length).TrimStart();

                if (Regex.IsMatch(lineText, @"^(\s*)-\s+") || Regex.IsMatch(lineText, @"^(\s*)(\d+)\.\s+"))
                {
                    int endOffset = line.EndOffset;
                    this.Editor.Document.Insert(line.Offset, "    ");
                    this.Editor.CaretOffset = endOffset + 4;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return false;
        }

        /// <summary>
        /// Decreases the indentation of the current list item when Shift+Tab is pressed.
        /// </summary>
        private bool HandleShiftTab()
        {
            try
            {
                var line = this.Editor.Document.GetLineByOffset(this.Editor.CaretOffset);
                string lineText = this.Editor.Document.GetText(line.Offset, line.Length);

                var match = Regex.Match(lineText, @"^(\s*)-\s+");
                if (!match.Success)
                {
                    match = Regex.Match(lineText, @"^(\s*)(\d+)\.\s+");
                }

                if (match.Success)
                {
                    string leadingWhitespace = match.Groups[1].Value;

                    if (leadingWhitespace.Length >= 4)
                    {
                        int endOffset = line.EndOffset;
                        this.Editor.Document.Remove(line.Offset, 4);
                        this.Editor.CaretOffset = Math.Max(line.Offset, endOffset - 4);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return false;
        }

        #endregion
    }
}
