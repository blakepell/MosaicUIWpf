/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Xml;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;

namespace Mosaic.UI.Wpf.Scripting;

internal sealed class ScriptEditorSupport : IDisposable
{
    private readonly SyntaxEditor _editor;
    private readonly ScriptEnvironment _environment;
    private CompletionWindow? _window;
    private readonly DependencyPropertyDescriptor _themeDescriptor = DependencyPropertyDescriptor.FromProperty(SyntaxEditor.ThemeProperty, typeof(SyntaxEditor));

    public ScriptEditorSupport(SyntaxEditor editor, ScriptEnvironment environment)
    {
        _editor = editor;
        _environment = environment;
        editor.TextArea.TextEntered += OnTextEntered;
        editor.TextArea.TextEntering += OnTextEntering;
        _themeDescriptor.AddValueChanged(editor, RefreshHighlighting);
        environment.RegistrationsChanged += RefreshHighlighting;
        RefreshHighlighting(null, EventArgs.Empty);
    }

    public void Dispose()
    {
        Close();
        _editor.TextArea.TextEntered -= OnTextEntered;
        _editor.TextArea.TextEntering -= OnTextEntering;
        _themeDescriptor.RemoveValueChanged(_editor, RefreshHighlighting);
        _environment.RegistrationsChanged -= RefreshHighlighting;
    }

    public void Close() => _window?.Close();

    public void ShowCompletion(bool snippets = false)
    {
        int offset = _editor.CaretOffset;
        string prefix = GetIdentifierBefore(offset);
        int start = offset - prefix.Length;
        if (snippets) { Show(ScriptCompletion.GetSnippets(), offset); return; }
        if (start > 0 && _editor.Document.GetCharAt(start - 1) == '.')
            Show(ScriptCompletion.GetMembers(_environment, GetIdentifierBefore(start - 1)), start);
        else
            Show(ScriptCompletion.GetModules(_environment, SyntaxCompletionController.GetWordBefore(_editor.Document, start) == "new"), start);
    }

    private void OnTextEntered(object sender, TextCompositionEventArgs e)
    {
        if (e.Text == ".") ShowCompletion();
        else if (e.Text == " " && SyntaxCompletionController.GetWordBefore(_editor.Document, _editor.CaretOffset) == "new")
            Show(ScriptCompletion.GetModules(_environment, true), _editor.CaretOffset);
    }

    private string GetIdentifierBefore(int end)
    {
        int start = end;
        while (start > 0)
        {
            char c = _editor.Document.GetCharAt(start - 1);
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '$') break;
            start--;
        }
        return _editor.Document.GetText(start, end - start);
    }

    private void OnTextEntering(object sender, TextCompositionEventArgs e)
    {
        if (_editor.IsReadOnly || e.Text.Length != 1) return;
        char c = e.Text[0];
        if (_window != null && !char.IsLetterOrDigit(c) && c != '_' && c != '$')
        {
            if (_window.CompletionList.SelectedItem != null) _window.CompletionList.RequestInsertion(e);
            else Close();
            if (e.Handled) return;
        }
        int offset = _editor.CaretOffset;
        if (c is ')' or ']' or '}' && offset < _editor.Document.TextLength && _editor.Document.GetCharAt(offset) == c)
        {
            _editor.CaretOffset++;
            e.Handled = true;
        }
        else if (c is '(' or '[' or '{')
        {
            string closing = c == '(' ? ")" : c == '[' ? "]" : "}";
            int start = _editor.SelectionStart;
            string selected = _editor.SelectedText;
            _editor.Document.Replace(start, _editor.SelectionLength, c + selected + closing);
            _editor.Select(start + 1, selected.Length);
            e.Handled = true;
        }
    }

    private void Show(IReadOnlyList<ICompletionData> items, int start)
    {
        Close();
        if (items.Count == 0 || _editor.IsReadOnly) return;
        // AvalonEdit leaves the window resizable, which makes Windows draw a resize strip along the top
        // edge of the borderless window; NoResize plus a uniform 1px border matches ApexGate's popup.
        var window = new CompletionWindow(_editor.TextArea)
        {
            StartOffset = start, Width = 300, ResizeMode = ResizeMode.NoResize, BorderThickness = new Thickness(1)
        };
        // Completion windows do not inherit the editor's resource tree.
        window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = ThemeDictionaryUris.Palette });
        window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = ThemeDictionaryUris.Typography });
        window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = ThemeDictionaryUris.GetThemeUri(_editor.Theme) });
        window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/Mosaic.UI.Wpf;component/Scripting/ScriptCompletionWindow.xaml", UriKind.Relative) });
        window.Resources.MergedDictionaries.Add(CreateCompletionPalette(window));
        if (window.TryFindResource("ScriptCompletionListStyle") is Style listStyle) window.CompletionList.Style = listStyle;
        window.SetResourceReference(Control.BackgroundProperty, MosaicTheme.ControlTextBackgroundBrush);
        window.SetResourceReference(Control.ForegroundProperty, MosaicTheme.ControlTextForegroundBrush);
        window.SetResourceReference(Control.BorderBrushProperty, MosaicTheme.ControlBorderBrush);
        foreach (var item in items) window.CompletionList.CompletionData.Add(item);
        window.Closed += (_, _) => { if (ReferenceEquals(_window, window)) _window = null; };
        _window = window;
        window.Show();
        if (start < _editor.CaretOffset) window.CompletionList.SelectItem(_editor.Document.GetText(start, _editor.CaretOffset - start));
    }

    /// <summary>
    /// Creates the icon and detail brushes used by ScriptCompletionWindow.xaml for the editor's theme.
    /// The icon colors are the ApexGate script editor's; the window is rebuilt on every open and
    /// closed on theme changes, so resolving once here keeps it in step with the theme.
    /// </summary>
    private ResourceDictionary CreateCompletionPalette(CompletionWindow window)
    {
        var palette = new ResourceDictionary();
        if (_editor.Theme == MosaicThemeMode.HighContrast)
        {
            foreach (string key in new[] { "Method", "Property", "Class", "Snippet" }) palette[$"ScriptCompletion{key}Brush"] = SystemColors.WindowTextBrush;
            palette["ScriptCompletionTypeBrush"] = SystemColors.HotTrackBrush;
            palette["ScriptCompletionDetailBrush"] = SystemColors.GrayTextBrush;
            return palette;
        }
        bool light = _editor.Theme == MosaicThemeMode.Light;
        Add("Method", light ? "#7E6187" : "#4EC9B0");
        Add("Property", light ? "#33479B" : "#569CD6");
        Add("Class", "#7160E8");
        Add("Snippet", light ? "#3B9036" : "#608B4E");
        Add("Type", light ? "#33479B" : "#7AA4FF");
        palette["ScriptCompletionDetailBrush"] = window.TryFindResource(MosaicTheme.ControlTextSecondaryForegroundBrush) as Brush ?? SystemColors.GrayTextBrush;
        return palette;

        void Add(string key, string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze();
            palette[$"ScriptCompletion{key}Brush"] = brush;
        }
    }

    private void RefreshHighlighting(object? sender, EventArgs e)
    {
        _editor.Dispatcher.VerifyAccess();
        Close();
        string suffix = _editor.Theme == MosaicThemeMode.Light ? "Light" : "Dark";
        using var stream = typeof(ScriptEditorSupport).Assembly.GetManifestResourceStream($"Mosaic.UI.Wpf.Scripting.Assets.Js{suffix}.xshd")!;
        using var reader = XmlReader.Create(stream);
        // Never mutate SyntaxEditor's cached definitions: each environment has different aliases.
        var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        var methods = _environment.Registrations.Values.SelectMany(r => r.Type.GetMethods())
            .Where(m => !m.IsSpecialName && ScriptCompletion.Visible(m)).Select(m => m.Name).Distinct();
        Color methodColor = suffix == "Light" ? Color.FromRgb(121, 94, 38) : Color.FromRgb(220, 220, 142);
        Color moduleColor = suffix == "Light" ? Color.FromRgb(38, 127, 153) : Color.FromRgb(78, 201, 176);
        if (_editor.Theme == MosaicThemeMode.HighContrast)
        {
            methodColor = moduleColor = SystemColors.WindowTextColor;
            foreach (var color in definition.NamedHighlightingColors) color.Foreground = new SimpleHighlightingBrush(SystemColors.WindowTextColor);
        }
        AddRule(methods, methodColor);
        AddRule(_environment.Registrations.Keys, moduleColor);
        _editor.SyntaxHighlighting = definition;

        void AddRule(IEnumerable<string> names, Color color)
        {
            string pattern = string.Join("|", names.Select(Regex.Escape));
            if (pattern.Length == 0) return;
            definition.MainRuleSet.Rules.Insert(0, new HighlightingRule
            {
                Regex = new Regex(@"(?<![\w$])(?:" + pattern + @")(?![\w$])", RegexOptions.CultureInvariant),
                Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(color) }
            });
        }
    }
}
