/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// AvalonEdit TextEditor binding behavior that allows for binding of the text property,
    /// the selected text property, the selection property, and the cursor position property.  Note,
    /// that this is useful as long as the TextEditor does not have large quantities of text.  AvalonEdit
    /// is built for large amounts of text (storing them in Ropes) and this turns those in real time into
    /// strings which can be excessively resource consuming.
    /// <![CDATA[
    /// <avalonedit:TextEditor>
    ///     <i:Interaction.Behaviors>
    ///         <behaviors:AvalonTextEditorBindingBehavior Text="{Binding TextProperty}" 
    ///                                                    SelectedText="{Binding SelectedTextProperty}" 
    ///                                                    Selection="{Binding SelectionProperty}" 
    ///                                                    CursorPosition="{Binding CursorPositionProperty}" />
    ///     </i:Interaction.Behaviors>
    /// </avalonedit:TextEditor>
    /// ]]>
    /// </summary>
    /// 
    public class AvalonTextEditorBindingBehavior : Behavior<TextEditor>
    {
        /// <summary>
        /// Attach the events needed for this behavior.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.TextChanged += AssociatedObjectTextChanged;
            AssociatedObject.TextArea.SelectionChanged += AssociatedObjectSelectionChanged;
            AssociatedObject.TextArea.Caret.PositionChanged += AssociatedObjectCaretPositionChanged;

            // Set the initial value of the Text property
            if (Text != null)
            {
                AssociatedObject.Text = Text;
            }

            // Set the initial value of the SelectedText property
            if (SelectedText != null)
            {
                AssociatedObject.SelectedText = SelectedText;
            }

            // Set the initial value of the Selection property
            var (start, length) = Selection;
            if (start != 0 || length != 0)
            {
                AssociatedObject.Select(start, length);
            }

            // Set the initial value of the CursorPosition property
            if (CursorPosition != 0)
            {
                AssociatedObject.CaretOffset = CursorPosition;
            }
        }

        /// <summary>
        /// Detach the events and cleanup resources.
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.TextChanged -= AssociatedObjectTextChanged;
            base.OnDetaching();
            AssociatedObject.TextArea.SelectionChanged -= AssociatedObjectSelectionChanged;
            AssociatedObject.TextArea.Caret.PositionChanged -= AssociatedObjectCaretPositionChanged;
        }

        /// <summary>
        /// The text property of the text editor.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// The text property of the text editor.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(AvalonTextEditorBindingBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextPropertyChanged, CoerceTextProperty));

        /// <summary>
        /// Coerce the text property of the text editor.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="baseValue"></param>
        private static object CoerceTextProperty(DependencyObject d, object baseValue)
        {
            return baseValue ?? string.Empty;
        }

        /// <summary>
        /// The text property changed event of the text editor.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AvalonTextEditorBindingBehavior b)
            {
                b.OnTextChanged();
            }
        }

        /// <summary>
        /// The text property changed event of the text editor.
        /// </summary>
        private void OnTextChanged()
        {
            if (AssociatedObject is null)
            {
                return;
            }

            if (AssociatedObject.Text != Text)
            {
                AssociatedObject.Text = Text ?? string.Empty;
            }
        }

        /// <summary>
        /// The associated object text changed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssociatedObjectTextChanged(object? sender, EventArgs e)
        {
            if (Text != AssociatedObject.Text)
            {
                this.SetCurrentValue(TextProperty, AssociatedObject.Text ?? string.Empty);
            }
        }

        /// <summary>
        /// The selected text property of the text editor.
        /// </summary>
        public string SelectedText
        {
            get => (string)GetValue(SelectedTextProperty);
            set => SetValue(SelectedTextProperty, value);
        }

        /// <summary>
        /// The selected text property of the text editor.
        /// </summary>
        public static readonly DependencyProperty SelectedTextProperty =
            DependencyProperty.Register(nameof(SelectedText), typeof(string), typeof(AvalonTextEditorBindingBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTextPropertyChanged));

        /// <summary>
        /// The selected text property changed of the text editor.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnSelectedTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AvalonTextEditorBindingBehavior b)
            {
                b.OnSelectedTextChanged();
            }
        }

        /// <summary>
        /// The selected text property changed event of text editor.  
        /// </summary>
        private void OnSelectedTextChanged()
        {
            if (AssociatedObject is null)
            {
                return;
            }

            if (AssociatedObject.SelectedText != SelectedText)
            {
                AssociatedObject.SelectedText = SelectedText ?? string.Empty;
            }
        }

        /// <summary>
        /// The selection property of the text editor.
        /// </summary>
        public (int start, int length) Selection
        {
            get => ((int start, int length))GetValue(SelectionProperty);
            set => SetValue(SelectionProperty, value);
        }

        /// <summary>
        /// The selection property of the text editor.
        /// </summary>
        public static readonly DependencyProperty SelectionProperty =
            DependencyProperty.Register(nameof(Selection), typeof((int start, int length)), typeof(AvalonTextEditorBindingBehavior), 
                new FrameworkPropertyMetadata((0, 0), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionPropertyChanged));

        /// <summary>
        /// The selection property changed event of the text editor.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnSelectionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AvalonTextEditorBindingBehavior b)
            {
                b.OnSelectionChanged();
            }
        }

        /// <summary>
        /// The selection property changed event of the text editor.
        /// </summary>
        private void OnSelectionChanged()
        {
            if (AssociatedObject is null)
            {
                return;
            }

            var associatedObjectSelection = (AssociatedObject.SelectionStart, AssociatedObject.SelectionLength);
            if (associatedObjectSelection != Selection)
            {
                var (start, end) = Selection;
                AssociatedObject.Select(start, end);
            }
        }

        /// <summary>
        /// The associated object selection changed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssociatedObjectSelectionChanged(object? sender, EventArgs e)
        {
            if (SelectedText != AssociatedObject.SelectedText)
            {
                this.SetCurrentValue(SelectedTextProperty, AssociatedObject.SelectedText ?? string.Empty);
            }

            var associatedObjectSelection = (AssociatedObject.SelectionStart, AssociatedObject.SelectionLength);
            if (Selection != associatedObjectSelection)
            {
                this.SetCurrentValue(SelectionProperty, associatedObjectSelection);
            }
        }

        /// <summary>
        /// The cursor position property of the text editor.
        /// </summary>
        public int CursorPosition
        {
            get => (int)GetValue(CursorPositionProperty);
            set => SetValue(CursorPositionProperty, value);
        }

        /// <summary>
        /// The cursor position property of the text editor.
        /// </summary>
        public static readonly DependencyProperty CursorPositionProperty =
            DependencyProperty.Register(nameof(CursorPosition), typeof(int), typeof(AvalonTextEditorBindingBehavior), 
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCursorPositionPropertyChanged));

        /// <summary>
        /// The cursor position property changed event of the text editor.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnCursorPositionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AvalonTextEditorBindingBehavior b)
            {
                b.OnCursorPositionChanged();
            }
        }

        /// <summary>
        /// The cursor position changed event of the text editor.
        /// </summary>
        private void OnCursorPositionChanged()
        {
            if (AssociatedObject is null)
            {
                return;
            }

            if (AssociatedObject.CaretOffset != CursorPosition)
            {
                AssociatedObject.CaretOffset = CursorPosition;
            }
        }

        /// <summary>
        /// The associated object caret position changed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AssociatedObjectCaretPositionChanged(object? sender, EventArgs e)
        {
            if (CursorPosition != AssociatedObject.CaretOffset)
            {
                this.SetCurrentValue(CursorPositionProperty, AssociatedObject.CaretOffset);
            }
        }
    }
}
