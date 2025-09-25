/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;
using ICSharpCode.AvalonEdit;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// A behavior that allows various common properties of an AvalonEdit <see cref="TextEditor"/> to be dynamically set or bound.
    /// </summary>
    /// <remarks>
    /// <code><![CDATA[
    /// <avalonedit:TextEditor>
    ///    <i:Interaction.Behaviors>
    ///        <behaviors:TextEditorPropertiesBrushBehavior CaretBrush="{Binding ForegroundBrush}" />
    ///    </i:Interaction.Behaviors>
    /// </avalonedit:TextEditor>
    /// ]]></code>
    /// </remarks>
    public class AvalonEditPropertiesBehavior : Behavior<TextEditor>
    {
        /// <summary>
        /// Identifies the <see cref="CaretBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CaretBrushProperty = DependencyProperty.Register(
            nameof(CaretBrush), typeof(Brush), typeof(AvalonEditPropertiesBehavior), new PropertyMetadata(null, OnCaretBrushChanged));

        /// <summary>
        /// Gets or sets the brush used to draw the caret.
        /// </summary>
        public Brush CaretBrush
        {
            get => (Brush)GetValue(CaretBrushProperty);
            set => SetValue(CaretBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EnableHyperLinks"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableHyperLinksProperty = DependencyProperty.Register(
            nameof(EnableHyperLinks), typeof(bool), typeof(AvalonEditPropertiesBehavior), new PropertyMetadata(false, OnEnableHyperLinksChanged));

        /// <summary>
        /// Gets or sets a value indicating whether hyperlinks are enabled in the text editor.
        /// </summary>
        public bool EnableHyperLinks
        {
            get => (bool)GetValue(EnableHyperLinksProperty);
            set => SetValue(EnableHyperLinksProperty, value);
        }

        /// <summary>
        /// Called when the behavior is attached to its associated object.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            ApplyBrush();
            ApplyEnableHyperLinksChanged();

            // If property is created later, hook to Loaded to ensure we apply once available.
            AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        /// <summary>
        /// Called when the behavior is being detached from its associated object.
        /// </summary>
        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Loaded -= AssociatedObject_Loaded;
            }

            base.OnDetaching();
        }

        /// <summary>
        /// Handles the Loaded event of the associated object and applies the brush.
        /// </summary>
        /// <param name="sender">The source of the event, typically the associated object.</param>
        /// <param name="e">The event data for the Loaded event.</param>
        private void AssociatedObject_Loaded(object? sender, RoutedEventArgs e)
        {
            ApplyBrush();
        }

        /// <summary>
        /// Handles changes to the CaretBrush dependency property and applies the updated brush.
        /// </summary>
        /// <param name="d">The dependency object on which the property change occurred. Expected to be of type <see
        /// cref="AvalonEditPropertiesBehavior"/>.</param>
        /// <param name="e">Provides data about the property change, including the old and new values.</param>
        private static void OnCaretBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AvalonEditPropertiesBehavior)d).ApplyBrush();
        }

        /// <summary>
        /// Applies the specified brush to the caret of the associated text area.
        /// </summary>
        /// <remarks>This method updates the caret's appearance by setting its brush to the value of the
        /// <see cref="CaretBrush"/> property. If the associated object, text area, or caret is null, the method
        /// performs no action.</remarks>
        private void ApplyBrush()
        {
            if (AssociatedObject == null)
            {
                return;
            }

            var textArea = AssociatedObject.TextArea;

            if (textArea == null)
            {
                return;
            }

            var caret = textArea.Caret;

            if (caret == null)
            {
                return;
            }

            caret.CaretBrush = CaretBrush;
        }

        /// <summary>
        /// Handles changes to the <see cref="EnableHyperLinks"/> dependency property.
        /// </summary>
        /// <param name="d">The dependency object on which the property change occurred.</param>
        /// <param name="e">The event data containing information about the property change.</param>
        private static void OnEnableHyperLinksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AvalonEditPropertiesBehavior)d).ApplyEnableHyperLinksChanged();
        }

        /// <summary>
        /// Updates the hyperlink functionality in the associated text area based on the current setting.
        /// </summary>
        /// <remarks>This method applies the value of the <see cref="EnableHyperLinks"/> property to the  
        /// <see cref="AssociatedObject"/>'s text area options. If the <see cref="AssociatedObject"/> or its
        /// text area is <see langword="null"/>, the method exits without making changes.</remarks>
        private void ApplyEnableHyperLinksChanged()
        {
            if (AssociatedObject == null)
            {
                return;
            }

            var textArea = AssociatedObject.TextArea;

            if (textArea == null)
            {
                return;
            }

            textArea.Options.RequireControlModifierForHyperlinkClick = false;
            textArea.Options.EnableHyperlinks = EnableHyperLinks;
        }
    }
}