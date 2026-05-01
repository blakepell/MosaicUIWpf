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
using System.Windows.Documents;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// A behavior that applies a retro VT/CRT terminal visual skin to an AvalonEdit <see cref="TextEditor"/>.
    /// When active the editor receives a phosphor-green color scheme, a solid block caret, and visual overlay
    /// effects (CRT scan lines, screen vignette, phosphor glow, and subtle noise).
    /// Toggle the skin at runtime via the <see cref="IsActive"/> dependency property.
    /// <![CDATA[
    /// <avalonedit:TextEditor>
    ///     <i:Interaction.Behaviors>
    ///         <behaviors:AvalonEditVtTerminalBehavior IsActive="{Binding IsTerminalMode}" />
    ///     </i:Interaction.Behaviors>
    /// </avalonedit:TextEditor>
    /// ]]>
    /// </summary>
    public class AvalonEditVtTerminalBehavior : Behavior<TextEditor>
    {
        #region Dependency Properties

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(AvalonEditVtTerminalBehavior),
                new PropertyMetadata(true, OnIsActiveChanged));

        /// <summary>
        /// Gets or sets whether the VT terminal skin is active.
        /// </summary>
        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public static readonly DependencyProperty ForegroundColorProperty =
            DependencyProperty.Register(
                nameof(ForegroundColor),
                typeof(Color),
                typeof(AvalonEditVtTerminalBehavior),
                new PropertyMetadata(Color.FromRgb(0x33, 0xff, 0x33), OnColorChanged));

        /// <summary>
        /// Gets or sets the phosphor foreground color. Defaults to classic green (#33FF33).
        /// </summary>
        public Color ForegroundColor
        {
            get => (Color)GetValue(ForegroundColorProperty);
            set => SetValue(ForegroundColorProperty, value);
        }

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register(
                nameof(BackgroundColor),
                typeof(Color),
                typeof(AvalonEditVtTerminalBehavior),
                new PropertyMetadata(Color.FromRgb(0x05, 0x05, 0x05), OnColorChanged));

        /// <summary>
        /// Gets or sets the terminal background color. Defaults to near-black (#050505).
        /// </summary>
        public Color BackgroundColor
        {
            get => (Color)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        #endregion

        #region Saved State

        private Brush? _savedBackground;
        private Brush? _savedForeground;
        private Brush? _savedCaretBrush;
        private Brush? _savedSelectionBrush;
        private Pen? _savedSelectionBorder;
        private Brush? _savedSelectionForeground;
        private Brush? _savedLineNumbersForeground;

        #endregion

        private VtTerminalBlockCaretRenderer? _caretRenderer;
        private VtTerminalAdorner? _adorner;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.Unloaded += OnUnloaded;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.Unloaded -= OnUnloaded;
            DisableSkin();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (IsActive)
            {
                EnableSkin();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DisableSkin();
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (AvalonEditVtTerminalBehavior)d;
            if (behavior.AssociatedObject == null || !behavior.AssociatedObject.IsLoaded)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                behavior.EnableSkin();
            }
            else
            {
                behavior.DisableSkin();
            }
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (AvalonEditVtTerminalBehavior)d;
            if (behavior.AssociatedObject == null || !behavior.AssociatedObject.IsLoaded || !behavior.IsActive)
            {
                return;
            }

            // Re-apply to pick up the new colors.
            behavior.DisableSkin();
            behavior.EnableSkin();
        }

        private void EnableSkin()
        {
            var editor = AssociatedObject;
            var fg = ForegroundColor;
            var bg = BackgroundColor;

            // Save the editor's current appearance so it can be fully restored when disabled.
            _savedBackground = editor.Background;
            _savedForeground = editor.Foreground;
            _savedCaretBrush = editor.TextArea.Caret.CaretBrush;
            _savedSelectionBrush = editor.TextArea.SelectionBrush;
            _savedSelectionBorder = editor.TextArea.SelectionBorder;
            _savedSelectionForeground = editor.TextArea.SelectionForeground;
            _savedLineNumbersForeground = editor.LineNumbersForeground;

            // Apply terminal colors.
            editor.Background = new SolidColorBrush(bg);
            editor.Foreground = new SolidColorBrush(fg);

            // Hide the default line caret; the block caret renderer replaces it.
            editor.TextArea.Caret.CaretBrush = Brushes.Transparent;

            // Selection styling using a semi-transparent tint of the foreground color.
            editor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(0x60, fg.R, fg.G, fg.B));
            editor.TextArea.SelectionBorder = new Pen(new SolidColorBrush(fg), 1);
            editor.TextArea.SelectionForeground = new SolidColorBrush(Colors.Black);
            editor.LineNumbersForeground = new SolidColorBrush(Color.FromArgb(0x80, fg.R, fg.G, fg.B));

            // Block caret renderer.
            _caretRenderer = new VtTerminalBlockCaretRenderer(editor, fg);
            editor.TextArea.TextView.BackgroundRenderers.Add(_caretRenderer);
            editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;

            // Overlay adorner for CRT effects.
            var adornerLayer = AdornerLayer.GetAdornerLayer(editor);
            if (adornerLayer != null)
            {
                _adorner = new VtTerminalAdorner(editor, fg);
                adornerLayer.Add(_adorner);
            }
        }

        private void DisableSkin()
        {
            var editor = AssociatedObject;
            if (editor == null)
            {
                return;
            }

            // Remove block caret renderer.
            if (_caretRenderer != null)
            {
                editor.TextArea.TextView.BackgroundRenderers.Remove(_caretRenderer);
                _caretRenderer = null;
            }

            editor.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;

            // Remove the CRT overlay adorner.
            if (_adorner != null)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(editor);
                adornerLayer?.Remove(_adorner);
                _adorner = null;
            }

            // Restore original appearance only if we previously saved it.
            if (_savedBackground == null)
            {
                return;
            }

            editor.Background = _savedBackground;
            editor.Foreground = _savedForeground!;
            editor.TextArea.Caret.CaretBrush = _savedCaretBrush;
            editor.TextArea.SelectionBrush = _savedSelectionBrush;
            editor.TextArea.SelectionBorder = _savedSelectionBorder;
            editor.TextArea.SelectionForeground = _savedSelectionForeground;
            editor.LineNumbersForeground = _savedLineNumbersForeground;

            _savedBackground = null;
            _savedForeground = null;
            _savedCaretBrush = null;
            _savedSelectionBrush = null;
            _savedSelectionBorder = null;
            _savedSelectionForeground = null;
            _savedLineNumbersForeground = null;
        }

        private void OnCaretPositionChanged(object? sender, EventArgs e)
        {
            AssociatedObject?.TextArea.TextView.InvalidateLayer(KnownLayer.Caret);
        }
    }

    /// <summary>
    /// Renders a solid block cursor at the caret position, replacing the default thin line cursor.
    /// </summary>
    internal class VtTerminalBlockCaretRenderer : IBackgroundRenderer
    {
        private readonly TextEditor _editor;
        private readonly SolidColorBrush _brush;

        public VtTerminalBlockCaretRenderer(TextEditor editor, Color foregroundColor)
        {
            _editor = editor;
            _brush = new SolidColorBrush(Color.FromArgb(0xCC, foregroundColor.R, foregroundColor.G, foregroundColor.B));
            _brush.Freeze();
        }

        public KnownLayer Layer => KnownLayer.Caret;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (!_editor.TextArea.IsFocused || _editor.Document == null)
            {
                return;
            }

            textView.EnsureVisualLines();

            var caretOffset = _editor.TextArea.Caret.Offset;
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

            xPos -= textView.ScrollOffset.X;
            yTop -= textView.ScrollOffset.Y;
            yBottom -= textView.ScrollOffset.Y;

            var rect = new Rect(xPos, yTop, textView.WideSpaceWidth, yBottom - yTop);
            drawingContext.DrawRectangle(_brush, null, rect);
        }
    }

    /// <summary>
    /// An adorner that renders CRT-style visual effects over the adorned element:
    /// scan lines, screen vignette, phosphor glow, and subtle noise texture.
    /// </summary>
    internal class VtTerminalAdorner : Adorner
    {
        private readonly LinearGradientBrush _scanLinesBrush;
        private readonly RadialGradientBrush _vignetteBrush;
        private readonly RadialGradientBrush _phosphorGlowBrush;
        private readonly DrawingBrush _noiseBrush;

        public VtTerminalAdorner(UIElement adornedElement, Color foregroundColor) : base(adornedElement)
        {
            IsHitTestVisible = false;

            // Repeating horizontal bands simulate CRT scan lines.
            _scanLinesBrush = new LinearGradientBrush
            {
                MappingMode = BrushMappingMode.Absolute,
                SpreadMethod = GradientSpreadMethod.Repeat,
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 4),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Colors.Transparent, 0),
                    new GradientStop(Colors.Transparent, 0.5),
                    new GradientStop(Color.FromArgb(0xCC, 0xCC, 0xCC, 0xCC), 0.5),
                    new GradientStop(Colors.Black, 1)
                }
            };
            _scanLinesBrush.Freeze();

            // Dark corners simulate the curvature of a CRT screen.
            _vignetteBrush = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.5),
                GradientOrigin = new Point(0.5, 0.5),
                RadiusX = 1,
                RadiusY = 1,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(0x00, 0, 0, 0), 0),
                    new GradientStop(Color.FromArgb(0x00, 0, 0, 0), 0.6),
                    new GradientStop(Color.FromArgb(0x80, 0, 0, 0), 1)
                }
            };
            _vignetteBrush.Freeze();

            // Radial glow from the center simulates phosphor bloom.
            _phosphorGlowBrush = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.5),
                GradientOrigin = new Point(0.5, 0.5),
                RadiusX = 1,
                RadiusY = 1,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(foregroundColor, 0),
                    new GradientStop(Color.FromArgb(0xFF,
                        (byte)(foregroundColor.R / 2),
                        (byte)(foregroundColor.G / 2),
                        (byte)(foregroundColor.B / 2)), 0.5),
                    new GradientStop(Color.FromArgb(0x00, 0, 0, 0), 1)
                }
            };
            _phosphorGlowBrush.Freeze();

            // Tiled two-pixel pattern gives a faint noise/grain texture.
            var noiseDrawing = new DrawingGroup();
            noiseDrawing.Children.Add(new GeometryDrawing(
                new SolidColorBrush(foregroundColor), null,
                new RectangleGeometry(new Rect(0, 0, 1, 1))));
            noiseDrawing.Children.Add(new GeometryDrawing(
                new SolidColorBrush(foregroundColor), null,
                new RectangleGeometry(new Rect(2, 2, 1, 1))));
            noiseDrawing.Freeze();

            _noiseBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 4, 4),
                ViewportUnits = BrushMappingMode.Absolute,
                Drawing = noiseDrawing
            };
            _noiseBrush.Freeze();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var bounds = new Rect(0, 0, AdornedElement.RenderSize.Width, AdornedElement.RenderSize.Height);

            // Clip all overlays to the adorned element's exact bounds.
            drawingContext.PushClip(new RectangleGeometry(bounds));

            // CRT scan lines at 15% opacity.
            drawingContext.PushOpacity(0.15);
            drawingContext.DrawRectangle(_scanLinesBrush, null, bounds);
            drawingContext.Pop();

            // Screen vignette (alpha is baked into brush gradient stops).
            drawingContext.DrawRectangle(_vignetteBrush, null, bounds);

            // Phosphor glow at 12% opacity.
            drawingContext.PushOpacity(0.12);
            drawingContext.DrawRectangle(_phosphorGlowBrush, null, bounds);
            drawingContext.Pop();

            // Subtle noise texture at 2% opacity.
            drawingContext.PushOpacity(0.02);
            drawingContext.DrawRectangle(_noiseBrush, null, bounds);
            drawingContext.Pop();

            drawingContext.Pop(); // clip
        }
    }
}
