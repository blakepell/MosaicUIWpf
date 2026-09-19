/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Automation;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using Mosaic.UI.Wpf.Themes;

namespace Mosaic.UI.Wpf.Scripting;

/// <summary>
/// The parameter information popup shown while the caret is inside a call's argument list. Every overload
/// is listed, the ones not matching the arguments are dimmed and the parameter being typed is bold. It sits
/// above the caret line so the completion list, which opens below, never covers it.
/// </summary>
/// <remarks>
/// A Popup rather than a CompletionWindowBase: the base window always prefers the space below the line and
/// repositions from private handlers, while Popup placement flips above/below and follows size changes.
/// Rows are built once per call; moving between arguments only toggles IsActive, which the styles in
/// ScriptCompletionWindow.xaml turn into opacity and weight, so nothing is re-created while typing.
/// </remarks>
internal sealed class ScriptSignaturePopup
{
    private readonly TextArea _textArea;
    private readonly Popup _popup;
    private readonly Border _root;
    private readonly StackPanel _rows = new();
    private readonly ScrollViewer _scroller;
    private readonly Border _rule = new();
    private readonly TextBlock _summary = new();
    private readonly TextBlock _parameter = new();
    private readonly InputHandler _input;
    private Window? _window;
    private int _nameOffset;
    private bool _isClosing;

    /// <summary>
    /// Creates the popup for a text area.
    /// </summary>
    /// <param name="textArea">The editor text area the popup follows.</param>
    /// <param name="addResources">Merges the themed resources into the popup's root, which has no parent resource scope.</param>
    public ScriptSignaturePopup(TextArea textArea, Action<FrameworkElement> addResources)
    {
        _textArea = textArea;
        _input = new InputHandler(this);
        _scroller = new ScrollViewer
        {
            Content = _rows, MaxHeight = 220, Focusable = false,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        _rule.SetResourceReference(FrameworkElement.StyleProperty, "ScriptCompletionRuleStyle");
        _summary.SetResourceReference(FrameworkElement.StyleProperty, "ScriptSignatureTextStyle");
        _parameter.SetResourceReference(FrameworkElement.StyleProperty, "ScriptSignatureTextStyle");
        _root = new Border { Child = new StackPanel { Children = { _scroller, _rule, _summary, _parameter } } };
        addResources(_root);
        _root.SetResourceReference(FrameworkElement.StyleProperty, "ScriptSignaturePopupStyle");
        AutomationProperties.SetName(_root, "Parameter information");
        _root.SizeChanged += (_, e) => { if (e.NewSize.Width > _root.MinWidth) { _root.MinWidth = e.NewSize.Width; } };
        _popup = new Popup
        {
            Child = _root, PlacementTarget = textArea.TextView, Placement = PlacementMode.Top,
            AllowsTransparency = true, PopupAnimation = PopupAnimation.Fade, StaysOpen = true, Focusable = false
        };
    }

    /// <summary>
    /// Gets the overloads being shown, or null when closed.
    /// </summary>
    public ScriptSignatureHelp? Help { get; private set; }

    public bool IsOpen => Help != null;

    /// <summary>
    /// Shows or updates the popup for a call; the rows are rebuilt only when the call changes.
    /// </summary>
    /// <param name="help">The overloads and the active overload and parameter.</param>
    /// <param name="nameOffset">The start of the callee text, which the popup aligns with horizontally.</param>
    public void Show(ScriptSignatureHelp help, int nameOffset)
    {
        _nameOffset = nameOffset;
        if (!ReferenceEquals(Help, help))
        {
            if (Help != null)
            {
                Help.PropertyChanged -= OnHelpChanged;
            }

            Help = help;
            help.PropertyChanged += OnHelpChanged;
            BuildRows(help);
        }
        UpdateDetails();
        if (!_popup.IsOpen)
        {
            if (!Reposition()) { Close(); return; }
            Attach();
            _popup.IsOpen = true;
        }
        else if (!Reposition())
        {
            Close();
        }
    }

    public void Close()
    {
        if (_isClosing || Help == null)
        {
            return;
        }

        _isClosing = true;
        try
        {
            _popup.IsOpen = false;
            Help.PropertyChanged -= OnHelpChanged;
            Help = null;
            Detach();
        }
        finally { _isClosing = false; }
    }

    private void Attach()
    {
        _textArea.PushStackedInputHandler(_input);
        _textArea.TextView.ScrollOffsetChanged += OnViewChanged;
        _textArea.TextView.VisualLinesChanged += OnViewChanged;
        _textArea.LostKeyboardFocus += OnLostFocus;
        _window = Window.GetWindow(_textArea);
        if (_window != null)
        {
            _window.LocationChanged += OnWindowMoved;
            _window.Deactivated += OnLostFocus;
        }
    }

    private void Detach()
    {
        _textArea.PopStackedInputHandler(_input);
        _textArea.TextView.ScrollOffsetChanged -= OnViewChanged;
        _textArea.TextView.VisualLinesChanged -= OnViewChanged;
        _textArea.LostKeyboardFocus -= OnLostFocus;
        if (_window != null)
        {
            _window.LocationChanged -= OnWindowMoved;
            _window.Deactivated -= OnLostFocus;
            _window = null;
        }
    }

    private void BuildRows(ScriptSignatureHelp help)
    {
        _rows.Children.Clear();
        _root.MinWidth = 0;
        foreach (var signature in help.Signatures)
        {
            var row = new TextBlock { DataContext = signature };
            row.SetResourceReference(FrameworkElement.StyleProperty, "ScriptSignatureRowStyle");
            if (signature.ReturnType.Length > 0)
            {
                row.Inlines.Add(TypeRun(signature.ReturnType + " "));
            }

            row.Inlines.Add(new Run(signature.Name + "("));
            for (int i = 0; i < signature.Parameters.Count; i++)
            {
                var parameter = signature.Parameters[i];
                if (i > 0)
                {
                    row.Inlines.Add(new Run(", "));
                }

                var span = new Span { DataContext = parameter };
                span.SetResourceReference(FrameworkContentElement.StyleProperty, "ScriptSignatureParameterStyle");
                if (parameter.IsParams)
                {
                    span.Inlines.Add(TypeRun("params "));
                }

                if (parameter.Type.Length > 0)
                {
                    span.Inlines.Add(TypeRun(parameter.Type + " "));
                }

                span.Inlines.Add(new Run(parameter.Name));
                if (parameter.DefaultValue != null)
                {
                    var value = new Run($" = {parameter.DefaultValue}");
                    value.SetResourceReference(TextElement.ForegroundProperty, "ScriptCompletionDetailBrush");
                    span.Inlines.Add(value);
                }
                row.Inlines.Add(span);
            }
            row.Inlines.Add(new Run(")"));
            if (help.Signatures.Count > 1)
            {
                row.Cursor = Cursors.Hand;
                row.MouseLeftButtonDown += (_, e) => { help.Select(signature); e.Handled = true; };
            }
            _rows.Children.Add(row);
        }

        static Run TypeRun(string text)
        {
            var run = new Run(text);
            run.SetResourceReference(TextElement.ForegroundProperty, "ScriptCompletionTypeBrush");
            return run;
        }
    }

    private void OnHelpChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateDetails();
        if (e.PropertyName == nameof(ScriptSignatureHelp.ActiveSignature))
        {
            _rows.Children.OfType<TextBlock>().FirstOrDefault(r => ReferenceEquals(r.DataContext, Help?.ActiveSignature))?.BringIntoView();
        }
    }

    /// <summary>
    /// Shows the active overload's description and the current parameter's description under the rows.
    /// </summary>
    private void UpdateDetails()
    {
        string summary = Help?.ActiveSignature?.Summary ?? string.Empty;
        _summary.Text = summary;
        _summary.Visibility = summary.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        _parameter.Inlines.Clear();
        if (Help?.ActiveParameter is { Description.Length: > 0 } parameter)
        {
            _parameter.Inlines.Add(new Run(parameter.Name) { FontWeight = FontWeights.Bold });
            _parameter.Inlines.Add(new Run(": " + parameter.Description));
            _parameter.Visibility = Visibility.Visible;
        }
        else
        {
            _parameter.Visibility = Visibility.Collapsed;
        }

        _rule.Visibility = _summary.Visibility == Visibility.Visible || _parameter.Visibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Anchors the popup to the caret line at the callee's column; false when the caret line is scrolled out of view.
    /// </summary>
    private bool Reposition()
    {
        var view = _textArea.TextView;
        var document = _textArea.Document;
        if (document == null)
        {
            return false;
        }

        var top = view.GetVisualPosition(_textArea.Caret.Position, VisualYPosition.LineTop) - view.ScrollOffset;
        var bottom = view.GetVisualPosition(_textArea.Caret.Position, VisualYPosition.LineBottom) - view.ScrollOffset;
        if (bottom.Y < 0 || top.Y > view.ActualHeight)
        {
            return false;
        }

        var name = new TextViewPosition(document.GetLocation(Math.Clamp(_nameOffset, 0, document.TextLength)));
        double x = view.GetVisualPosition(name, VisualYPosition.LineTop).X - view.ScrollOffset.X - _root.Padding.Left - _root.BorderThickness.Left;
        var anchor = new Rect(Math.Max(0, x), top.Y, 0, Math.Max(1, bottom.Y - top.Y));
        if (_popup.PlacementRectangle != anchor)
        {
            _popup.PlacementRectangle = anchor;
        }

        return true;
    }

    private void OnViewChanged(object? sender, EventArgs e)
    {
        if (!Reposition())
        {
            Close();
        }
    }

    /// <summary>
    /// Popups do not follow their window, so nudging the offset forces WPF to place it again.
    /// </summary>
    private void OnWindowMoved(object? sender, EventArgs e)
    {
        double offset = _popup.HorizontalOffset;
        _popup.HorizontalOffset = offset + 1;
        _popup.HorizontalOffset = offset;
    }

    private void OnLostFocus(object? sender, EventArgs e) =>
        // Deferred like AvalonEdit's completion window: clicking the completion list briefly activates it.
        _textArea.Dispatcher.InvokeAsync(() =>
        {
            bool ownedWindowActive = _window?.OwnedWindows.Cast<Window>().Any(w => w.IsActive) == true;
            if (!_textArea.IsKeyboardFocusWithin && !ownedWindowActive)
            {
                Close();
            }
        }, DispatcherPriority.Background);

    /// <summary>
    /// Escape closes, Up and Down move between overloads. The completion window's handler is stacked above
    /// this one while it is open, so those keys reach the completion list first.
    /// </summary>
    private sealed class InputHandler(ScriptSignaturePopup owner) : TextAreaStackedInputHandler(owner._textArea)
    {
        public override void Detach() => owner.Close();

        public override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                owner.Close();
                e.Handled = true;
            }
            else if (e.Key is Key.Up or Key.Down && Keyboard.Modifiers == ModifierKeys.None && owner.Help is { Signatures.Count: > 1 } help)
            {
                help.Cycle(e.Key == Key.Up ? -1 : 1);
                e.Handled = true;
            }
        }
    }
}
