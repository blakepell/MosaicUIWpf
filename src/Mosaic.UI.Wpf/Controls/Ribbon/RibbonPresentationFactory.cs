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
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Executes commands with the policy every Ribbon presentation shares: a routed command without an
    /// explicit target routes from the source (and, because the Ribbon is a focus scope, on to the
    /// document's focused element); a command that cannot execute never runs.
    /// </summary>
    public static class RibbonCommandHelper
    {
        /// <summary>
        /// Returns whether the command can execute with the given parameter and target.
        /// </summary>
        public static bool CanExecute(ICommand? command, object? parameter, IInputElement? target, IInputElement source)
        {
            if (command == null)
            {
                return true;
            }

            return command is RoutedCommand routed
                ? routed.CanExecute(parameter, target ?? source)
                : command.CanExecute(parameter);
        }

        /// <summary>
        /// Executes the command if it can execute.
        /// </summary>
        /// <returns><c>true</c> if the command ran or no command was assigned.</returns>
        public static bool Execute(ICommand? command, object? parameter, IInputElement? target, IInputElement source)
        {
            if (command == null)
            {
                return true;
            }

            if (!CanExecute(command, parameter, target, source))
            {
                return false;
            }

            if (command is RoutedCommand routed)
            {
                routed.Execute(parameter, target ?? source);
            }
            else
            {
                command.Execute(parameter);
            }

            return true;
        }
    }

    /// <summary>
    /// Popup helpers used by Ribbon templates.
    /// </summary>
    public static class RibbonPopupService
    {
        /// <summary>
        /// Identifies the CloseOnEscape attached property. When set on a <see cref="Popup"/>, Escape closes the
        /// popup (innermost first) and returns focus to its placement target.
        /// </summary>
        public static readonly DependencyProperty CloseOnEscapeProperty = DependencyProperty.RegisterAttached(
            "CloseOnEscape", typeof(bool), typeof(RibbonPopupService), new FrameworkPropertyMetadata(false, OnCloseOnEscapeChanged));

        /// <summary>Gets whether Escape closes the popup.</summary>
        public static bool GetCloseOnEscape(DependencyObject element) => (bool)element.GetValue(CloseOnEscapeProperty);

        /// <summary>Sets whether Escape closes the popup.</summary>
        public static void SetCloseOnEscape(DependencyObject element, bool value) => element.SetValue(CloseOnEscapeProperty, value);

        private static void OnCloseOnEscapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Popup popup)
            {
                return;
            }

            popup.PreviewKeyDown -= OnPopupPreviewKeyDown;
            if ((bool)e.NewValue)
            {
                popup.PreviewKeyDown += OnPopupPreviewKeyDown;
            }
        }

        private static void OnPopupPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape || e.Handled || sender is not Popup popup || e.OriginalSource is MenuItem { IsSubmenuOpen: true })
            {
                return;
            }

            popup.SetCurrentValue(Popup.IsOpenProperty, false);
            (popup.PlacementTarget as UIElement)?.Focus();
            e.Handled = true;
        }
    }

    /// <summary>
    /// A context menu styled with the Ribbon theme tokens, used for the Ribbon's display options and
    /// Quick Access Toolbar menus.
    /// </summary>
    public class RibbonContextMenu : ContextMenu
    {
        static RibbonContextMenu()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonContextMenu), new FrameworkPropertyMetadata(typeof(RibbonContextMenu)));
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) => item is MenuItem or Separator;

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride() => new RibbonMenuItem();
    }

    /// <summary>
    /// Creates the generated presentations (overflow rows and Quick Access Toolbar buttons) for Ribbon
    /// commands. Each presentation is independently parented and bound to the source's shared state, so a
    /// WPF element never has two visual parents and checked/value state cannot diverge.
    /// </summary>
    public static class RibbonPresentationFactory
    {
        /// <summary>
        /// Binds the shared visual identity (label, images, glyph, description) of a source to a presentation.
        /// </summary>
        public static void BindVisuals(FrameworkElement presentation, FrameworkElement source)
        {
            Bind(presentation, RibbonItem.LabelProperty, source, RibbonItem.LabelProperty);
            Bind(presentation, RibbonItem.SmallImageProperty, source, RibbonItem.SmallImageProperty);
            Bind(presentation, RibbonItem.LargeImageProperty, source, RibbonItem.LargeImageProperty);
            Bind(presentation, RibbonItem.GlyphProperty, source, RibbonItem.GlyphProperty);
            Bind(presentation, RibbonItem.GlyphBrushProperty, source, RibbonItem.GlyphBrushProperty);
            Bind(presentation, RibbonItem.DescriptionProperty, source, RibbonItem.DescriptionProperty);
            Bind(presentation, RibbonItem.KeyGestureTextProperty, source, RibbonItem.KeyGestureTextProperty);
            Bind(presentation, UIElement.IsEnabledProperty, source, UIElement.IsEnabledProperty);
            RibbonItem.SetSourceKey(presentation, RibbonItem.GetKey(source));
            BindAutomationName(presentation, source);
        }

        /// <summary>
        /// Keeps a presentation's automation name in step with the source label, so a relabeled command (for
        /// example after a language switch) is announced correctly on every surface.
        /// </summary>
        private static void BindAutomationName(FrameworkElement presentation, FrameworkElement source)
        {
            BindingOperations.SetBinding(presentation, System.Windows.Automation.AutomationProperties.NameProperty, new Binding
            {
                Source = source,
                Path = new PropertyPath(RibbonItem.LabelProperty),
                Mode = BindingMode.OneWay,
                TargetNullValue = string.Empty,
                FallbackValue = string.Empty
            });
        }

        /// <summary>
        /// Creates a one-way binding from <paramref name="sourceProperty"/> on <paramref name="source"/>.
        /// </summary>
        public static void Bind(DependencyObject target, DependencyProperty targetProperty, object source, DependencyProperty sourceProperty, BindingMode mode = BindingMode.OneWay)
        {
            BindingOperations.SetBinding(target, targetProperty, new Binding { Source = source, Path = new PropertyPath(sourceProperty), Mode = mode });
        }

        /// <summary>
        /// Creates a compact button that forwards activation to <paramref name="invoke"/>.
        /// </summary>
        internal static RibbonButton CreateButton(FrameworkElement source, Action invoke)
        {
            var button = new RibbonButton { ForwardClick = invoke };
            BindVisuals(button, source);
            return button;
        }

        /// <summary>
        /// Creates a compact toggle that mirrors <see cref="System.Windows.Controls.Primitives.ToggleButton.IsChecked"/> of the source and forwards
        /// activation to it.
        /// </summary>
        internal static RibbonToggleButton CreateToggle(System.Windows.Controls.Primitives.ToggleButton source, Action invoke)
        {
            var toggle = new RibbonToggleButton { ForwardClick = invoke };
            BindVisuals(toggle, source);
            Bind(toggle, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, source, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty);
            return toggle;
        }

        /// <summary>
        /// Creates an overflow menu row that forwards activation to <paramref name="invoke"/>.
        /// </summary>
        internal static RibbonMenuItem CreateMenuItem(FrameworkElement source, Action invoke, System.Windows.Controls.Primitives.ToggleButton? checkSource = null)
        {
            var item = new RibbonMenuItem { ForwardClick = invoke };
            BindVisuals(item, source);
            Bind(item, HeaderedItemsControl.HeaderProperty, source, RibbonItem.LabelProperty);

            if (checkSource != null)
            {
                item.IsCheckable = true;
                Bind(item, MenuItem.IsCheckedProperty, checkSource, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty);
            }

            return item;
        }

        /// <summary>
        /// Wraps an editor presentation in a labeled overflow row.
        /// </summary>
        internal static FrameworkElement CreateEditorRow(FrameworkElement source, FrameworkElement editor)
        {
            var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), MinWidth = 60 };
            Bind(label, TextBlock.TextProperty, source, RibbonItem.LabelProperty);

            var panel = new DockPanel { Margin = new Thickness(28, 3, 8, 3), LastChildFill = true };
            DockPanel.SetDock(label, Dock.Left);
            panel.Children.Add(label);
            panel.Children.Add(editor);
            RibbonItem.SetSourceKey(panel, RibbonItem.GetKey(source));
            BindAutomationName(editor, source);
            return panel;
        }
    }
}
