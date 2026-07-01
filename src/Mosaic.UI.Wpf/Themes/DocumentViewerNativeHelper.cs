/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Mosaic.UI.Wpf.Themes
{
    /// <summary>
    /// Attached behavior that applies the Mosaic native <see cref="ToolBar"/> style to a
    /// <see cref="DocumentViewer"/>'s built-in find toolbar (the bar shown for Ctrl+F).
    /// </summary>
    /// <remarks>
    /// WPF's find toolbar (<c>MS.Internal.Documents.FindToolBar</c>) is loaded from PresentationUI
    /// and declares its own <c>{x:Type ToolBar}</c> style in its element resources. That local style
    /// shadows the Mosaic native ToolBar style, so the find toolbar's chrome (background, grip,
    /// overflow) is not themed by default. There is no XAML/theme hook to override element-local
    /// resources on a framework-internal control, so this helper reaches the hosted toolbar through
    /// the public template part and assigns the themed style explicitly. It is wired automatically by
    /// the native <c>DocumentViewer</c> style; it is a no-op when the themed style cannot be found.
    /// </remarks>
    public static class DocumentViewerNativeHelper
    {
        private const string FindToolBarHostPartName = "PART_FindToolBarHost";

        /// <summary>
        /// Identifies the ThemeFindToolBar attached dependency property.
        /// </summary>
        public static readonly DependencyProperty ThemeFindToolBarProperty = DependencyProperty.RegisterAttached(
            "ThemeFindToolBar", typeof(bool), typeof(DocumentViewerNativeHelper),
            new PropertyMetadata(false, OnThemeFindToolBarChanged));

        /// <summary>
        /// Sets a value indicating whether the DocumentViewer's find toolbar should be styled with
        /// the Mosaic native ToolBar style.
        /// </summary>
        public static void SetThemeFindToolBar(DependencyObject element, bool value) => element.SetValue(ThemeFindToolBarProperty, value);

        /// <summary>
        /// Gets a value indicating whether the DocumentViewer's find toolbar should be styled with
        /// the Mosaic native ToolBar style.
        /// </summary>
        public static bool GetThemeFindToolBar(DependencyObject element) => (bool)element.GetValue(ThemeFindToolBarProperty);

        private static void OnThemeFindToolBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DocumentViewer dv)
            {
                return;
            }

            if (e.NewValue is true)
            {
                dv.Loaded += OnDocumentViewerLoaded;

                if (dv.IsLoaded)
                {
                    ApplyFindToolBarStyle(dv);
                }
            }
            else
            {
                dv.Loaded -= OnDocumentViewerLoaded;
            }
        }

        private static void OnDocumentViewerLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is DocumentViewer dv)
            {
                ApplyFindToolBarStyle(dv);
            }
        }

        /// <summary>
        /// Locates the hosted find toolbar and applies the themed ToolBar style to it.
        /// </summary>
        private static void ApplyFindToolBarStyle(DocumentViewer dv)
        {
            // The find toolbar is created during OnApplyTemplate and hosted as the content of the
            // PART_FindToolBarHost content control. It may not be present on the very first pass, so
            // retry once at background priority if it is not found yet.
            if (dv.Template?.FindName(FindToolBarHostPartName, dv) is ContentControl { Content: ToolBar findToolBar })
            {
                if (dv.TryFindResource(typeof(ToolBar)) is Style toolBarStyle)
                {
                    findToolBar.Style = toolBarStyle;
                }

                return;
            }

            dv.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                if (dv.Template?.FindName(FindToolBarHostPartName, dv) is ContentControl { Content: ToolBar findToolBar }
                    && dv.TryFindResource(typeof(ToolBar)) is Style toolBarStyle)
                {
                    findToolBar.Style = toolBarStyle;
                }
            }));
        }
    }
}
