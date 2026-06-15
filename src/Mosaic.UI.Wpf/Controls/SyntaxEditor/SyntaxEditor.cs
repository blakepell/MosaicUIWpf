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
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Mosaic.UI.Wpf.Themes;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A code editor built on AvalonEdit's <see cref="TextEditor"/> that integrates with the Mosaic
    /// theming system and provides bundled, theme-aware syntax highlighting selected via the
    /// <see cref="Language"/> property.
    /// </summary>
    /// <remarks>
    /// The editor defaults to Consolas 12 with line numbers enabled. Colors for the editing surface
    /// (background, foreground, line numbers, selection, current line) are derived from the Mosaic
    /// theme indicated by <see cref="Theme"/>. When <see cref="FollowGlobalTheme"/> is <c>true</c>
    /// (the default) the editor also tracks <see cref="ThemeManager.ThemeChanged"/>.
    /// </remarks>
    [DefaultEvent(nameof(ContextMenuRequested))]
    [DefaultProperty(nameof(Language))]
    public class SyntaxEditor : TextEditor
    {
        private const string XshdResourceFormat = "Mosaic.UI.Wpf.Assets.{0}.{1}.xshd";

        // Cache parsed highlighting definitions keyed by "Base|ThemeSuffix" so repeated theme/language
        // toggles do not re-parse the embedded xshd each time.
        private static readonly Dictionary<string, IHighlightingDefinition> HighlightingCache = new();

        // Cache the per-theme resource dictionaries so we resolve the theme color tokens only once.
        private static readonly Dictionary<MosaicThemeMode, ResourceDictionary> ThemeDictionaryCache = new();

        private bool _subscribedToGlobalTheme;

        #region Routed Commands

        /// <summary>
        /// Command that re-formats (pretty-prints) the JSON document with indentation.
        /// </summary>
        public static readonly RoutedUICommand FormatJsonCommand = new("Format JSON", nameof(FormatJsonCommand), typeof(SyntaxEditor));

        /// <summary>
        /// Command that minifies (compacts) the JSON document onto a single line.
        /// </summary>
        public static readonly RoutedUICommand MinifyJsonCommand = new("Minify JSON", nameof(MinifyJsonCommand), typeof(SyntaxEditor));

        /// <summary>
        /// Command that validates the JSON document and reports the result.
        /// </summary>
        public static readonly RoutedUICommand ValidateJsonCommand = new("Validate JSON", nameof(ValidateJsonCommand), typeof(SyntaxEditor));

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="ContextMenuRequested"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ContextMenuRequestedEvent = EventManager.RegisterRoutedEvent(
            nameof(ContextMenuRequested),
            RoutingStrategy.Bubble,
            typeof(EventHandler<SyntaxEditorContextMenuEventArgs>),
            typeof(SyntaxEditor));

        /// <summary>
        /// Raised after the editor has populated the standard and language-specific context menu items
        /// but before the menu is shown, allowing consumers to customize the menu for the current file type.
        /// </summary>
        public event EventHandler<SyntaxEditorContextMenuEventArgs> ContextMenuRequested
        {
            add => this.AddHandler(ContextMenuRequestedEvent, value);
            remove => this.RemoveHandler(ContextMenuRequestedEvent, value);
        }

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Theme"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(
            nameof(Theme),
            typeof(MosaicThemeMode),
            typeof(SyntaxEditor),
            new FrameworkPropertyMetadata(MosaicThemeMode.Light, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnThemeChanged));

        /// <summary>
        /// Gets or sets the Mosaic theme that drives the editor's surface colors and the variant of the
        /// syntax highlighting definition that is loaded.
        /// </summary>
        [Category("Mosaic")]
        [Description("The Mosaic theme that drives the editor's surface colors and syntax highlighting variant.")]
        public MosaicThemeMode Theme
        {
            get => (MosaicThemeMode)this.GetValue(ThemeProperty);
            set => this.SetValue(ThemeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Language"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LanguageProperty = DependencyProperty.Register(
            nameof(Language),
            typeof(SyntaxLanguage),
            typeof(SyntaxEditor),
            new FrameworkPropertyMetadata(SyntaxLanguage.None, OnLanguageChanged));

        /// <summary>
        /// Gets or sets the language whose bundled syntax highlighting definition should be applied.
        /// </summary>
        [Category("Mosaic")]
        [Description("The language whose bundled syntax highlighting definition should be applied.")]
        public SyntaxLanguage Language
        {
            get => (SyntaxLanguage)this.GetValue(LanguageProperty);
            set => this.SetValue(LanguageProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FollowGlobalTheme"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FollowGlobalThemeProperty = DependencyProperty.Register(
            nameof(FollowGlobalTheme),
            typeof(bool),
            typeof(SyntaxEditor),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the editor automatically tracks
        /// <see cref="ThemeManager.ThemeChanged"/> and updates its <see cref="Theme"/> accordingly.
        /// Defaults to <c>true</c>. Setting <see cref="Theme"/> directly always overrides the current value.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the editor automatically tracks the global ThemeManager theme.")]
        public bool FollowGlobalTheme
        {
            get => (bool)this.GetValue(FollowGlobalThemeProperty);
            set => this.SetValue(FollowGlobalThemeProperty, value);
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SyntaxEditor"/> class.
        /// </summary>
        public SyntaxEditor()
        {
            this.FontFamily = new FontFamily("Consolas");
            this.FontSize = 12;
            this.ShowLineNumbers = true;
            this.Options.HighlightCurrentLine = true;

            this.CommandBindings.Add(new CommandBinding(FormatJsonCommand, (_, _) => this.FormatJson()));
            this.CommandBindings.Add(new CommandBinding(MinifyJsonCommand, (_, _) => this.MinifyJson()));
            this.CommandBindings.Add(new CommandBinding(ValidateJsonCommand, (_, _) => this.ValidateJson()));

            this.ContextMenu = new ContextMenu();
            this.ContextMenuOpening += this.OnContextMenuOpening;
            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;

            this.ApplyTheme();
            this.ReloadHighlighting();
        }

        private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SyntaxEditor editor)
            {
                editor.ApplyTheme();
                editor.ReloadHighlighting();
            }
        }

        private static void OnLanguageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as SyntaxEditor)?.ReloadHighlighting();
        }

        #region Global theme tracking

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.FollowGlobalTheme && !_subscribedToGlobalTheme)
            {
                ThemeManager.ThemeChanged += this.OnGlobalThemeChanged;
                _subscribedToGlobalTheme = true;

                // Adopt the current global theme (if one can be located) on first load.
                var current = TryGetGlobalTheme();
                if (current.HasValue)
                {
                    this.Theme = current.Value;
                }
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_subscribedToGlobalTheme)
            {
                ThemeManager.ThemeChanged -= this.OnGlobalThemeChanged;
                _subscribedToGlobalTheme = false;
            }
        }

        private void OnGlobalThemeChanged(object? sender, MosaicThemeMode theme)
        {
            if (this.FollowGlobalTheme)
            {
                this.Theme = theme;
            }
        }

        /// <summary>
        /// Attempts to locate a <see cref="ThemeManager"/> in the application's merged resource
        /// dictionaries so the editor can adopt the current theme without explicit wiring.
        /// </summary>
        private static MosaicThemeMode? TryGetGlobalTheme()
        {
            var app = Application.Current;
            if (app?.Resources == null)
            {
                return null;
            }

            foreach (var dictionary in app.Resources.MergedDictionaries)
            {
                if (dictionary is ThemeManager manager)
                {
                    return manager.Theme;
                }
            }

            return null;
        }

        #endregion

        #region Theming

        /// <summary>
        /// Resolves the Mosaic theme color tokens for the current <see cref="Theme"/> and applies them to
        /// the editor's background, foreground, line numbers, selection, current line, and border.
        /// </summary>
        private void ApplyTheme()
        {
            var dictionary = GetThemeDictionary(this.Theme);
            if (dictionary == null)
            {
                return;
            }

            var background = ResolveColor(dictionary, MosaicTheme.ControlTextBackgroundColor, MosaicTheme.ControlBackgroundColor, Colors.White);
            var foreground = ResolveColor(dictionary, MosaicTheme.ControlTextForegroundColor, MosaicTheme.ControlForegroundColor, Colors.Black);
            var lineNumbers = ResolveColor(dictionary, MosaicTheme.PlaceholderTextColor, null, Colors.Gray);
            var selectionBg = ResolveColor(dictionary, MosaicTheme.SelectionBackgroundColor, MosaicTheme.AccentColor, Colors.RoyalBlue);
            var selectionFg = ResolveColor(dictionary, MosaicTheme.SelectionForegroundColor, null, Colors.White);
            var border = ResolveColor(dictionary, MosaicTheme.ControlBorderColor, null, Colors.Gray);
            var hover = ResolveColor(dictionary, MosaicTheme.ControlHoverBackgroundColor, null, lineNumbers);

            this.Background = new SolidColorBrush(background);
            this.Foreground = new SolidColorBrush(foreground);
            this.LineNumbersForeground = new SolidColorBrush(lineNumbers);

            // A translucent selection keeps the underlying text legible while honoring the theme accent.
            this.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(0x99, selectionBg.R, selectionBg.G, selectionBg.B));
            this.TextArea.SelectionForeground = new SolidColorBrush(selectionFg);
            this.TextArea.SelectionBorder = new Pen(new SolidColorBrush(selectionBg), 1.0);

            this.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(Color.FromArgb(0x28, hover.R, hover.G, hover.B));
            this.TextArea.TextView.CurrentLineBorder = new Pen(new SolidColorBrush(Color.FromArgb(0x40, hover.R, hover.G, hover.B)), 1.0);

            this.BorderBrush = new SolidColorBrush(border);
            this.BorderThickness = new Thickness(1);
        }

        private static ResourceDictionary? GetThemeDictionary(MosaicThemeMode theme)
        {
            if (ThemeDictionaryCache.TryGetValue(theme, out var cached))
            {
                return cached;
            }

            try
            {
                var dictionary = new ResourceDictionary { Source = ThemeDictionaryUris.GetThemeUri(theme) };
                ThemeDictionaryCache[theme] = dictionary;
                return dictionary;
            }
            catch
            {
                return null;
            }
        }

        private static Color ResolveColor(ResourceDictionary dictionary, ComponentResourceKey primaryKey, ComponentResourceKey? fallbackKey, Color defaultColor)
        {
            if (dictionary.Contains(primaryKey) && dictionary[primaryKey] is Color primary)
            {
                return primary;
            }

            if (fallbackKey != null && dictionary.Contains(fallbackKey) && dictionary[fallbackKey] is Color fallback)
            {
                return fallback;
            }

            return defaultColor;
        }

        #endregion

        #region Syntax highlighting

        /// <summary>
        /// Loads (or clears) the syntax highlighting definition for the current <see cref="Language"/> and
        /// <see cref="Theme"/>.
        /// </summary>
        private void ReloadHighlighting()
        {
            string? baseName = SyntaxLanguageMap.GetResourceBaseName(this.Language);
            if (baseName == null)
            {
                this.SyntaxHighlighting = null;
                return;
            }

            // HighContrast falls back to the Dark highlighting variant.
            string themeSuffix = this.Theme == MosaicThemeMode.Light ? "Light" : "Dark";
            string cacheKey = $"{baseName}|{themeSuffix}";

            try
            {
                if (!HighlightingCache.TryGetValue(cacheKey, out var definition))
                {
                    string resourceName = string.Format(XshdResourceFormat, baseName, themeSuffix);
                    using var stream = typeof(SyntaxEditor).Assembly.GetManifestResourceStream(resourceName);
                    if (stream == null)
                    {
                        this.SyntaxHighlighting = null;
                        return;
                    }

                    using var reader = XmlReader.Create(stream);
                    definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    HighlightingCache[cacheKey] = definition;
                }

                this.SyntaxHighlighting = definition;
            }
            catch
            {
                this.SyntaxHighlighting = null;
            }
        }

        /// <summary>
        /// Sets <see cref="Language"/> based on a file path or extension (e.g. <c>".cs"</c> or
        /// <c>"data.json"</c>). Unrecognized extensions set <see cref="SyntaxLanguage.None"/>.
        /// </summary>
        /// <param name="pathOrExtension">The file path or extension to inspect.</param>
        public void SetSyntaxFromFileExtension(string? pathOrExtension)
        {
            this.Language = SyntaxLanguageMap.FromExtension(pathOrExtension);
        }

        #endregion

        #region Context menu

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var menu = this.ContextMenu;
            if (menu == null)
            {
                return;
            }

            menu.Items.Clear();

            menu.Items.Add(CreateItem("Cut", ApplicationCommands.Cut));
            menu.Items.Add(CreateItem("Copy", ApplicationCommands.Copy));
            menu.Items.Add(CreateItem("Paste", ApplicationCommands.Paste));
            menu.Items.Add(new Separator());
            menu.Items.Add(CreateItem("Select All", ApplicationCommands.SelectAll));

            AddLanguageItems(menu);

            // Allow consumers to customize the populated menu before it is displayed.
            this.RaiseEvent(new SyntaxEditorContextMenuEventArgs(ContextMenuRequestedEvent, this, menu, this.Language));
        }

        private void AddLanguageItems(ContextMenu menu)
        {
            if (this.Language == SyntaxLanguage.Json)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(CreateItem("Format JSON", FormatJsonCommand));
                menu.Items.Add(CreateItem("Minify JSON", MinifyJsonCommand));
                menu.Items.Add(CreateItem("Validate JSON", ValidateJsonCommand));
            }
        }

        private MenuItem CreateItem(string header, ICommand command)
        {
            return new MenuItem
            {
                Header = header,
                Command = command,
                CommandTarget = this
            };
        }

        #endregion

        #region JSON operations

        private void FormatJson()
        {
            this.TransformJson(static node => node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private void MinifyJson()
        {
            this.TransformJson(static node => node.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
        }

        private void TransformJson(Func<JsonNode, string> transform)
        {
            string text = this.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                var options = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
                var node = JsonNode.Parse(text, documentOptions: options);
                if (node != null)
                {
                    this.Document.Text = transform(node);
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"The document is not valid JSON and could not be transformed.\r\n\r\n{ex.Message}", "Invalid JSON", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ValidateJson()
        {
            string text = this.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("The document is empty.", "Validate JSON", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var options = new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
                using var _ = JsonDocument.Parse(text, options);
                MessageBox.Show("The JSON is valid.", "Validate JSON", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"The JSON is not valid.\r\n\r\n{ex.Message}", "Validate JSON", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
