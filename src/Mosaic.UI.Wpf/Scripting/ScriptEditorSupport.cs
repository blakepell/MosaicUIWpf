/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Xml;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
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
    private readonly Dictionary<string, IReadOnlyList<ScriptSignature>> _signatures = new(StringComparer.Ordinal);
    private ScriptSignaturePopup? _signaturePopup;
    private MosaicThemeMode _signaturePopupTheme;
    private TextAnchor? _callAnchor;
    private SignatureTrigger _pendingSignatureTrigger;
    private bool _isSignatureUpdateQueued;
    private bool _isDisposed;

    /// <summary>
    /// What caused a parameter information update; a later trigger in the same dispatcher turn wins if stronger.
    /// </summary>
    private enum SignatureTrigger
    {
        /// <summary>The caret or text changed: follow the caret while open, never open.</summary>
        Refresh,
        /// <summary>A method completion was committed: open only if the caret is right after its parenthesis.</summary>
        CallStart,
        /// <summary>An opening parenthesis, a comma or Ctrl+Shift+Space: open for any call around the caret.</summary>
        Open
    }

    public ScriptEditorSupport(SyntaxEditor editor, ScriptEnvironment environment)
    {
        _editor = editor;
        _environment = environment;
        editor.TextArea.TextEntered += OnTextEntered;
        editor.TextArea.TextEntering += OnTextEntering;
        editor.TextArea.Caret.PositionChanged += OnCaretOrTextChanged;
        editor.TextArea.DocumentChanged += OnDocumentChanged;
        editor.TextChanged += OnCaretOrTextChanged;
        _themeDescriptor.AddValueChanged(editor, RefreshHighlighting);
        environment.RegistrationsChanged += RefreshHighlighting;
        RefreshHighlighting(null, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the parameter information being shown, or null when it is closed.
    /// </summary>
    internal ScriptSignatureHelp? SignatureHelp => _signaturePopup?.Help;

    public void Dispose()
    {
        _isDisposed = true;
        Close();
        _editor.TextArea.TextEntered -= OnTextEntered;
        _editor.TextArea.TextEntering -= OnTextEntering;
        _editor.TextArea.Caret.PositionChanged -= OnCaretOrTextChanged;
        _editor.TextArea.DocumentChanged -= OnDocumentChanged;
        _editor.TextChanged -= OnCaretOrTextChanged;
        _themeDescriptor.RemoveValueChanged(_editor, RefreshHighlighting);
        _environment.RegistrationsChanged -= RefreshHighlighting;
    }

    /// <summary>
    /// Closes the completion list and the parameter information.
    /// </summary>
    public void Close()
    {
        _window?.Close();
        _signaturePopup?.Close();
    }

    public void ShowCompletion(bool snippets = false)
    {
        int offset = _editor.CaretOffset;
        string prefix = GetIdentifierBefore(offset);
        int start = offset - prefix.Length;
        if (snippets) { Show(ScriptCompletion.GetSnippets(), offset); return; }
        if (start > 0 && _editor.Document.GetCharAt(start - 1) == '.')
        {
            Show(ScriptCompletion.GetMembers(_environment, GetIdentifierBefore(start - 1)), start);
        }
        else
        {
            Show(ScriptCompletion.GetModules(_environment, SyntaxCompletionController.GetWordBefore(_editor.Document, start) == "new"), start);
        }
    }

    /// <summary>
    /// Shows the overloads of the call around the caret, if it has any.
    /// </summary>
    public void ShowSignatureHelp() => UpdateSignatureHelp(SignatureTrigger.Open);

    private void OnTextEntered(object sender, TextCompositionEventArgs e)
    {
        if (e.Text == ".")
        {
            ShowCompletion();
        }
        else if (e.Text == " " && SyntaxCompletionController.GetWordBefore(_editor.Document, _editor.CaretOffset) == "new")
        {
            Show(ScriptCompletion.GetModules(_environment, true), _editor.CaretOffset);
        }
    }

    private string GetIdentifierBefore(int end)
    {
        int start = end;
        while (start > 0)
        {
            char c = _editor.Document.GetCharAt(start - 1);
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '$')
            {
                break;
            }

            start--;
        }
        return _editor.Document.GetText(start, end - start);
    }

    private void OnTextEntering(object sender, TextCompositionEventArgs e)
    {
        if (_editor.IsReadOnly || e.Text.Length != 1)
        {
            return;
        }

        char c = e.Text[0];
        // Queued before the completion commit below can mark the input handled; it runs once the text is in.
        if (c is '(' or ',')
        {
            QueueSignatureHelp(SignatureTrigger.Open);
        }

        if (_window != null && !char.IsLetterOrDigit(c) && c != '_' && c != '$')
        {
            if (_window.CompletionList.SelectedItem != null)
            {
                _window.CompletionList.RequestInsertion(e);
            }
            else
            {
                _window.Close();
            }

            if (e.Handled)
            {
                return;
            }
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

    private void OnCaretOrTextChanged(object? sender, EventArgs e)
    {
        if (_signaturePopup?.IsOpen == true)
        {
            QueueSignatureHelp(SignatureTrigger.Refresh);
        }
    }

    private void OnDocumentChanged(object? sender, EventArgs e) => _signaturePopup?.Close();

    /// <summary>
    /// Coalesces the caret, text and typing notifications of one edit into a single update. Normal priority
    /// runs after the typed text is inserted but before the next render, so the popup never lags a frame.
    /// </summary>
    private void QueueSignatureHelp(SignatureTrigger trigger)
    {
        if (trigger > _pendingSignatureTrigger)
        {
            _pendingSignatureTrigger = trigger;
        }

        if (_isSignatureUpdateQueued)
        {
            return;
        }

        _isSignatureUpdateQueued = true;
        _editor.Dispatcher.InvokeAsync(() =>
        {
            var pending = _pendingSignatureTrigger;
            _pendingSignatureTrigger = SignatureTrigger.Refresh;
            _isSignatureUpdateQueued = false;
            if (!_isDisposed)
            {
                UpdateSignatureHelp(pending);
            }
        }, DispatcherPriority.Normal);
    }

    private void UpdateSignatureHelp(SignatureTrigger trigger)
    {
        bool isOpen = _signaturePopup?.IsOpen == true;
        if (!isOpen && trigger == SignatureTrigger.Refresh)
        {
            return;
        }

        int caret = _editor.CaretOffset;
        var found = ScriptCallParser.Find(_editor.Document.Text, caret, c => GetSignatures(c).Count > 0);
        if (found is not { } call)
        {
            _signaturePopup?.Close();
            return;
        }
        if (!isOpen && trigger == SignatureTrigger.CallStart && call.OpenParenOffset != caret - 1)
        {
            return;
        }

        var popup = GetSignaturePopup();
        var help = popup.Help;
        // The anchor tells a new call apart from the same call shifted by edits above it, keeping a picked overload.
        if (help == null || help.Key != call.Key || _callAnchor is not { IsDeleted: false } anchor || anchor.Offset != call.OpenParenOffset)
        {
            help = new ScriptSignatureHelp(call.Key, GetSignatures(call));
            _callAnchor = _editor.Document.CreateAnchor(call.OpenParenOffset);
        }
        help.Update(call.ArgumentIndex, call.ArgumentCount);
        popup.Show(help, call.NameOffset);
    }

    private IReadOnlyList<ScriptSignature> GetSignatures(ScriptCallContext call)
    {
        if (!_signatures.TryGetValue(call.Key, out var signatures))
        {
            _signatures[call.Key] = signatures = ScriptCompletion.GetSignatures(_environment, call);
        }

        return signatures;
    }

    private ScriptSignaturePopup GetSignaturePopup()
    {
        if (_signaturePopup == null || _signaturePopupTheme != _editor.Theme)
        {
            _signaturePopup?.Close();
            _signaturePopup = new ScriptSignaturePopup(_editor.TextArea, AddThemeResources);
            _signaturePopupTheme = _editor.Theme;
        }
        return _signaturePopup;
    }

    private void Show(IReadOnlyList<ICompletionData> items, int start)
    {
        _window?.Close();
        if (items.Count == 0 || _editor.IsReadOnly)
        {
            return;
        }
        // AvalonEdit leaves the window resizable, which makes Windows draw a resize strip along the top
        // edge of the borderless window; NoResize plus a uniform 1px border matches ApexGate's popup.
        var window = new CompletionWindow(_editor.TextArea)
        {
            StartOffset = start, Width = 300, ResizeMode = ResizeMode.NoResize, BorderThickness = new Thickness(1)
        };
        AddThemeResources(window);
        if (window.TryFindResource("ScriptCompletionListStyle") is Style listStyle)
        {
            window.CompletionList.Style = listStyle;
        }

        window.SetResourceReference(Control.BackgroundProperty, MosaicTheme.ControlTextBackgroundBrush);
        window.SetResourceReference(Control.ForegroundProperty, MosaicTheme.ControlTextForegroundBrush);
        window.SetResourceReference(Control.BorderBrushProperty, MosaicTheme.ControlBorderBrush);
        foreach (var item in items)
        {
            window.CompletionList.CompletionData.Add(item);
        }
        // Runs after the window's own handler has inserted the item: a method leaves the caret inside "()".
        window.CompletionList.InsertionRequested += (_, _) => QueueSignatureHelp(SignatureTrigger.CallStart);
        window.Closed += (_, _) => { if (ReferenceEquals(_window, window)) { _window = null; } };
        _window = window;
        window.Show();
        if (start < _editor.CaretOffset)
        {
            window.CompletionList.SelectItem(_editor.Document.GetText(start, _editor.CaretOffset - start));
        }
    }

    /// <summary>
    /// Merges the theme and script popup resources into a completion window or the parameter information
    /// popup; neither inherits the editor's resource tree.
    /// </summary>
    private void AddThemeResources(FrameworkElement element) => AddThemeResources(element, _editor.Theme);

    internal static void AddThemeResources(FrameworkElement element, MosaicThemeMode theme)
    {
        var dictionaries = element.Resources.MergedDictionaries;
        dictionaries.Add(new ResourceDictionary { Source = ThemeDictionaryUris.Palette });
        dictionaries.Add(new ResourceDictionary { Source = ThemeDictionaryUris.Typography });
        dictionaries.Add(new ResourceDictionary { Source = ThemeDictionaryUris.GetThemeUri(theme) });
        dictionaries.Add(new ResourceDictionary { Source = new Uri("/Mosaic.UI.Wpf;component/Scripting/ScriptCompletionWindow.xaml", UriKind.Relative) });
        dictionaries.Add(CreateCompletionPalette(element, theme));
    }

    /// <summary>
    /// Creates the icon and detail brushes used by ScriptCompletionWindow.xaml for the editor's theme.
    /// The icon colors are the ApexGate script editor's; the popups are rebuilt when the theme changes,
    /// so resolving once here keeps them in step with the theme.
    /// </summary>
    private static ResourceDictionary CreateCompletionPalette(FrameworkElement element, MosaicThemeMode theme)
    {
        var palette = new ResourceDictionary();
        if (theme == MosaicThemeMode.HighContrast)
        {
            foreach (string key in new[] { "Method", "Property", "Class", "Snippet" })
            {
                palette[$"ScriptCompletion{key}Brush"] = SystemColors.WindowTextBrush;
            }

            palette["ScriptCompletionTypeBrush"] = SystemColors.HotTrackBrush;
            palette["ScriptCompletionDetailBrush"] = SystemColors.GrayTextBrush;
            return palette;
        }
        bool light = theme == MosaicThemeMode.Light;
        Add("Method", light ? "#7E6187" : "#4EC9B0");
        Add("Property", light ? "#33479B" : "#569CD6");
        Add("Class", "#7160E8");
        Add("Snippet", light ? "#3B9036" : "#608B4E");
        Add("Type", light ? "#33479B" : "#7AA4FF");
        palette["ScriptCompletionDetailBrush"] = element.TryFindResource(MosaicTheme.ControlTextSecondaryForegroundBrush) as Brush ?? SystemColors.GrayTextBrush;
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
        _signatures.Clear();
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
            foreach (var color in definition.NamedHighlightingColors)
            {
                color.Foreground = new SimpleHighlightingBrush(SystemColors.WindowTextColor);
            }
        }
        AddRule(methods, methodColor);
        AddRule(_environment.Registrations.Keys, moduleColor);
        _editor.SyntaxHighlighting = definition;

        void AddRule(IEnumerable<string> names, Color color)
        {
            string pattern = string.Join("|", names.Select(Regex.Escape));
            if (pattern.Length == 0)
            {
                return;
            }

            definition.MainRuleSet.Rules.Insert(0, new HighlightingRule
            {
                Regex = new Regex(@"(?<![\w$])(?:" + pattern + @")(?![\w$])", RegexOptions.CultureInvariant),
                Color = new HighlightingColor { Foreground = new SimpleHighlightingBrush(color) }
            });
        }
    }
}
