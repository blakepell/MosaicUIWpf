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
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// A document-editor harness for the <see cref="Ribbon"/>: Home/Insert/View tabs, a picture context,
    /// a Backstage, a style gallery with live preview, Quick Access Toolbar customization and persisted
    /// preferences. The editor logic lives here, outside the Ribbon.
    /// </summary>
    public partial class RibbonExample
    {
        private static readonly string PreferencesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MosaicWpfDemo", "ribbon-preferences.xml");

        private readonly List<RibbonStyleItem> _styles = RibbonStyleItem.CreateDefaults();
        private readonly RibbonDeveloperViewModel _developer = new();
        private Brush? _highlight = new SolidColorBrush(Color.FromRgb(0xFF, 0xF2, 0x00));
        private PreviewState? _preview;
        private bool _updatingFormatState;

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonExample"/> class.
        /// </summary>
        public RibbonExample()
        {
            InitializeComponent();

            ModeCombo.ItemsSource = Enum.GetValues<RibbonLayoutMode>();
            DensityCombo.ItemsSource = Enum.GetValues<UserInterfaceDensity>();
            TabIndicatorAnimationCombo.ItemsSource = Enum.GetValues<RibbonTabIndicatorAnimation>();

            FontFamilyCombo.ItemsSource = new[] { "Segoe UI", "Calibri", "Cambria", "Aptos", "Arial", "Georgia", "Times New Roman", "Verdana", "Consolas" };
            FontSizeCombo.ItemsSource = new[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 36, 48, 72 };
            StylesGallery.ItemsSource = _styles;
            PictureStylesGallery.ItemsSource = RibbonPictureStyle.CreateDefaults();
            RecentList.ItemsSource = new[] { "Quarterly report.docx", "Project plan.docx", "Meeting notes.docx", "Ribbon specification.docx" };

            // Command availability is application state: the document refuses clipboard commands while the
            // harness says so, and every Ribbon presentation (inline, overflow, QAT) disables together.
            foreach (var command in new[] { ApplicationCommands.Cut, ApplicationCommands.Copy, ApplicationCommands.Paste })
            {
                Document.CommandBindings.Add(new CommandBinding(command, null, OnCanExecuteClipboard));
            }

            Document.Document = CreateSampleDocument();
            CommandsEnabledCheck.IsChecked = true;

            // The Developer tab (and the footer message bar) bind to a view model. The DataContext set on the Ribbon
            // flows into every tab, group, drop-down and optional region.
            DemoRibbon.DataContext = _developer;
            _developer.Logged += message => Log($"Developer: {message}");
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            // Default Quick Access Toolbar, then any saved preferences (restoration needs the commands to exist).
            DemoRibbon.AddToQuickAccessToolbar("Paste");
            DemoRibbon.AddToQuickAccessToolbar("Bold");
            DemoRibbon.AddToQuickAccessToolbar("Pictures");

            if (File.Exists(PreferencesPath))
            {
                Report("Restore saved preferences", RibbonSerializer.LoadFromFile(DemoRibbon, PreferencesPath));
            }

            if (DemoRibbon.KeyTips is { } keyTips)
            {
                keyTips.Activated += (_, args) => Log($"Key tip '{args.KeyTip}' → {DescribeElement(args.Target)}");
            }

            InfoText.Text = $"Preferences are saved to {PreferencesPath}.";
            Document.Focus();
            Log("Ribbon ready. Narrow the host width to watch groups reduce, collapse and scroll.");
        }

        #region Harness

        private void HostWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyHostWidth();
        }

        private void ConstrainWidth_Changed(object sender, RoutedEventArgs e)
        {
            ApplyHostWidth();
        }

        private void ApplyHostWidth()
        {
            if (HostBorder == null)
            {
                return;
            }

            if (ConstrainWidthCheck.IsChecked == true)
            {
                HostBorder.Width = HostWidthSlider.Value;
                HostBorder.HorizontalAlignment = HorizontalAlignment.Left;
                HostWidthText.Text = $"{HostWidthSlider.Value:0} px";
            }
            else
            {
                HostBorder.Width = double.NaN;
                HostBorder.HorizontalAlignment = HorizontalAlignment.Stretch;
                HostWidthText.Text = "(auto)";
            }
        }

        private void PictureSelected_Changed(object sender, RoutedEventArgs e)
        {
            // Activating a context reveals its tabs; deactivating it while its tab is selected falls back to an
            // ordinary tab (the Ribbon owns that policy).
            PictureContext.IsActive = PictureSelectedCheck.IsChecked == true;
            Log(PictureContext.IsActive ? "Picture context active" : "Picture context removed");
        }

        private void CommandsEnabled_Changed(object sender, RoutedEventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
            Log(CommandsEnabledCheck.IsChecked == true ? "Clipboard commands available" : "Clipboard commands unavailable — every presentation disables");
        }

        private void OnCanExecuteClipboard(object sender, CanExecuteRoutedEventArgs e)
        {
            if (CommandsEnabledCheck.IsChecked != true)
            {
                e.CanExecute = false;
                e.Handled = true;
            }
        }

        private void MessageBar_Changed(object sender, RoutedEventArgs e)
        {
            if (MessageBarCheck.IsChecked != true)
            {
                DemoRibbon.FooterContent = null;
                return;
            }

            var close = new Button { Content = "Dismiss", Padding = new Thickness(10, 2, 10, 2), Margin = new Thickness(12, 0, 0, 0) };
            close.Click += (_, _) => MessageBarCheck.IsChecked = false;

            var bar = new DockPanel { Margin = new Thickness(6, 0, 6, 6) };
            var text = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = "PROTECTED VIEW  Be careful — files from the Internet can contain viruses. Unless you need to edit, it's safer to stay in Protected View.",
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            DockPanel.SetDock(close, Dock.Right);
            bar.Children.Add(close);
            bar.Children.Add(text);

            DemoRibbon.FooterContent = new Border
            {
                Child = bar,
                Padding = new Thickness(10, 6, 10, 6),
                Background = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xC8, 0x3D)),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(6, 0, 6, 6)
            };
        }

        private void SavePreferences_Click(object sender, RoutedEventArgs e)
        {
            RibbonSerializer.SaveToFile(DemoRibbon, PreferencesPath);
            Log($"Saved preferences ({DemoRibbon.QuickAccessToolbarKeys.Count} QAT commands) to {PreferencesPath}");
        }

        private void RestorePreferences_Click(object sender, RoutedEventArgs e)
        {
            Report("Restore preferences", RibbonSerializer.LoadFromFile(DemoRibbon, PreferencesPath));
        }

        private void RestoreCorrupt_Click(object sender, RoutedEventArgs e)
        {
            Report("Restore malformed XML", RibbonSerializer.Deserialize(DemoRibbon, "<RibbonPreferences version=\"1\"><LayoutMode>Simplified"));

            const string partiallyInvalid = """
                <RibbonPreferences version="1">
                  <LayoutMode>Sideways</LayoutMode>
                  <Density>Spacious</Density>
                  <QuickAccessToolbar location="Below" mode="Visible" showLabels="maybe">
                    <Items>
                      <Command key="Paste" />
                      <Command key="Paste" />
                      <Command key="RemovedInVersion2" />
                      <Command key="Find" />
                    </Items>
                  </QuickAccessToolbar>
                </RibbonPreferences>
                """;
            Report("Restore partially invalid XML", RibbonSerializer.Deserialize(DemoRibbon, partiallyInvalid));
        }

        private void Report(string action, RibbonRestoreResult result)
        {
            Log($"{action}: {(result.Success ? "applied" : "rejected, defaults kept")}");
            foreach (var diagnostic in result.Diagnostics)
            {
                Log("    " + diagnostic);
            }
        }

        #endregion

        #region Ribbon events

        private void DemoRibbon_CommandInvoked(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source)
            {
                Log($"Invoked {DescribeElement(source)}");
            }
        }

        private void DemoRibbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource == DemoRibbon && DemoRibbon.SelectedTab is { } tab)
            {
                Log($"Tab selected: {tab.Header}");
            }

            e.Handled = true;
        }

        private void DemoRibbon_LayoutModeChanged(object? sender, EventArgs e) => Log($"Layout mode: {DemoRibbon.LayoutMode}");

        private void DemoRibbon_IsMinimizedChanged(object? sender, EventArgs e) => Log(DemoRibbon.IsMinimized ? "Ribbon minimized (tabs only)" : "Ribbon pinned");

        private void DemoRibbon_QuickAccessToolbarItemAdding(object? sender, QuickAccessToolbarItemAddingEventArgs e)
        {
            // Demonstrates cancellation: the Share call-to-action stays in the tab row only.
            if (e.Key == "Share")
            {
                e.Cancel = true;
                Log("QAT add of 'Share' was cancelled by the host");
            }
        }

        private void DemoRibbon_QuickAccessToolbarItemAdded(object? sender, QuickAccessToolbarItemEventArgs e) => Log($"QAT added: {e.Key}");

        private void DemoRibbon_QuickAccessToolbarItemRemoved(object? sender, QuickAccessToolbarItemEventArgs e) => Log($"QAT removed: {e.Key}");

        private void Backstage_Closed(object sender, RoutedEventArgs e) => Log("Backstage closed; focus restored");

        #endregion

        #region Commands (application logic)

        private void Launcher_Click(object sender, RoutedEventArgs e)
        {
            var name = e.OriginalSource is RibbonGroup g ? $"{g.Header} dialog launcher" : DescribeElement((DependencyObject)e.OriginalSource);
            MessageBox.Show($"{name}: a dialog would open here.", "Ribbon demo");
        }

        private void PasteTextOnly_Click(object sender, RoutedEventArgs e)
        {
            if (CommandsEnabledCheck.IsChecked == true && Clipboard.ContainsText())
            {
                Document.Selection.Text = Clipboard.GetText();
            }
        }

        private void Highlight_Click(object sender, RoutedEventArgs e)
        {
            Document.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, _highlight);
        }

        private void HighlightColor_Click(object sender, RoutedEventArgs e)
        {
            _highlight = (sender as FrameworkElement)?.Tag is string hex ? new BrushConverter().ConvertFromString(hex) as Brush : null;
            Highlight_Click(sender, e);
        }

        private void FontColor_Click(object sender, RoutedEventArgs e)
        {
            object? brush = (sender as FrameworkElement)?.Tag is string hex ? new BrushConverter().ConvertFromString(hex) : null;
            if (brush != null)
            {
                Document.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            }
            else
            {
                Document.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Document.Foreground);
            }
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

        private void Document_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // Shared state: reflect the selection's formatting in the toggles; every presentation (inline,
            // overflow, Quick Access Toolbar) is bound to these same toggles.
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

        private void StylesGallery_ItemHighlighted(object sender, RoutedPropertyChangedEventArgs<object?> e)
        {
            // Live preview: apply while highlighted, revert when the highlight ends without a commit.
            var paragraph = Document.CaretPosition.Paragraph;
            if (paragraph == null)
            {
                return;
            }

            _preview?.Restore();
            _preview = null;

            if (e.NewValue is RibbonStyleItem style)
            {
                _preview = new PreviewState(paragraph);
                style.ApplyTo(paragraph);
            }
        }

        private void StylesGallery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            if (StylesGallery.SelectedItem is RibbonStyleItem style && Document.CaretPosition.Paragraph is { } paragraph)
            {
                _preview = null;
                style.ApplyTo(paragraph);
                Log($"Style applied once: {style.Name}");
            }
        }

        private void ClearFormatting_Click(object sender, RoutedEventArgs e)
        {
            Document.Selection.ClearAllProperties();
            StylesGallery.SelectedItem = null;
        }

        private void Find_Click(object sender, RoutedEventArgs e) => Log("Find: the navigation pane would open here");

        private void Replace_Click(object sender, RoutedEventArgs e) => Log("Replace: the Find and Replace dialog would open here");

        private void InsertText_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: string text })
            {
                Document.CaretPosition.InsertTextInRun(text);
            }
        }

        private void InsertPicture_Click(object sender, RoutedEventArgs e)
        {
            var picture = new Border
            {
                Width = 180,
                Height = 110,
                CornerRadius = new CornerRadius(4),
                Background = new LinearGradientBrush(Color.FromRgb(0x0F, 0x6C, 0xBD), Color.FromRgb(0x2E, 0x9E, 0x5B), 35),
                Child = new TextBlock
                {
                    Text = "",
                    FontFamily = RibbonIconPresenter.DefaultGlyphFontFamily,
                    FontSize = 40,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            _ = new InlineUIContainer(picture, Document.CaretPosition);

            // Selecting a picture reveals its contextual tab, and Office switches to it.
            PictureSelectedCheck.IsChecked = true;
            DemoRibbon.SelectTab(PictureFormatTab, openWhenMinimized: false);
        }

        private void PictureCommand_Click(object sender, RoutedEventArgs e) => Log($"Picture command: {(sender as RibbonButton)?.Label}");

        private void PictureStylesGallery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            if (PictureStylesGallery.SelectedItem is RibbonPictureStyle style)
            {
                Log($"Picture style: {style.Name}");
            }
        }

        private void TableSizePicker_TableSizeSelected(object? sender, TableSizeSelectedEventArgs e)
        {
            TableMenu.IsDropDownOpen = false;

            var table = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) };
            for (var c = 0; c < e.ColumnCount; c++)
            {
                table.Columns.Add(new TableColumn());
            }

            var group = new TableRowGroup();
            for (var r = 0; r < e.RowCount; r++)
            {
                var row = new TableRow();
                for (var c = 0; c < e.ColumnCount; c++)
                {
                    row.Cells.Add(new TableCell(new Paragraph(new Run(" "))) { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5), Padding = new Thickness(4) });
                }

                group.Rows.Add(row);
            }

            table.RowGroups.Add(group);
            Document.Document.Blocks.Add(table);
            Log($"Inserted a {e.RowCount}×{e.ColumnCount} table");
            Document.Focus();
        }

        private void ViewMode_Checked(object sender, RoutedEventArgs e)
        {
            // Read Mode / Print Layout / Web Layout behave as a mutually exclusive set.
            foreach (var toggle in new[] { ReadModeToggle, PrintLayoutToggle, WebLayoutToggle })
            {
                if (toggle != sender)
                {
                    toggle.IsChecked = false;
                }
            }

            Page.MaxWidth = sender == WebLayoutToggle ? double.PositiveInfinity : sender == ReadModeToggle ? 640 : 820;
        }

        private void ShowOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RibbonCheckBox box)
            {
                Log($"{box.Label}: {(box.IsChecked == true ? "shown" : "hidden")}");
            }
        }

        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: string factorText } && double.TryParse(factorText, NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
            {
                SetZoom(factor);
            }
        }

        private void ZoomText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && double.TryParse(ZoomText.Text.TrimEnd('%'), NumberStyles.Float, CultureInfo.CurrentCulture, out var percent))
            {
                SetZoom(Math.Clamp(percent, 25, 400) / 100.0);
                DemoRibbon.RestoreDocumentFocus();
                e.Handled = true;
            }
        }

        private void SetZoom(double factor)
        {
            PageZoom.ScaleX = PageZoom.ScaleY = factor;
            ZoomText.Text = (factor * 100).ToString("0", CultureInfo.CurrentCulture);
        }

        private void Comments_Click(object sender, RoutedEventArgs e) => Log("Comments pane would open here");

        private void Share_Click(object sender, RoutedEventArgs e) => Log("Share: a sharing dialog would open here");

        private void NewDocument_Click(object sender, RoutedEventArgs e)
        {
            Document.Document = new FlowDocument(new Paragraph(new Run((sender as ContentControl)?.Content as string ?? "New document")));
            Backstage.Close(restoreFocus: true);
            Log("New document");
        }

        private void RecentList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RecentList.SelectedItem is string name)
            {
                Document.Document = new FlowDocument(new Paragraph(new Run($"Opened {name}")));
                Backstage.Close(restoreFocus: true);
                Log($"Opened {name}");
            }
        }

        private void CloseBackstageInCode_Click(object sender, RoutedEventArgs e) => Backstage.IsOpen = false;

        private void Save_Click(object sender, RoutedEventArgs e) => Log("Saved (Backstage closed first, then the command ran)");

        private void SaveAs_Click(object sender, RoutedEventArgs e) => Log("Save As dialog would open here");

        private void Print_Click(object sender, RoutedEventArgs e) => Log("Print preview would open here");

        private void CloseDocument_Click(object sender, RoutedEventArgs e)
        {
            Document.Document = new FlowDocument();
            Log("Document closed");
        }

        #endregion

        #region Helpers

        private static string DescribeElement(DependencyObject element)
        {
            return element switch
            {
                IRibbonControl rc => $"{element.GetType().Name} '{rc.Label ?? rc.Key}'",
                RibbonGroup g => $"group '{g.Header}'",
                RibbonTabHeader h => $"tab '{h.Tab.Header}'",
                RibbonApplicationButton => "File",
                _ => element.GetType().Name
            };
        }

        private void Log(string message)
        {
            if (EventLog == null)
            {
                return;
            }

            EventLog.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
            while (EventLog.Items.Count > 200)
            {
                EventLog.Items.RemoveAt(EventLog.Items.Count - 1);
            }
        }

        private static FlowDocument CreateSampleDocument()
        {
            var doc = new FlowDocument { PagePadding = new Thickness(0), FontFamily = new FontFamily("Segoe UI"), FontSize = 14.667 };
            doc.Blocks.Add(new Paragraph(new Run("Ribbon specification")) { FontSize = 28, FontWeight = FontWeights.Light, Margin = new Thickness(0, 0, 0, 8) });
            doc.Blocks.Add(new Paragraph(new Run(
                "Select text and use Home › Font to make it bold, italic or underlined. The toggles follow the selection, and the same state appears in the Quick Access Toolbar and in Simplified overflow menus.")));
            doc.Blocks.Add(new Paragraph(new Run(
                "Point at a style in Home › Styles to preview it on this paragraph; click to apply it once. Insert › Pictures adds a picture and reveals the Picture Format contextual tab; clear “Picture selected” in the harness to remove the context and watch selection fall back to an ordinary tab.")));
            doc.Blocks.Add(new Paragraph(new Run(
                "Narrow the host width to see groups reduce from right to left, collapse into popup buttons, and finally scroll. Switch to the Simplified ribbon from the display options button (bottom right of the command card) to see single-row layout and overflow menus.")));
            doc.Blocks.Add(new Paragraph(new Run(
                "Press Alt (or F10) for key tips: H for Home, then V for Paste. Escape walks back out one scope at a time. Ctrl+F1 collapses the Ribbon to its tabs.")));
            return doc;
        }

        /// <summary>
        /// Remembers paragraph formatting so a live preview can be reverted.
        /// </summary>
        private sealed class PreviewState
        {
            private readonly Paragraph _paragraph;
            private readonly object _fontSize;
            private readonly object _fontWeight;
            private readonly object _fontStyle;
            private readonly object _foreground;

            public PreviewState(Paragraph paragraph)
            {
                _paragraph = paragraph;
                _fontSize = paragraph.ReadLocalValue(TextElement.FontSizeProperty);
                _fontWeight = paragraph.ReadLocalValue(TextElement.FontWeightProperty);
                _fontStyle = paragraph.ReadLocalValue(TextElement.FontStyleProperty);
                _foreground = paragraph.ReadLocalValue(TextElement.ForegroundProperty);
            }

            public void Restore()
            {
                Set(TextElement.FontSizeProperty, _fontSize);
                Set(TextElement.FontWeightProperty, _fontWeight);
                Set(TextElement.FontStyleProperty, _fontStyle);
                Set(TextElement.ForegroundProperty, _foreground);
            }

            private void Set(DependencyProperty property, object value)
            {
                if (value == DependencyProperty.UnsetValue)
                {
                    _paragraph.ClearValue(property);
                }
                else
                {
                    _paragraph.SetValue(property, value);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// A paragraph style shown in the Styles gallery.
    /// </summary>
    public sealed class RibbonStyleItem
    {
        /// <summary>Gets or sets the style name.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Gets or sets the document font size in DIPs.</summary>
        public double FontSize { get; init; } = 14.667;

        /// <summary>Gets the size used for the gallery sample.</summary>
        public double PreviewSize => Math.Min(20, Math.Max(11, FontSize * 0.75));

        /// <summary>Gets or sets the weight.</summary>
        public FontWeight FontWeight { get; init; } = FontWeights.Normal;

        /// <summary>Gets or sets the style.</summary>
        public FontStyle FontStyle { get; init; } = FontStyles.Normal;

        /// <summary>Gets or sets an optional color; <c>null</c> uses the document color.</summary>
        public Brush? Foreground { get; init; }

        /// <summary>Applies the style to a paragraph.</summary>
        public void ApplyTo(Paragraph paragraph)
        {
            paragraph.FontSize = FontSize;
            paragraph.FontWeight = FontWeight;
            paragraph.FontStyle = FontStyle;
            if (Foreground != null)
            {
                paragraph.Foreground = Foreground;
            }
            else
            {
                paragraph.ClearValue(TextElement.ForegroundProperty);
            }
        }

        internal static List<RibbonStyleItem> CreateDefaults()
        {
            var accent = new SolidColorBrush(Color.FromRgb(0x0F, 0x6C, 0xBD));
            accent.Freeze();
            return new List<RibbonStyleItem>
            {
                new() { Name = "Normal" },
                new() { Name = "No Spacing", FontSize = 13.333 },
                new() { Name = "Heading 1", FontSize = 21.333, Foreground = accent },
                new() { Name = "Heading 2", FontSize = 17.333, Foreground = accent },
                new() { Name = "Title", FontSize = 37.333, FontWeight = FontWeights.Light },
                new() { Name = "Subtitle", FontSize = 14.667, Foreground = Brushes.Gray },
                new() { Name = "Emphasis", FontStyle = FontStyles.Italic },
                new() { Name = "Strong", FontWeight = FontWeights.Bold },
                new() { Name = "Quote", FontStyle = FontStyles.Italic, Foreground = Brushes.Gray },
                new() { Name = "Intense Quote", FontStyle = FontStyles.Italic, Foreground = accent },
                new() { Name = "Subtle Emphasis", FontStyle = FontStyles.Italic, Foreground = Brushes.DimGray },
                new() { Name = "Book Title", FontWeight = FontWeights.Bold, FontStyle = FontStyles.Italic }
            };
        }
    }

    /// <summary>
    /// A picture style shown in the Picture Styles gallery.
    /// </summary>
    public sealed class RibbonPictureStyle
    {
        /// <summary>Gets or sets the style name.</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Gets or sets the picture fill.</summary>
        public Brush Fill { get; init; } = Brushes.SteelBlue;

        /// <summary>Gets or sets the frame color.</summary>
        public Brush Frame { get; init; } = Brushes.White;

        /// <summary>Gets or sets the frame thickness.</summary>
        public Thickness FrameThickness { get; init; }

        /// <summary>Gets or sets the corner radius.</summary>
        public CornerRadius Corner { get; init; }

        internal static List<RibbonPictureStyle> CreateDefaults()
        {
            var fills = new[] { Brushes.SteelBlue, Brushes.SeaGreen, Brushes.IndianRed, Brushes.DarkOrange, Brushes.MediumPurple, Brushes.CadetBlue };
            var list = new List<RibbonPictureStyle>();
            var names = new[] { "Simple Frame", "Rounded", "Thick Frame", "Soft Edge", "Oval", "Bevel" };
            for (var i = 0; i < 18; i++)
            {
                list.Add(new RibbonPictureStyle
                {
                    Name = $"{names[i % names.Length]} {i / names.Length + 1}",
                    Fill = fills[i % fills.Length],
                    Frame = i % 3 == 0 ? Brushes.White : Brushes.Black,
                    FrameThickness = new Thickness(i % 3 + 1),
                    Corner = new CornerRadius(i % names.Length == 1 ? 10 : i % names.Length == 4 ? 26 : 0)
                });
            }

            return list;
        }
    }

    /// <summary>
    /// The view model behind the Developer tab. It knows nothing about the Ribbon: it exposes ordinary commands,
    /// state and collections, and the tab's markup binds to them.
    /// </summary>
    public sealed partial class RibbonDeveloperViewModel : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonDeveloperViewModel"/> class.
        /// </summary>
        public RibbonDeveloperViewModel()
        {
            ExceptionFilters = new ObservableCollection<RibbonExceptionFilter>
            {
                new("Common Language Runtime exceptions", true),
                new("Managed debugging assistants", false),
                new("Win32 exceptions", false),
                new("JavaScript exceptions", false)
            };

            foreach (var filter in ExceptionFilters)
            {
                filter.PropertyChanged += (_, _) => Message = $"Break on “{filter.Name}”: {(filter.IsEnabled ? "on" : "off")}.";
            }

            RecentProjects = new ObservableCollection<RibbonRecentProject>(
                new[] { ("Inventory Service", @"D:\Source\Inventory"), ("Field App", @"D:\Source\FieldApp"), ("Build Tools", @"D:\Source\BuildTools") }
                    .Select(p => new RibbonRecentProject(p.Item1, p.Item2, new RelayCommand(() => Message = $"Opened {p.Item1} from {p.Item2}."))));

            Accents = new[]
            {
                new RibbonAccentItem("Blue", Color.FromRgb(0x0F, 0x6C, 0xBD)),
                new RibbonAccentItem("Teal", Color.FromRgb(0x03, 0x83, 0x87)),
                new RibbonAccentItem("Green", Color.FromRgb(0x10, 0x7C, 0x10)),
                new RibbonAccentItem("Orange", Color.FromRgb(0xCA, 0x50, 0x10)),
                new RibbonAccentItem("Red", Color.FromRgb(0xC5, 0x0F, 0x1F)),
                new RibbonAccentItem("Pink", Color.FromRgb(0xBF, 0x00, 0x77)),
                new RibbonAccentItem("Purple", Color.FromRgb(0x5C, 0x2E, 0x91)),
                new RibbonAccentItem("Gray", Color.FromRgb(0x69, 0x79, 0x7E))
            };
        }

        /// <summary>Occurs whenever the view model reports a message, so the host can log it.</summary>
        public event Action<string>? Logged;

        /// <summary>Gets the build configurations shown in the Config combo box.</summary>
        public IReadOnlyList<string> Configurations { get; } = new[] { "Debug", "Release", "Staging" };

        /// <summary>Gets the exception categories listed in the Break on Exceptions drop-down.</summary>
        public ObservableCollection<RibbonExceptionFilter> ExceptionFilters { get; }

        /// <summary>Gets the projects listed in the Recent drop-down.</summary>
        public ObservableCollection<RibbonRecentProject> RecentProjects { get; }

        /// <summary>Gets the accent colors shown in the gallery.</summary>
        public IReadOnlyList<RibbonAccentItem> Accents { get; }

        /// <summary>Gets or sets the selected build configuration (two-way bound to the combo box).</summary>
        [ObservableProperty]
        public partial string Configuration { get; set; } = "Debug";

        /// <summary>Gets or sets a value indicating whether watch mode is on (two-way bound to a toggle button).</summary>
        [ObservableProperty]
        public partial bool IsWatchMode { get; set; }

        /// <summary>Gets or sets a value indicating whether warnings are errors (two-way bound to a check box).</summary>
        [ObservableProperty]
        public partial bool WarningsAsErrors { get; set; }

        /// <summary>Gets or sets a value indicating whether the debugger breaks on exceptions (a checkable split button).</summary>
        [ObservableProperty]
        public partial bool BreakOnExceptions { get; set; } = true;

        /// <summary>Gets or sets the accent picked in the gallery.</summary>
        [ObservableProperty]
        public partial RibbonAccentItem? SelectedAccent { get; set; }

        /// <summary>Gets or sets the text of the Ribbon's footer message bar; <c>null</c> hides it.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMessage))]
        public partial string? Message { get; set; }

        /// <summary>Gets a value indicating whether the message bar is shown.</summary>
        public bool HasMessage => !string.IsNullOrEmpty(Message);

        partial void OnMessageChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Logged?.Invoke(value);
            }
        }

        partial void OnConfigurationChanged(string value) => Message = $"Configuration: {value}.";

        partial void OnIsWatchModeChanged(bool value) => Message = value ? "Watch mode on: rebuilding when files change." : "Watch mode off.";

        partial void OnWarningsAsErrorsChanged(bool value) => Message = $"Warnings as errors: {(value ? "on" : "off")}.";

        /// <summary>
        /// Builds for a moment. An async relay command reports CanExecute = false while it runs, so the Ribbon
        /// button (and any Quick Access Toolbar or overflow copy) disables itself until the build finishes.
        /// </summary>
        [RelayCommand]
        private async Task BuildAsync()
        {
            Message = $"Building ({Configuration})…";
            await Task.Delay(1500);
            Message = WarningsAsErrors
                ? $"Build failed ({Configuration}): 1 warning treated as an error."
                : $"Build succeeded ({Configuration}).";
        }

        [RelayCommand]
        private void OpenBuildSettings() => Message = "The dialog launcher ran OpenBuildSettingsCommand.";

        [RelayCommand]
        private void BreakModeChanged() => Message = $"Break on exceptions: {(BreakOnExceptions ? "on" : "off")}. The split button toggled before running its command.";

        [RelayCommand]
        private void ApplyAccent(RibbonAccentItem? accent)
        {
            if (accent != null)
            {
                Message = $"Accent “{accent.Name}” applied. The gallery passed the picked item as the command parameter.";
            }
        }

        [RelayCommand]
        private void ResetAccent()
        {
            SelectedAccent = null;
            Message = "Accent reset. Clearing SelectedAccent from code did not run ApplyAccentCommand.";
        }

        [RelayCommand]
        private void Search(string? query)
        {
            Message = string.IsNullOrWhiteSpace(query)
                ? "Type a symbol name, then press Enter."
                : $"Searched for “{query.Trim()}”: {Math.Abs(query.Trim().GetHashCode()) % 12 + 1} matches.";
        }

        [RelayCommand]
        private void Note(string? text) => Message = text;

        [RelayCommand]
        private void DismissMessage() => Message = null;
    }

    /// <summary>
    /// An exception category in the Break on Exceptions drop-down (a checkable, data-bound menu row).
    /// </summary>
    public sealed partial class RibbonExceptionFilter : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonExceptionFilter"/> class.
        /// </summary>
        public RibbonExceptionFilter(string name, bool isEnabled)
        {
            Name = name;
            IsEnabled = isEnabled;
        }

        /// <summary>Gets the category name.</summary>
        public string Name { get; }

        /// <summary>Gets or sets a value indicating whether the debugger breaks on this category.</summary>
        [ObservableProperty]
        public partial bool IsEnabled { get; set; }
    }

    /// <summary>
    /// A recent project. Each item carries its own command, so the menu's container style binds it directly.
    /// </summary>
    public sealed record RibbonRecentProject(string Name, string Path, ICommand Open);

    /// <summary>
    /// An accent color in the Appearance gallery.
    /// </summary>
    public sealed class RibbonAccentItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonAccentItem"/> class.
        /// </summary>
        public RibbonAccentItem(string name, Color color)
        {
            Name = name;
            Brush = new SolidColorBrush(color);
            Brush.Freeze();
        }

        /// <summary>Gets the display name.</summary>
        public string Name { get; }

        /// <summary>Gets the swatch brush.</summary>
        public SolidColorBrush Brush { get; }
    }

    /// <summary>
    /// Inverts a boolean binding.
    /// </summary>
    public sealed class InverseBooleanConverter : IValueConverter
    {
        /// <summary>Gets the shared instance.</summary>
        public static InverseBooleanConverter Instance { get; } = new();

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : value;

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : value;
    }
}
