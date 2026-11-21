/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using Mosaic.UI.Wpf.Themes;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Attached properties for Ribbon key tips.
    /// </summary>
    public static class RibbonKeyTipService
    {
        /// <summary>
        /// Identifies the KeyTip attached property: the key sequence (one or more letters or digits) that
        /// activates an element while key tips are shown. When unset a unique tip is generated from the label.
        /// </summary>
        public static readonly DependencyProperty KeyTipProperty = DependencyProperty.RegisterAttached(
            "KeyTip", typeof(string), typeof(RibbonKeyTipService), new FrameworkPropertyMetadata(null));

        /// <summary>Gets the assigned key tip.</summary>
        public static string? GetKeyTip(DependencyObject element) => (string?)element.GetValue(KeyTipProperty);

        /// <summary>Sets the key tip.</summary>
        public static void SetKeyTip(DependencyObject element, string? value) => element.SetValue(KeyTipProperty, value);

        /// <summary>
        /// Generates key tips for a scope. Assigned tips are kept (duplicates are reported and regenerated);
        /// generated tips are unique and never a prefix of another tip in the scope, so typing a tip never
        /// activates a different target first.
        /// </summary>
        /// <param name="targets">The scope's targets, in visual order, with their labels and assigned tips.</param>
        /// <param name="diagnostics">Receives collision diagnostics.</param>
        /// <returns>The tip for each target, in the same order.</returns>
        public static IReadOnlyList<string> GenerateKeyTips(IReadOnlyList<(string? Label, string? Assigned, bool IsNumbered)> targets, ICollection<string>? diagnostics = null)
        {
            var result = new string?[targets.Count];
            var used = new List<string>();

            bool Conflicts(string candidate) => used.Any(u => u.StartsWith(candidate, StringComparison.Ordinal) || candidate.StartsWith(u, StringComparison.Ordinal));

            // Assigned tips first.
            for (var i = 0; i < targets.Count; i++)
            {
                var assigned = targets[i].Assigned?.Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(assigned))
                {
                    continue;
                }

                if (Conflicts(assigned))
                {
                    diagnostics?.Add($"Key tip '{assigned}' for '{targets[i].Label}' collides with another tip in the same scope; a tip was generated instead.");
                    continue;
                }

                result[i] = assigned;
                used.Add(assigned);
            }

            // Numbered targets (Quick Access Toolbar): 1–9, then 09, 08, ... as Office does.
            var number = 1;
            for (var i = 0; i < targets.Count; i++)
            {
                if (result[i] != null || !targets[i].IsNumbered)
                {
                    continue;
                }

                string candidate;
                do
                {
                    candidate = number <= 9 ? number.ToString() : "0" + (19 - number).ToString();
                    number++;
                }
                while (Conflicts(candidate) && number < 19);

                if (!Conflicts(candidate))
                {
                    result[i] = candidate;
                    used.Add(candidate);
                }
            }

            for (var i = 0; i < targets.Count; i++)
            {
                if (result[i] != null)
                {
                    continue;
                }

                var letters = new string((targets[i].Label ?? string.Empty).ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
                var chosen = Candidates(letters).FirstOrDefault(c => !Conflicts(c));
                if (chosen != null)
                {
                    result[i] = chosen;
                    used.Add(chosen);
                }
            }

            return result.Select(r => r ?? string.Empty).ToList();
        }

        private static IEnumerable<string> Candidates(string letters)
        {
            if (letters.Length > 0)
            {
                yield return letters[0].ToString();
                for (var j = 1; j < letters.Length; j++)
                {
                    yield return $"{letters[0]}{letters[j]}";
                }

                for (var j = 1; j < letters.Length; j++)
                {
                    yield return letters[j].ToString();
                }

                for (var d = 1; d <= 9; d++)
                {
                    yield return $"{letters[0]}{d}";
                }
            }

            for (var a = 'A'; a <= 'Z'; a++)
            {
                for (var b = 'A'; b <= 'Z'; b++)
                {
                    yield return $"{a}{b}";
                }
            }
        }
    }

    /// <summary>
    /// Runs Ribbon key tip mode for one window: Alt or F10 shows tips for the root scope (File, tabs,
    /// Quick Access Toolbar); typing a tip activates its target and, for tabs and drop-downs, opens a
    /// nested scope; Escape returns to the parent scope. Application shortcuts are left untouched.
    /// </summary>
    public sealed class KeyTipManager : IDisposable
    {
        private readonly Ribbon _ribbon;
        private readonly Window _window;
        private readonly Stack<Scope> _scopes = new();
        private readonly List<KeyTipAdorner> _adorners = new();
        private bool _altDown;
        private bool _otherKeyWhileAlt;
        private string _typed = string.Empty;

        private sealed class Scope
        {
            public required UIElement Root;
            public Action? OnPop;
            public List<(UIElement Target, string Tip)> Tips = new();
        }

        /// <summary>
        /// Occurs when key tip mode starts.
        /// </summary>
        public event EventHandler? Entered;

        /// <summary>
        /// Occurs when key tip mode ends.
        /// </summary>
        public event EventHandler? Exited;

        /// <summary>
        /// Occurs when a key tip activates its target.
        /// </summary>
        public event EventHandler<KeyTipActivatedEventArgs>? Activated;

        /// <summary>
        /// Gets the collision diagnostics of the scopes shown so far.
        /// </summary>
        public List<string> Diagnostics { get; } = new();

        /// <summary>
        /// Gets a value indicating whether key tips are shown.
        /// </summary>
        public bool IsActive => _scopes.Count > 0;

        internal KeyTipManager(Ribbon ribbon, Window window)
        {
            _ribbon = ribbon;
            _window = window;
            _window.PreviewKeyDown += OnPreviewKeyDown;
            _window.PreviewKeyUp += OnPreviewKeyUp;
            _window.PreviewTextInput += OnPreviewTextInput;
            _window.PreviewMouseDown += OnPreviewMouseDown;
            _window.LocationChanged += OnWindowMoved;
            _window.SizeChanged += OnWindowMoved;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Exit();
            _window.PreviewKeyDown -= OnPreviewKeyDown;
            _window.PreviewKeyUp -= OnPreviewKeyUp;
            _window.PreviewTextInput -= OnPreviewTextInput;
            _window.PreviewMouseDown -= OnPreviewMouseDown;
            _window.LocationChanged -= OnWindowMoved;
            _window.SizeChanged -= OnWindowMoved;
        }

        private bool CanRun => _ribbon.AreKeyTipsEnabled && _ribbon.IsVisible && !_ribbon.IsCollapsed && !_ribbon.IsBackstageOpen;

        /// <summary>
        /// Shows the root scope's key tips.
        /// </summary>
        public void Enter()
        {
            if (!CanRun)
            {
                return;
            }

            Exit();
            Push(new Scope { Root = _ribbon });
            Entered?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Hides all key tips and leaves key tip mode.
        /// </summary>
        public void Exit()
        {
            if (_scopes.Count == 0)
            {
                return;
            }

            ClearAdorners();
            _scopes.Clear();
            _typed = string.Empty;
            Exited?.Invoke(this, EventArgs.Empty);
        }

        private void OnWindowMoved(object? sender, EventArgs e) => Exit();

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) => Exit();

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Typed key tips must never reach the document.
            if (IsActive)
            {
                e.Handled = true;
            }
        }

        private static char? ToChar(Key key)
        {
            if (key is >= Key.A and <= Key.Z)
            {
                return (char)('A' + (key - Key.A));
            }

            if (key is >= Key.D0 and <= Key.D9)
            {
                return (char)('0' + (key - Key.D0));
            }

            if (key is >= Key.NumPad0 and <= Key.NumPad9)
            {
                return (char)('0' + (key - Key.NumPad0));
            }

            return null;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (key == Key.F1 && Keyboard.Modifiers == ModifierKeys.Control && _ribbon.IsMinimizable && _ribbon.IsVisible)
            {
                _ribbon.SetCurrentValue(Ribbon.IsMinimizedProperty, !_ribbon.IsMinimized);
                e.Handled = true;
                return;
            }

            if (IsActive)
            {
                HandleActiveKey(key, e);
                return;
            }

            if (key is Key.LeftAlt or Key.RightAlt)
            {
                if (!e.IsRepeat)
                {
                    _altDown = true;
                    _otherKeyWhileAlt = false;
                }

                return;
            }

            if (key == Key.F10 && Keyboard.Modifiers == ModifierKeys.None && CanRun)
            {
                Enter();
                e.Handled = true;
                return;
            }

            if (_altDown)
            {
                _otherKeyWhileAlt = true;

                // Alt+letter jumps straight to a root target (for example Alt+H for Home) — but only when it
                // matches, so application Alt shortcuts and access keys keep working.
                if (Keyboard.Modifiers == ModifierKeys.Alt && ToChar(key) is { } c && CanRun && RootMatches(c))
                {
                    Enter();
                    HandleChar(c);
                    e.Handled = true;
                }
            }
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key is not (Key.LeftAlt or Key.RightAlt))
            {
                return;
            }

            var tapped = _altDown && !_otherKeyWhileAlt && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Windows)) == 0;
            _altDown = false;

            if (!tapped)
            {
                return;
            }

            if (IsActive)
            {
                Exit();
            }
            else
            {
                Enter();
            }

            e.Handled = true;
        }

        private bool RootMatches(char c)
        {
            var probe = new Scope { Root = _ribbon };
            Collect(probe);
            return probe.Tips.Any(t => t.Tip.StartsWith(c));
        }

        private void HandleActiveKey(Key key, KeyEventArgs e)
        {
            switch (key)
            {
                case Key.LeftAlt or Key.RightAlt:
                    _altDown = true;
                    _otherKeyWhileAlt = false;
                    e.Handled = true;
                    return;
                case Key.Escape:
                    Pop();
                    e.Handled = true;
                    return;
                case Key.Back:
                    if (_typed.Length > 0)
                    {
                        _typed = _typed[..^1];
                        Highlight();
                    }
                    else
                    {
                        Pop();
                    }

                    e.Handled = true;
                    return;
                case Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl:
                    return;
            }

            _otherKeyWhileAlt = true;
            if (ToChar(key) is { } c)
            {
                HandleChar(c);
                e.Handled = true;
                return;
            }

            // Navigation keys leave key tip mode and keep their normal meaning.
            Exit();
        }

        private void HandleChar(char c)
        {
            if (_scopes.Count == 0)
            {
                return;
            }

            var scope = _scopes.Peek();
            var typed = _typed + c;
            var matches = scope.Tips.Where(t => t.Tip.StartsWith(typed, StringComparison.Ordinal)).ToList();
            if (matches.Count == 0)
            {
                // Unknown input is ignored; the current tips stay up.
                return;
            }

            _typed = typed;
            var exact = matches.FirstOrDefault(t => t.Tip == typed);
            if (exact.Target != null)
            {
                Activate(exact.Target, exact.Tip);
                return;
            }

            Highlight();
        }

        private void Highlight()
        {
            foreach (var adorner in _adorners)
            {
                adorner.Opacity = adorner.Tip.StartsWith(_typed, StringComparison.Ordinal) ? 1 : 0.3;
            }
        }

        private void Activate(UIElement target, string tip)
        {
            ClearAdorners();
            _typed = string.Empty;

            Activated?.Invoke(this, new KeyTipActivatedEventArgs(target, tip));
            var next = (target as IRibbonKeyTipTarget)?.ActivateKeyTip();
            if (!IsActive)
            {
                // The activation closed key tip mode (for example a command was invoked).
                return;
            }

            if (next == null)
            {
                Exit();
                return;
            }

            Action? onPop = null;
            if (FindOwningPopup(next) is { } popup)
            {
                onPop = () => popup.SetCurrentValue(Popup.IsOpenProperty, false);
            }
            else if (_ribbon.IsMinimized)
            {
                onPop = () => _ribbon.SetCurrentValue(Ribbon.IsMinimizedPopupOpenProperty, false);
            }

            // Popups and newly selected tabs lay out asynchronously; show the nested scope once they have.
            var scope = new Scope { Root = next, OnPop = onPop };
            _scopes.Push(scope);
            _ribbon.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (_scopes.Count > 0 && _scopes.Peek() == scope)
                {
                    Show(scope);
                }
            }));
        }

        private static Popup? FindOwningPopup(DependencyObject element)
        {
            DependencyObject? current = element;
            while (current != null)
            {
                if (current is Popup popup)
                {
                    return popup;
                }

                current = LogicalTreeHelper.GetParent(current) ?? (current is Visual ? VisualTreeHelper.GetParent(current) : null);
                if (current?.GetType().Name == "PopupRoot")
                {
                    current = LogicalTreeHelper.GetParent(current) ?? VisualTreeHelper.GetParent(current);
                }
            }

            return null;
        }

        private void Pop()
        {
            ClearAdorners();
            _typed = string.Empty;

            if (_scopes.Count <= 1)
            {
                Exit();
                return;
            }

            var scope = _scopes.Pop();
            scope.OnPop?.Invoke();
            Show(_scopes.Peek());
        }

        private void Push(Scope scope)
        {
            _scopes.Push(scope);
            Show(scope);
        }

        private void Show(Scope scope)
        {
            ClearAdorners();
            Collect(scope);

            foreach (var (target, tip) in scope.Tips)
            {
                if (string.IsNullOrEmpty(tip) || AdornerLayer.GetAdornerLayer(target) is not { } layer)
                {
                    continue;
                }

                var adorner = new KeyTipAdorner(target, tip);
                layer.Add(adorner);
                _adorners.Add(adorner);
            }
        }

        private void Collect(Scope scope)
        {
            var targets = new List<UIElement>();
            var isRoot = scope.Root == _ribbon;
            Walk(scope.Root, targets, isRoot);
            if (isRoot && !_ribbon.IsAncestorOf(_ribbon.QuickAccessToolbar) && _ribbon.QuickAccessToolbar.IsVisible)
            {
                Walk(_ribbon.QuickAccessToolbar, targets, skipTabBodies: true);
            }

            var spec = targets.Select(t => (
                Label: RibbonItem.ResolveLabel(t),
                Assigned: RibbonKeyTipService.GetKeyTip(t),
                IsNumbered: isRoot && IsInQuickAccessToolbar(t))).ToList();

            var tips = RibbonKeyTipService.GenerateKeyTips(spec, Diagnostics);
            scope.Tips = targets.Zip(tips, (t, k) => (t, k)).Where(p => p.k.Length > 0).ToList();
        }

        private static bool IsInQuickAccessToolbar(DependencyObject element)
        {
            DependencyObject? current = element;
            while (current != null)
            {
                if (current is RibbonQuickAccessToolbar)
                {
                    return true;
                }

                if (current is Ribbon)
                {
                    return false;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static void Walk(DependencyObject parent, List<UIElement> targets, bool skipTabBodies)
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is UIElement { IsVisible: false })
                {
                    continue;
                }

                if (skipTabBodies && child is RibbonTabItem)
                {
                    continue;
                }

                if (child is IRibbonKeyTipTarget { IsKeyTipTarget: true } && child is UIElement { IsEnabled: true } element)
                {
                    targets.Add(element);
                    continue;
                }

                Walk(child, targets, skipTabBodies);
            }
        }

        private void ClearAdorners()
        {
            foreach (var adorner in _adorners)
            {
                (VisualTreeHelper.GetParent(adorner) as AdornerLayer)?.Remove(adorner);
            }

            _adorners.Clear();
        }
    }

    /// <summary>
    /// Provides data for <see cref="KeyTipManager.Activated"/>.
    /// </summary>
    public class KeyTipActivatedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyTipActivatedEventArgs"/> class.
        /// </summary>
        public KeyTipActivatedEventArgs(UIElement target, string keyTip)
        {
            Target = target;
            KeyTip = keyTip;
        }

        /// <summary>Gets the activated element.</summary>
        public UIElement Target { get; }

        /// <summary>Gets the key tip that was typed.</summary>
        public string KeyTip { get; }
    }

    /// <summary>
    /// Draws one key tip badge over its target.
    /// </summary>
    internal sealed class KeyTipAdorner : Adorner
    {
        private readonly Border _badge;

        public string Tip { get; }

        public KeyTipAdorner(UIElement target, string tip) : base(target)
        {
            Tip = tip;
            IsHitTestVisible = false;

            var text = new TextBlock
            {
                Text = tip,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            text.SetResourceReference(TextBlock.ForegroundProperty, MosaicTheme.RibbonKeyTipForegroundBrush);
            text.SetResourceReference(TextBlock.FontFamilyProperty, MosaicTheme.FontFamily);

            _badge = new Border
            {
                Child = text,
                MinWidth = 16,
                Padding = new Thickness(4, 0, 4, 1),
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1),
                SnapsToDevicePixels = true
            };
            _badge.SetResourceReference(Border.BackgroundProperty, MosaicTheme.RibbonKeyTipBackgroundBrush);
            _badge.SetResourceReference(Border.BorderBrushProperty, MosaicTheme.RibbonKeyTipBorderBrush);
            AddVisualChild(_badge);
            System.Windows.Automation.AutomationProperties.SetName(_badge, tip);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _badge;

        protected override Size MeasureOverride(Size constraint)
        {
            _badge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return AdornedElement.RenderSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = AdornedElement.RenderSize;
            var badge = _badge.DesiredSize;

            // Large targets (large buttons, tab headers) take a centered tip at the bottom edge, like Office;
            // row targets take a tip near their left edge.
            var isLarge = size.Height >= 40 || AdornedElement is RibbonTabHeader or RibbonApplicationButton;
            var x = isLarge ? (size.Width - badge.Width) / 2 : Math.Min(10, Math.Max(0, size.Width - badge.Width));
            var y = AdornedElement is RibbonTabHeader or RibbonApplicationButton
                ? size.Height - (badge.Height / 2) - 2
                : isLarge ? size.Height - badge.Height + 2 : (size.Height / 2) + 1;

            _badge.Arrange(new Rect(new Point(x, y), badge));
            return finalSize;
        }
    }
}
