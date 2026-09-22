/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.Xaml.Behaviors;
using System.Globalization;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// The appearance settings the <see cref="AvalonEditBlockCaretRenderer"/> reads as it draws.
    /// Implemented by <see cref="AvalonEditBlockCaretBehavior"/> and by controls such as
    /// <see cref="Mosaic.UI.Wpf.Controls.CommandBox"/> that host the same renderer directly.
    /// </summary>
    internal interface IBlockCaretOptions
    {
        /// <summary>
        /// The brush the block is filled with, or <see langword="null"/> to use the editor foreground.
        /// </summary>
        Brush? CaretBrush { get; }

        /// <summary>
        /// The brush the covered character is re-drawn in, or <see langword="null"/> to pick a
        /// contrasting color automatically.
        /// </summary>
        Brush? CaretTextBrush { get; }

        /// <summary>
        /// The opacity of the block, from 0.0 to 1.0.
        /// </summary>
        double CaretOpacity { get; }
    }

    /// <summary>
    /// Attaches to an AvalonEdit <see cref="TextEditor"/> and swaps the default thin line caret for
    /// a solid block caret. The <see cref="IsEnabled"/> dependency property controls which caret
    /// style is shown and can be toggled at runtime (including via data binding). The character
    /// sitting underneath the block is re-drawn on top of it in a contrasting color so that it
    /// stays legible.
    /// </summary>
    /// <remarks>
    /// This is the AvalonEdit counterpart of <see cref="BlockCaretBehavior"/>, which targets a plain
    /// WPF <see cref="TextBox"/> and cannot be attached to a <see cref="TextEditor"/>.
    /// <see cref="Mosaic.UI.Wpf.Controls.CommandBox"/> has the same renderer built in behind its
    /// <c>UseBlockCaret</c> property, so this behavior is only needed for other editors such as
    /// <see cref="Mosaic.UI.Wpf.Controls.SyntaxEditor"/> or a bare <see cref="TextEditor"/>.
    /// </remarks>
    /// <example>
    /// <code lang="XAML"><![CDATA[
    /// <avalonEdit:TextEditor xmlns:avalonEdit="http://icsharpcode.net/sharpdevelop/avalonedit"
    ///                        xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"
    ///                        xmlns:i="http://schemas.microsoft.com/xaml/behaviors">
    ///     <i:Interaction.Behaviors>
    ///         <mosaic:AvalonEditBlockCaretBehavior IsEnabled="{Binding UseBlockCaret}"
    ///                                              CaretBrush="Black"
    ///                                              CaretTextBrush="White"
    ///                                              CaretOpacity="0.8" />
    ///     </i:Interaction.Behaviors>
    /// </avalonEdit:TextEditor>
    /// ]]></code>
    /// </example>
    public class AvalonEditBlockCaretBehavior : Behavior<TextEditor>, IBlockCaretOptions
    {
        /// <summary>
        /// The installed renderer, or <see langword="null"/> when the block caret is not active.
        /// </summary>
        private AvalonEditBlockCaretRenderer? _renderer;

        /// <summary>
        /// Identifies the <see cref="IsEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
            nameof(IsEnabled),
            typeof(bool),
            typeof(AvalonEditBlockCaretBehavior),
            new PropertyMetadata(true, OnIsEnabledChanged));

        /// <summary>
        /// Identifies the <see cref="CaretBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CaretBrushProperty = DependencyProperty.Register(
            nameof(CaretBrush),
            typeof(Brush),
            typeof(AvalonEditBlockCaretBehavior),
            new PropertyMetadata(null, OnCaretAppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="CaretTextBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CaretTextBrushProperty = DependencyProperty.Register(
            nameof(CaretTextBrush),
            typeof(Brush),
            typeof(AvalonEditBlockCaretBehavior),
            new PropertyMetadata(null, OnCaretAppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="CaretOpacity"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CaretOpacityProperty = DependencyProperty.Register(
            nameof(CaretOpacity),
            typeof(double),
            typeof(AvalonEditBlockCaretBehavior),
            new PropertyMetadata(0.8, OnCaretAppearanceChanged));

        /// <summary>
        /// Determines whether the block caret is shown in place of the editor's normal caret.
        /// Defaults to <see langword="true"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("Determines whether the block caret is shown in place of the editor's normal caret.")]
        public bool IsEnabled
        {
            get => (bool)this.GetValue(IsEnabledProperty);
            set => this.SetValue(IsEnabledProperty, value);
        }

        /// <summary>
        /// The brush the block caret is filled with. When <see langword="null"/> (the default) the
        /// editor's <see cref="Control.Foreground"/> is used. A <see cref="SolidColorBrush"/> is
        /// recommended; it is the only brush type the automatic <see cref="CaretTextBrush"/>
        /// contrast calculation can inspect.
        /// </summary>
        [Category("Mosaic")]
        [Description("The brush the block caret is filled with. Null uses the editor foreground.")]
        public Brush? CaretBrush
        {
            get => (Brush?)this.GetValue(CaretBrushProperty);
            set => this.SetValue(CaretBrushProperty, value);
        }

        /// <summary>
        /// The brush used to re-draw the character the block caret covers. When
        /// <see langword="null"/> (the default) a contrasting color is chosen automatically based on
        /// the composited color of the block.
        /// </summary>
        [Category("Mosaic")]
        [Description("The brush the character under the block caret is re-drawn in. Null picks a contrasting color.")]
        public Brush? CaretTextBrush
        {
            get => (Brush?)this.GetValue(CaretTextBrushProperty);
            set => this.SetValue(CaretTextBrushProperty, value);
        }

        /// <summary>
        /// The opacity of the block caret in the range 0.0 to 1.0. Defaults to 0.8.
        /// </summary>
        [Category("Mosaic")]
        [Description("The opacity of the block caret, from 0.0 to 1.0.")]
        public double CaretOpacity
        {
            get => (double)this.GetValue(CaretOpacityProperty);
            set => this.SetValue(CaretOpacityProperty, value);
        }

        /// <summary>
        /// Installs the block caret renderer when the behavior is attached, if it is enabled.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            if (this.IsEnabled)
            {
                this.AddRenderer();
            }
        }

        /// <summary>
        /// Removes the block caret renderer and restores the editor's own caret.
        /// </summary>
        protected override void OnDetaching()
        {
            this.RemoveRenderer();
            base.OnDetaching();
        }

        /// <summary>
        /// Adds or removes the block caret renderer when <see cref="IsEnabled"/> changes.
        /// </summary>
        /// <param name="d">The behavior whose property changed.</param>
        /// <param name="e">The property change details.</param>
        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not AvalonEditBlockCaretBehavior { AssociatedObject: not null } behavior)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                behavior.AddRenderer();
            }
            else
            {
                behavior.RemoveRenderer();
            }
        }

        /// <summary>
        /// Forces the caret to be redrawn when one of the appearance properties changes. The
        /// renderer reads those properties as it draws, so nothing else has to be pushed over to it.
        /// </summary>
        /// <param name="d">The behavior whose property changed.</param>
        /// <param name="e">The property change details.</param>
        private static void OnCaretAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AvalonEditBlockCaretBehavior { AssociatedObject: not null } behavior && behavior._renderer != null)
            {
                behavior.AssociatedObject.TextArea.TextView.InvalidateLayer(KnownLayer.Caret);
            }
        }

        /// <summary>
        /// Installs the block caret renderer and hides the editor's normal caret.
        /// </summary>
        private void AddRenderer()
        {
            if (_renderer != null || this.AssociatedObject == null)
            {
                return;
            }

            _renderer = AvalonEditBlockCaretRenderer.Install(this.AssociatedObject, this);
        }

        /// <summary>
        /// Removes the block caret renderer and restores the editor's normal caret.
        /// </summary>
        private void RemoveRenderer()
        {
            if (_renderer == null || this.AssociatedObject == null)
            {
                return;
            }

            AvalonEditBlockCaretRenderer.Uninstall(this.AssociatedObject, _renderer);
            _renderer = null;
        }
    }

    /// <summary>
    /// Renders a solid block cursor at the caret position of a <see cref="TextEditor"/>, replacing
    /// the default thin line cursor. The character underneath the block is re-drawn on top of it so
    /// that it remains readable.
    /// </summary>
    internal class AvalonEditBlockCaretRenderer : IBackgroundRenderer
    {
        /// <summary>
        /// The editor the caret is drawn for.
        /// </summary>
        private readonly TextEditor _editor;

        /// <summary>
        /// Supplies the brushes and opacity the block is drawn with.
        /// </summary>
        private readonly IBlockCaretOptions _options;

        /// <summary>
        /// Cache of the last generated block brush so that a brush is not allocated on every redraw
        /// (the caret layer redraws on every blink).
        /// </summary>
        private SolidColorBrush? _cachedBlockBrush;

        /// <summary>
        /// The color the <see cref="_cachedBlockBrush"/> was generated from.
        /// </summary>
        private Color _cachedBlockColor;

        /// <summary>
        /// Cache of the opacity adjusted clone of a non solid color caret brush.
        /// </summary>
        private Brush? _cachedCustomBrush;

        /// <summary>
        /// The brush and opacity the <see cref="_cachedCustomBrush"/> was cloned from.
        /// </summary>
        private (Brush? Source, double Opacity) _cachedCustomBrushKey;

        /// <summary>
        /// Creates a new renderer.
        /// </summary>
        /// <param name="editor">The editor the caret is drawn for.</param>
        /// <param name="options">Supplies the brushes and opacity the block is drawn with.</param>
        internal AvalonEditBlockCaretRenderer(TextEditor editor, IBlockCaretOptions options)
        {
            _editor = editor;
            _options = options;
        }

        /// <summary>
        /// The layer the block caret is drawn on.
        /// </summary>
        public KnownLayer Layer => KnownLayer.Caret;

        /// <summary>
        /// Creates a renderer, adds it to the editor's background renderers and hides the editor's
        /// own caret.
        /// </summary>
        /// <param name="editor">The editor to install the block caret on.</param>
        /// <param name="options">Supplies the brushes and opacity the block is drawn with.</param>
        internal static AvalonEditBlockCaretRenderer Install(TextEditor editor, IBlockCaretOptions options)
        {
            var renderer = new AvalonEditBlockCaretRenderer(editor, options);
            editor.TextArea.TextView.BackgroundRenderers.Add(renderer);
            editor.TextArea.Caret.CaretBrush = Brushes.Transparent;
            return renderer;
        }

        /// <summary>
        /// Removes a renderer from the editor and restores the editor's own caret.
        /// </summary>
        /// <param name="editor">The editor the renderer was installed on.</param>
        /// <param name="renderer">The renderer to remove.</param>
        internal static void Uninstall(TextEditor editor, AvalonEditBlockCaretRenderer renderer)
        {
            editor.TextArea.TextView.BackgroundRenderers.Remove(renderer);
            editor.TextArea.Caret.CaretBrush = null;
        }

        /// <summary>
        /// Draws the block and, when there is one, the character it covers.
        /// </summary>
        /// <param name="textView">The text view being rendered.</param>
        /// <param name="drawingContext">The drawing context to render into.</param>
        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (!_editor.TextArea.IsFocused || _editor.Document == null)
            {
                return;
            }

            textView.EnsureVisualLines();

            int caretOffset = _editor.TextArea.Caret.Offset;

            if (caretOffset < 0 || caretOffset > _editor.Document.TextLength)
            {
                return;
            }

            var caretLine = _editor.Document.GetLineByOffset(caretOffset);
            var visualLine = textView.GetVisualLine(caretLine.LineNumber);

            if (visualLine == null)
            {
                return;
            }

            int visualColumn = _editor.TextArea.Caret.Position.VisualColumn;
            double xPos = visualLine.GetVisualPosition(visualColumn, VisualYPosition.LineTop).X;
            double yTop = visualLine.GetVisualPosition(visualColumn, VisualYPosition.LineTop).Y;
            double yBottom = visualLine.GetVisualPosition(visualColumn, VisualYPosition.LineBottom).Y;
            double baseline = visualLine.GetVisualPosition(visualColumn, VisualYPosition.Baseline).Y;

            // The text (if any) that the block is going to cover. Whitespace and control characters
            // are ignored, there is nothing to make legible in those cases.
            string? glyph = this.GetGlyph(caretLine.EndOffset, caretOffset);

            // Use the real advance width of the covered character when we have one so wide (e.g. CJK)
            // glyphs are not clipped by a single space wide block.
            double width = textView.WideSpaceWidth;

            if (glyph != null && visualColumn + glyph.Length <= visualLine.VisualLength)
            {
                double nextX = visualLine.GetVisualPosition(visualColumn + glyph.Length, VisualYPosition.LineTop).X;

                if (nextX > xPos)
                {
                    width = nextX - xPos;
                }
            }

            xPos -= textView.ScrollOffset.X;
            yTop -= textView.ScrollOffset.Y;
            yBottom -= textView.ScrollOffset.Y;
            baseline -= textView.ScrollOffset.Y;

            var blockBrush = this.GetBlockBrush(out var blockColor);
            var rect = new Rect(xPos, yTop, width, yBottom - yTop);
            drawingContext.DrawRectangle(blockBrush, null, rect);

            if (glyph == null)
            {
                return;
            }

            var (typeface, emSize) = this.GetFont(visualLine, visualColumn);

            var formattedText = new FormattedText(
                glyph,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                emSize,
                _options.CaretTextBrush ?? GetContrastBrush(blockColor, this.GetEditorBackgroundColor()),
                VisualTreeHelper.GetDpi(_editor).PixelsPerDip);

            drawingContext.DrawText(formattedText, new Point(xPos, baseline - formattedText.Baseline));
        }

        /// <summary>
        /// Returns the text that sits underneath the caret, or <see langword="null"/> when there is
        /// nothing worth drawing there (end of line, whitespace or a control character).
        /// </summary>
        /// <param name="lineEndOffset">The offset of the end of the line the caret is on.</param>
        /// <param name="caretOffset">The current caret offset.</param>
        private string? GetGlyph(int lineEndOffset, int caretOffset)
        {
            if (_editor.Document == null || caretOffset >= lineEndOffset)
            {
                return null;
            }

            char c = _editor.Document.GetCharAt(caretOffset);

            if (char.IsControl(c) || char.IsWhiteSpace(c))
            {
                return null;
            }

            // Surrogate pairs (emoji and the like) have to be drawn as both halves or they render
            // as a pair of replacement boxes.
            if (char.IsHighSurrogate(c) && caretOffset + 1 < lineEndOffset)
            {
                return _editor.Document.GetText(caretOffset, 2);
            }

            return c.ToString();
        }

        /// <summary>
        /// The typeface and size the covered character is drawn with. This is taken from the visual
        /// line element so that syntax highlighting (bold, italic, a different font) is honored,
        /// falling back to the editor's own font when it cannot be resolved.
        /// </summary>
        /// <param name="visualLine">The visual line the caret is on.</param>
        /// <param name="visualColumn">The visual column of the caret.</param>
        private (Typeface Typeface, double EmSize) GetFont(VisualLine visualLine, int visualColumn)
        {
            foreach (var element in visualLine.Elements)
            {
                if (visualColumn < element.VisualColumn || visualColumn >= element.VisualColumn + element.VisualLength)
                {
                    continue;
                }

                var properties = element.TextRunProperties;

                if (properties?.Typeface != null)
                {
                    return (properties.Typeface, properties.FontRenderingEmSize);
                }

                break;
            }

            return (new Typeface(_editor.FontFamily, _editor.FontStyle, _editor.FontWeight, _editor.FontStretch), _editor.FontSize);
        }

        /// <summary>
        /// The brush the block itself is filled with, honoring <see cref="IBlockCaretOptions.CaretBrush"/>
        /// and <see cref="IBlockCaretOptions.CaretOpacity"/>.
        /// </summary>
        /// <param name="blockColor">
        /// The effective color of the block (opacity included) used for contrast calculations.
        /// </param>
        private Brush GetBlockBrush(out Color blockColor)
        {
            double opacity = Math.Clamp(_options.CaretOpacity, 0.0, 1.0);
            var source = _options.CaretBrush ?? _editor.Foreground;

            // Anything that is not a solid color brush (a gradient for instance) cannot have the
            // opacity folded into a color, so a clone of it is cached instead. There is no single
            // color to contrast against in that case, black is assumed.
            if (source is not SolidColorBrush solidBrush)
            {
                if (source != null)
                {
                    blockColor = Colors.Black;

                    if (_cachedCustomBrush == null || _cachedCustomBrushKey != (source, opacity))
                    {
                        _cachedCustomBrushKey = (source, opacity);
                        _cachedCustomBrush = source.Clone();
                        _cachedCustomBrush.Opacity = source.Opacity * opacity;

                        if (_cachedCustomBrush.CanFreeze)
                        {
                            _cachedCustomBrush.Freeze();
                        }
                    }

                    return _cachedCustomBrush;
                }

                solidBrush = Brushes.Black;
            }

            var baseColor = solidBrush.Color;
            blockColor = Color.FromArgb((byte)(baseColor.A * opacity), baseColor.R, baseColor.G, baseColor.B);

            if (_cachedBlockBrush == null || _cachedBlockColor != blockColor)
            {
                _cachedBlockColor = blockColor;
                _cachedBlockBrush = new SolidColorBrush(blockColor);
                _cachedBlockBrush.Freeze();
            }

            return _cachedBlockBrush;
        }

        /// <summary>
        /// The color sitting behind the caret, used to work out what the translucent block actually
        /// looks like on screen. When the editor has no usable background the inverse of its
        /// foreground is assumed.
        /// </summary>
        private Color GetEditorBackgroundColor()
        {
            if (_editor.Background is SolidColorBrush { Color.A: > 0 } background)
            {
                return background.Color;
            }

            var foreground = _editor.Foreground is SolidColorBrush brush ? brush.Color : Colors.Black;

            return IsDark(foreground) ? Colors.White : Colors.Black;
        }

        /// <summary>
        /// Picks black or white, whichever is more legible on top of the block caret once the block
        /// has been composited over the editor's background.
        /// </summary>
        /// <param name="blockColor">The color of the block caret.</param>
        /// <param name="backgroundColor">The color behind the block caret.</param>
        private static Brush GetContrastBrush(Color blockColor, Color backgroundColor)
        {
            double alpha = blockColor.A / 255.0;

            var composited = Color.FromRgb(
                (byte)((blockColor.R * alpha) + (backgroundColor.R * (1.0 - alpha))),
                (byte)((blockColor.G * alpha) + (backgroundColor.G * (1.0 - alpha))),
                (byte)((blockColor.B * alpha) + (backgroundColor.B * (1.0 - alpha))));

            return IsDark(composited) ? Brushes.White : Brushes.Black;
        }

        /// <summary>
        /// Whether a color is dark enough that light text should be drawn on top of it.
        /// </summary>
        /// <param name="color">The color to test.</param>
        private static bool IsDark(Color color)
        {
            return ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) < 140.0;
        }
    }
}
