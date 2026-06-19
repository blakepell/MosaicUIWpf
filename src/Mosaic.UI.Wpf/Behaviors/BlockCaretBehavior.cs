/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;
using System.Globalization;
using System.Windows.Documents;
using System.Windows.Threading;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// A behavior that replaces the standard I-beam caret in a <see cref="TextBox"/> with a
    /// full-character-width block caret, similar to terminal emulators and classic text editors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The behavior hides the native TextBox caret by setting <see cref="TextBox.CaretBrush"/> to
    /// transparent and adds an <see cref="Adorner"/> that draws a solid rectangle at the current caret
    /// position. Optionally the character under the caret is re-drawn on top of the block in the
    /// contrasting color (controlled by <see cref="ShowCharacterUnderCaret"/>) so the text remains
    /// legible. The block is only visible while the TextBox has keyboard focus and automatically
    /// disappears when a range of text is selected. The adorner is cleaned up when the behavior is
    /// detached or the TextBox is unloaded.
    /// </para>
    /// <para>
    /// The block width is measured per-character using <see cref="FormattedText"/> so it tracks
    /// proportional fonts correctly. The typeface is cached and only re-created when the font
    /// properties change. Scroll position is tracked via the internal <see cref="ScrollViewer"/>
    /// so the block stays in sync when the user scrolls a multi-line TextBox with the mouse wheel.
    /// </para>
    /// </remarks>
    /// <example>
    /// Minimal usage — block color defaults to the TextBox Foreground:
    /// <code lang="XAML">
    /// <![CDATA[
    /// <TextBox FontFamily="Consolas" FontSize="14">
    ///     <i:Interaction.Behaviors>
    ///         <behaviors:BlockCaretBehavior />
    ///     </i:Interaction.Behaviors>
    /// </TextBox>
    /// ]]>
    /// </code>
    /// </example>
    /// <example>
    /// Terminal-style green block with white character overlay:
    /// <code lang="XAML">
    /// <![CDATA[
    /// <TextBox FontFamily="Consolas" FontSize="14" Background="Black" Foreground="LimeGreen">
    ///     <i:Interaction.Behaviors>
    ///         <behaviors:BlockCaretBehavior BlockBrush="LimeGreen" CharacterBrush="Black" />
    ///     </i:Interaction.Behaviors>
    /// </TextBox>
    /// ]]>
    /// </code>
    /// </example>
    public class BlockCaretBehavior : Behavior<TextBox>
    {
        private BlockCaretAdorner? _adorner;
        private Brush? _savedCaretBrush;
        private bool _savedCaretBrushSet;
        private ScrollViewer? _scrollViewer;

        /// <summary>
        /// The brush used to fill the block caret rectangle.
        /// When <see langword="null"/> (the default) the TextBox's <see cref="Control.Foreground"/> is used.
        /// </summary>
        public static readonly DependencyProperty BlockBrushProperty =
            DependencyProperty.Register(
                nameof(BlockBrush),
                typeof(Brush),
                typeof(BlockCaretBehavior),
                new PropertyMetadata(null, static (d, _) => ((BlockCaretBehavior)d).InvalidateAdorner()));

        /// <summary>
        /// The brush used to fill the block caret rectangle.
        /// When <see langword="null"/> (the default) the TextBox's <see cref="Control.Foreground"/> is used.
        /// </summary>
        public Brush? BlockBrush
        {
            get => (Brush?)GetValue(BlockBrushProperty);
            set => SetValue(BlockBrushProperty, value);
        }

        /// <summary>
        /// The brush used to draw the character that sits under the caret (the "inverted" foreground).
        /// When <see langword="null"/> (the default) the TextBox's <see cref="Control.Background"/> is used.
        /// </summary>
        public static readonly DependencyProperty CharacterBrushProperty =
            DependencyProperty.Register(
                nameof(CharacterBrush),
                typeof(Brush),
                typeof(BlockCaretBehavior),
                new PropertyMetadata(null, static (d, _) => ((BlockCaretBehavior)d).InvalidateAdorner()));

        /// <summary>
        /// The brush used to draw the character that sits under the caret (the "inverted" foreground).
        /// When <see langword="null"/> (the default) the TextBox's <see cref="Control.Background"/> is used.
        /// </summary>
        public Brush? CharacterBrush
        {
            get => (Brush?)GetValue(CharacterBrushProperty);
            set => SetValue(CharacterBrushProperty, value);
        }

        /// <summary>
        /// When <see langword="true"/> (the default) the character under the caret is re-rendered on
        /// top of the block in <see cref="CharacterBrush"/> so the text remains legible.
        /// Set to <see langword="false"/> for a solid opaque block that hides the character.
        /// </summary>
        public static readonly DependencyProperty ShowCharacterUnderCaretProperty =
            DependencyProperty.Register(
                nameof(ShowCharacterUnderCaret),
                typeof(bool),
                typeof(BlockCaretBehavior),
                new PropertyMetadata(true, static (d, _) => ((BlockCaretBehavior)d).InvalidateAdorner()));

        /// <summary>
        /// When <see langword="true"/> (the default) the character under the caret is re-rendered on
        /// top of the block in <see cref="CharacterBrush"/> so the text remains legible.
        /// Set to <see langword="false"/> for a solid opaque block that hides the character.
        /// </summary>
        public bool ShowCharacterUnderCaret
        {
            get => (bool)GetValue(ShowCharacterUnderCaretProperty);
            set => SetValue(ShowCharacterUnderCaretProperty, value);
        }

        /// <inheritdoc />
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.Unloaded += OnUnloaded;
            AssociatedObject.GotFocus += OnFocusChanged;
            AssociatedObject.LostFocus += OnFocusChanged;
            AssociatedObject.SelectionChanged += OnSelectionChanged;
            AssociatedObject.TextChanged += OnTextChanged;

            if (AssociatedObject.IsLoaded)
            {
                Initialize();
            }
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            base.OnDetaching();
            Cleanup();
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.Unloaded -= OnUnloaded;
            AssociatedObject.GotFocus -= OnFocusChanged;
            AssociatedObject.LostFocus -= OnFocusChanged;
            AssociatedObject.SelectionChanged -= OnSelectionChanged;
            AssociatedObject.TextChanged -= OnTextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => Initialize();
        private void OnUnloaded(object sender, RoutedEventArgs e) => Cleanup();
        private void OnFocusChanged(object sender, RoutedEventArgs e) => InvalidateAdorner();
        private void OnSelectionChanged(object sender, RoutedEventArgs e) => InvalidateAdorner();
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e) => InvalidateAdorner();

        // TextChanged fires while the TextBox is still mid-layout, so GetRectFromCharacterIndex
        // returns stale/empty coords at that point. Deferring to Background priority ensures the
        // layout pass has finished before the adorner tries to measure the new caret position.
        private void OnTextChanged(object sender, TextChangedEventArgs e) =>
            Dispatcher.BeginInvoke(InvalidateAdorner, DispatcherPriority.Background);

        private void Initialize()
        {
            if (!_savedCaretBrushSet)
            {
                _savedCaretBrush = AssociatedObject.CaretBrush;
                _savedCaretBrushSet = true;
            }

            AssociatedObject.CaretBrush = Brushes.Transparent;

            var layer = AdornerLayer.GetAdornerLayer(AssociatedObject);
            if (layer != null && _adorner == null)
            {
                _adorner = new BlockCaretAdorner(AssociatedObject, this);
                layer.Add(_adorner);
            }

            _scrollViewer = FindDescendant<ScrollViewer>(AssociatedObject);
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += OnScrollChanged;
            }

            InvalidateAdorner();
        }

        private void Cleanup()
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged -= OnScrollChanged;
                _scrollViewer = null;
            }

            if (_adorner != null)
            {
                AdornerLayer.GetAdornerLayer(AssociatedObject)?.Remove(_adorner);
                _adorner = null;
            }

            if (_savedCaretBrushSet)
            {
                AssociatedObject.CaretBrush = _savedCaretBrush;
                _savedCaretBrushSet = false;
            }
        }

        private void InvalidateAdorner() => _adorner?.InvalidateVisual();

        private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                {
                    return t;
                }

                var result = FindDescendant<T>(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private sealed class BlockCaretAdorner : Adorner
        {
            private readonly TextBox _textBox;
            private readonly BlockCaretBehavior _behavior;
            private Typeface? _typeface;
            private FontFamily? _cachedFamily;
            private FontStyle _cachedStyle;
            private FontWeight _cachedWeight;
            private FontStretch _cachedStretch;

            public BlockCaretAdorner(TextBox textBox, BlockCaretBehavior behavior) : base(textBox)
            {
                _textBox = textBox;
                _behavior = behavior;
                IsHitTestVisible = false;
            }

            protected override void OnRender(DrawingContext dc)
            {
                if (!_textBox.IsFocused || _textBox.SelectionLength > 0)
                {
                    return;
                }

                Rect caretRect;
                try
                {
                    caretRect = _textBox.GetRectFromCharacterIndex(_textBox.CaretIndex);
                }
                catch
                {
                    return;
                }

                if (caretRect.IsEmpty || double.IsInfinity(caretRect.X) || double.IsInfinity(caretRect.Y))
                {
                    return;
                }

                double blockWidth = MeasureCharWidth(_textBox.CaretIndex);
                var blockRect = new Rect(caretRect.X, caretRect.Y, blockWidth, caretRect.Height);

                dc.DrawRectangle(_behavior.BlockBrush ?? _textBox.Foreground, null, blockRect);

                if (_behavior.ShowCharacterUnderCaret)
                {
                    int idx = _textBox.CaretIndex;
                    string text = _textBox.Text;

                    if (idx < text.Length && text[idx] is not ('\n' or '\r'))
                    {
                        var charBrush = _behavior.CharacterBrush ?? _textBox.Background ?? Brushes.White;
                        var dpi = VisualTreeHelper.GetDpi(_textBox).PixelsPerDip;
                        var ft = new FormattedText(
                            text[idx].ToString(),
                            CultureInfo.CurrentUICulture,
                            _textBox.FlowDirection,
                            GetTypeface(),
                            _textBox.FontSize,
                            charBrush,
                            dpi);

                        dc.DrawText(ft, blockRect.TopLeft);
                    }
                }
            }

            private double MeasureCharWidth(int caretIndex)
            {
                string text = _textBox.Text;

                // Use a visible reference character for whitespace and end-of-text so the
                // block stays a consistent width rather than collapsing to near-zero on spaces.
                char c = caretIndex < text.Length && text[caretIndex] is not ('\n' or '\r' or ' ' or '\t')
                    ? text[caretIndex]
                    : 'M';

                var dpi = VisualTreeHelper.GetDpi(_textBox).PixelsPerDip;
                var ft = new FormattedText(
                    c.ToString(),
                    CultureInfo.CurrentUICulture,
                    _textBox.FlowDirection,
                    GetTypeface(),
                    _textBox.FontSize,
                    Brushes.Black,
                    dpi);

                return Math.Max(ft.Width, 2.0);
            }

            private Typeface GetTypeface()
            {
                if (_typeface == null ||
                    !Equals(_cachedFamily, _textBox.FontFamily) ||
                    _cachedStyle != _textBox.FontStyle ||
                    _cachedWeight != _textBox.FontWeight ||
                    _cachedStretch != _textBox.FontStretch)
                {
                    _cachedFamily = _textBox.FontFamily;
                    _cachedStyle = _textBox.FontStyle;
                    _cachedWeight = _textBox.FontWeight;
                    _cachedStretch = _textBox.FontStretch;
                    _typeface = new Typeface(_cachedFamily, _cachedStyle, _cachedWeight, _cachedStretch);
                }

                return _typeface;
            }
        }
    }
}
