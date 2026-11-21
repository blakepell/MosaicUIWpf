/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Argus.Memory;
using Microsoft.Win32;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// A small word processor in its own window: a custom title bar, a <see cref="Ribbon"/> with a Backstage,
    /// a Table Design contextual tab, a document and a status bar. File commands are real (Rich Text open,
    /// save and print) and Ribbon preferences persist between sessions.
    /// </summary>
    public partial class RibbonDemoWindow
    {
        private const string AppName = "Mosaic Writer";

        private static readonly string PreferencesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MosaicWpfDemo", "ribbon-window-preferences.xml");

        private string? _filePath;
        private bool _isDirty;
        private bool _updatingFormatState;

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonDemoWindow"/> class.
        /// </summary>
        public RibbonDemoWindow()
        {
            InitializeComponent();

            FontFamilyCombo.ItemsSource = new[] { "Segoe UI", "Calibri", "Cambria", "Aptos", "Arial", "Georgia", "Times New Roman", "Verdana", "Consolas" };
            FontSizeCombo.ItemsSource = new[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 36, 48, 72 };
            TabIndicatorAnimationCombo.ItemsSource = Enum.GetValues<RibbonTabIndicatorAnimation>();
            foreach (var animation in Enum.GetValues<RibbonTabIndicatorAnimation>())
            {
                var item = new RibbonMenuItem { Header = animation.ToString(), Tag = animation, IsCheckable = true };
                item.Click += TabAnimationItem_Click;
                TabAnimationMenu.Items.Add(item);
            }

            UpdateTabAnimationChecks();
            StylesGallery.ItemsSource = RibbonStyleItem.CreateDefaults();
            GreetingText.Text = DateTime.Now.Hour switch { < 12 => "Good morning", < 18 => "Good afternoon", _ => "Good evening" };
            PrintLayoutToggle.IsChecked = true;

            LoadDocument(CreateWelcomeDocument(), null);
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            // Default Quick Access Toolbar; saved preferences (restored after the commands exist) replace it.
            WriterRibbon.AddToQuickAccessToolbar("Undo");
            WriterRibbon.AddToQuickAccessToolbar("Redo");
            WriterRibbon.AddToQuickAccessToolbar("Bold");

            var result = RibbonSerializer.LoadFromFile(WriterRibbon, PreferencesPath);
            PreferencesText.Text = result.Success
                ? $"Ribbon preferences were restored from {PreferencesPath} and are saved again when this window closes."
                : $"Ribbon preferences are saved to {PreferencesPath} when this window closes.";

            SyncLayoutOptions();
            WriterRibbon.LayoutModeChanged += (_, _) => SyncLayoutOptions();
            Document.Focus();
            Document_SelectionChanged(Document, e);
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            try
            {
                RibbonSerializer.SaveToFile(WriterRibbon, PreferencesPath);
            }
            catch (IOException)
            {
                // Preferences are a convenience; never block closing the window.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        #region Window chrome

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            AppServices.GetRequiredService<ThemeManager>().CycleTheme();
        }

        private void UpdateTitle()
        {
            var name = _filePath != null ? Path.GetFileName(_filePath) : "Document1";
            Title = $"{name}{(_isDirty ? "*" : string.Empty)} - {AppName}";
            InfoText.Text = _filePath != null
                ? $"Location: {_filePath}\nStatus: {(_isDirty ? "unsaved changes" : "saved")}"
                : "This document has not been saved yet. Use Save As to choose a location.";
        }

        #endregion

        #region File commands

        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadDocument(new FlowDocument(new Paragraph()), null);
            SetStatus("New document");
        }

        private void NewFromTemplate_Click(object sender, RoutedEventArgs e)
        {
            LoadDocument(CreateWelcomeDocument(), null);
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Rich Text (*.rtf)|*.rtf|Text (*.txt)|*.txt" };
            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            var document = new FlowDocument();
            var range = new TextRange(document.ContentStart, document.ContentEnd);
            using (var stream = File.OpenRead(dialog.FileName))
            {
                range.Load(stream, IsRtf(dialog.FileName) ? DataFormats.Rtf : DataFormats.Text);
            }

            LoadDocument(document, dialog.FileName);
            SetStatus($"Opened {Path.GetFileName(dialog.FileName)}");
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_filePath == null)
            {
                SaveAs_Executed(sender, e);
            }
            else
            {
                SaveTo(_filePath);
            }
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Filter = "Rich Text (*.rtf)|*.rtf|Text (*.txt)|*.txt", FileName = _filePath != null ? Path.GetFileName(_filePath) : "Document1.rtf" };
            if (dialog.ShowDialog(this) == true)
            {
                SaveTo(dialog.FileName);
            }
        }

        private void SaveTo(string path)
        {
            var range = new TextRange(Document.Document.ContentStart, Document.Document.ContentEnd);
            using (var stream = File.Create(path))
            {
                range.Save(stream, IsRtf(path) ? DataFormats.Rtf : DataFormats.Text);
            }

            _filePath = path;
            _isDirty = false;
            UpdateTitle();
            SetStatus($"Saved {Path.GetFileName(path)}");
        }

        private void Print_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            // Print a copy so pagination doesn't disturb the document being edited.
            var copy = new FlowDocument
            {
                PageWidth = dialog.PrintableAreaWidth,
                PageHeight = dialog.PrintableAreaHeight,
                PagePadding = new Thickness(72),
                ColumnWidth = double.PositiveInfinity
            };
            var buffer = new MemoryStream();
            new TextRange(Document.Document.ContentStart, Document.Document.ContentEnd).Save(buffer, DataFormats.Xaml);
            new TextRange(copy.ContentStart, copy.ContentEnd).Load(buffer, DataFormats.Xaml);
            dialog.PrintDocument(((IDocumentPaginatorSource)copy).DocumentPaginator, Title);
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e) => Close();

        private void LoadDocument(FlowDocument document, string? path)
        {
            document.PagePadding = new Thickness(0);
            if (path == null || !IsRtf(path))
            {
                document.FontFamily = new FontFamily("Segoe UI");
                document.FontSize = 14.667;
            }

            Document.Document = document;
            _filePath = path;
            _isDirty = false;
            UpdateTitle();
            UpdateWordCount();

            if (Backstage.IsOpen)
            {
                Backstage.Close(restoreFocus: true);
            }
        }

        private static bool IsRtf(string path) => string.Equals(Path.GetExtension(path), ".rtf", StringComparison.OrdinalIgnoreCase);

        #endregion

        #region Home and Insert

        private void PasteTextOnly_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                Document.Selection.Text = Clipboard.GetText();
            }
        }

        private void FontColor_Click(object sender, RoutedEventArgs e)
        {
            var brush = (sender as FrameworkElement)?.Tag is string hex ? new BrushConverter().ConvertFromString(hex) : Document.Foreground;
            Document.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
        }

        private void FontFamilyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_updatingFormatState && FontFamilyCombo.SelectedItem is string family)
            {
                Document.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily(family));
            }
        }

        private void FontSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_updatingFormatState && FontSizeCombo.SelectedItem is int points)
            {
                Document.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, points * 96.0 / 72.0);
            }
        }

        private void StylesGallery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            if (StylesGallery.SelectedItem is RibbonStyleItem style && Document.CaretPosition.Paragraph is { } paragraph)
            {
                style.ApplyTo(paragraph);
            }
        }

        private void InsertPageBreak_Click(object sender, RoutedEventArgs e)
        {
            Document.CaretPosition.InsertParagraphBreak();
            if (Document.CaretPosition.Paragraph is { } paragraph)
            {
                paragraph.BreakPageBefore = true;
            }
        }

        private void InsertText_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: string text })
            {
                Document.Selection.Text = text == "date" ? DateTime.Now.ToString("D", CultureInfo.CurrentCulture) : text;
                Document.CaretPosition = Document.Selection.End;
            }
        }

        private void InsertLink_Click(object sender, RoutedEventArgs e)
        {
            var text = Document.Selection.IsEmpty ? "https://www.apexgate.net" : Document.Selection.Text;
            Document.Selection.Text = string.Empty;
            var position = Document.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
            _ = new System.Windows.Documents.Hyperlink(new Run(text), position) { NavigateUri = Uri.TryCreate(text, UriKind.Absolute, out var uri) ? uri : null };
        }

        private void TableSizePicker_TableSizeSelected(object? sender, TableSizeSelectedEventArgs e)
        {
            TableMenu.IsDropDownOpen = false;

            var table = new Table { CellSpacing = 0 };
            for (var c = 0; c < e.ColumnCount; c++)
            {
                table.Columns.Add(new TableColumn());
            }

            var group = new TableRowGroup();
            for (var r = 0; r < e.RowCount; r++)
            {
                group.Rows.Add(CreateRow(e.ColumnCount));
            }

            table.RowGroups.Add(group);

            // Insert after the caret's paragraph (or at the end), then put the caret in the first cell so the
            // Table Design contextual tab appears.
            if (Document.CaretPosition.Paragraph is { Parent: FlowDocument } paragraph)
            {
                Document.Document.Blocks.InsertAfter(paragraph, table);
            }
            else
            {
                Document.Document.Blocks.Add(table);
            }

            Document.CaretPosition = ((Paragraph)group.Rows[0].Cells[0].Blocks.FirstBlock).ContentStart;
            Document.Focus();
            WriterRibbon.SelectTab(TableDesignTab, openWhenMinimized: false);
        }

        #endregion

        #region Review and View

        private void Spelling_Changed(object sender, RoutedEventArgs e)
        {
            SpellCheck.SetIsEnabled(Document, SpellingToggle.IsChecked == true);
        }

        private void WordCount_Click(object sender, RoutedEventArgs e)
        {
            var text = new TextRange(Document.Document.ContentStart, Document.Document.ContentEnd).Text;
            SetStatus($"{CountWords(text)} words, {text.Replace("\r\n", string.Empty).Length} characters");
        }

        private void ViewMode_Checked(object sender, RoutedEventArgs e)
        {
            var other = sender == PrintLayoutToggle ? WebLayoutToggle : PrintLayoutToggle;
            other.IsChecked = false;
            Page.MaxWidth = sender == WebLayoutToggle ? double.PositiveInfinity : 820;
        }

        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value = (sender as FrameworkElement)?.Tag switch
            {
                "+" => Math.Min(ZoomSlider.Maximum, Math.Round(ZoomSlider.Value / 10) * 10 + 10),
                "-" => Math.Max(ZoomSlider.Minimum, Math.Round(ZoomSlider.Value / 10) * 10 - 10),
                _ => 100
            };
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Raised while the XAML is still loading (Minimum/Value are set before the other elements exist).
            if (PageZoom == null || ZoomText == null)
            {
                return;
            }

            PageZoom.ScaleX = PageZoom.ScaleY = e.NewValue / 100.0;
            ZoomText.Text = $"{e.NewValue:0}%";
        }

        private void ToggleLayoutMode_Click(object sender, RoutedEventArgs e)
        {
            WriterRibbon.LayoutMode = WriterRibbon.LayoutMode == RibbonLayoutMode.Classic ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
        }

        private void ToggleMinimize_Click(object sender, RoutedEventArgs e) => WriterRibbon.IsMinimized = !WriterRibbon.IsMinimized;

        private void TabAnimationItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: RibbonTabIndicatorAnimation animation })
            {
                WriterRibbon.TabIndicatorAnimation = animation;
                SetStatus($"Tab underline animation: {animation} (switch tabs to see it)");
            }

            UpdateTabAnimationChecks();
        }

        private void TabAnimationMenu_DropDownOpened(object sender, RoutedEventArgs e)
        {
            // The animation can also change from File › Options, so refresh the checks each time the menu opens.
            UpdateTabAnimationChecks();
        }

        private void UpdateTabAnimationChecks()
        {
            foreach (var item in TabAnimationMenu.Items.OfType<RibbonMenuItem>())
            {
                item.IsChecked = Equals(item.Tag, WriterRibbon.TabIndicatorAnimation);
            }
        }

        private void LayoutOption_Checked(object sender, RoutedEventArgs e)
        {
            WriterRibbon.LayoutMode = sender == SimplifiedOption ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
        }

        private void SyncLayoutOptions()
        {
            ClassicOption.IsChecked = WriterRibbon.LayoutMode == RibbonLayoutMode.Classic;
            SimplifiedOption.IsChecked = WriterRibbon.LayoutMode == RibbonLayoutMode.Simplified;
        }

        private void NotImplemented_Click(object sender, RoutedEventArgs e)
        {
            SetStatus($"{(sender as RibbonButton)?.Label} isn't part of this sample");
        }

        #endregion

        #region Table Design (contextual)

        private void InsertRowAbove_Click(object sender, RoutedEventArgs e) => InsertRow(above: true);

        private void InsertRowBelow_Click(object sender, RoutedEventArgs e) => InsertRow(above: false);

        private void InsertRow(bool above)
        {
            if (CurrentCell() is { Parent: TableRow row } && row.Parent is TableRowGroup group)
            {
                var index = group.Rows.IndexOf(row) + (above ? 0 : 1);
                group.Rows.Insert(index, CreateRow(row.Cells.Count));
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentCell() is { Parent: TableRow row } && row.Parent is TableRowGroup { Parent: Table table } group)
            {
                group.Rows.Remove(row);
                if (group.Rows.Count == 0)
                {
                    RemoveTable(table);
                }
            }
        }

        private void DeleteTable_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentCell() is { Parent: TableRow { Parent: TableRowGroup { Parent: Table table } } })
            {
                RemoveTable(table);
            }
        }

        private void RemoveTable(Table table)
        {
            Document.Document.Blocks.Remove(table);
            if (Document.Document.Blocks.Count == 0)
            {
                Document.Document.Blocks.Add(new Paragraph());
            }

            Document.Focus();
            UpdateTableContext();
        }

        private TableCell? CurrentCell()
        {
            DependencyObject? current = Document.CaretPosition.Parent;
            while (current is TextElement element)
            {
                if (element is TableCell cell)
                {
                    return cell;
                }

                current = element.Parent;
            }

            return null;
        }

        private static TableRow CreateRow(int columns)
        {
            var row = new TableRow();
            for (var c = 0; c < columns; c++)
            {
                row.Cells.Add(new TableCell(new Paragraph()) { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5), Padding = new Thickness(4) });
            }

            return row;
        }

        private void UpdateTableContext()
        {
            // The contextual tab follows the caret: it appears inside a table and disappears outside one,
            // and the Ribbon falls back to an ordinary tab if Table Design was selected.
            TableContext.IsActive = CurrentCell() != null;
        }

        #endregion

        #region Document state

        private void Document_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateTableContext();

            _updatingFormatState = true;
            try
            {
                var selection = Document.Selection;
                BoldToggle.IsChecked = Equals(selection.GetPropertyValue(TextElement.FontWeightProperty), FontWeights.Bold);
                ItalicToggle.IsChecked = Equals(selection.GetPropertyValue(TextElement.FontStyleProperty), FontStyles.Italic);
                UnderlineToggle.IsChecked = selection.GetPropertyValue(Inline.TextDecorationsProperty) is TextDecorationCollection { Count: > 0 };

                if (selection.GetPropertyValue(TextElement.FontFamilyProperty) is FontFamily family)
                {
                    FontFamilyCombo.Text = family.Source;
                }

                if (selection.GetPropertyValue(TextElement.FontSizeProperty) is double size)
                {
                    FontSizeCombo.Text = Math.Round(size * 72.0 / 96.0).ToString(CultureInfo.InvariantCulture);
                }
            }
            finally
            {
                _updatingFormatState = false;
            }
        }

        private void Document_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (!_isDirty)
            {
                _isDirty = true;
                UpdateTitle();
            }

            UpdateWordCount();
        }

        private void WriterRibbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Tab selection is the Ribbon's own concern; don't let it bubble into anything listening for list selection.
            e.Handled = true;
        }

        private void UpdateWordCount()
        {
            var words = CountWords(new TextRange(Document.Document.ContentStart, Document.Document.ContentEnd).Text);
            WordCountText.Text = words == 1 ? "1 word" : $"{words:N0} words";
        }

        private static int CountWords(string text) => Regex.Matches(text, @"\S+").Count;

        private void SetStatus(string message) => StatusText.Text = message;

        private static FlowDocument CreateWelcomeDocument()
        {
            var doc = new FlowDocument();
            doc.Blocks.Add(new Paragraph(new Run("Welcome to Mosaic Writer")) { FontSize = 28, FontWeight = FontWeights.Light, Margin = new Thickness(0, 0, 0, 8) });
            doc.Blocks.Add(new Paragraph(new Run(
                "This window hosts a Mosaic Ribbon the way a real application would: under a custom title bar, above the document, with a status bar at the bottom. Resize the window to watch the groups reduce from right to left, collapse into buttons, and finally hide the Ribbon entirely when the window gets very small.")));
            doc.Blocks.Add(new Paragraph(new Run(
                "Click File to open the Backstage. Open, Save, Save As and Print work on Rich Text files. Insert › Table adds a table; click inside it to reveal the Table Design contextual tab, and click outside it to make the tab go away.")));
            doc.Blocks.Add(new Paragraph(new Run(
                "Press Alt for key tips, Ctrl+F1 to collapse the Ribbon, and right-click any command to add it to the Quick Access Toolbar. The Ribbon's layout mode, collapsed state and toolbar are remembered the next time you open this window.")));
            return doc;
        }

        #endregion
    }
}
