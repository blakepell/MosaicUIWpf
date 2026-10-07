/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.Specialized;
using Mosaic.UI.Wpf.Controls.Scripting.DataList;

namespace Mosaic.UI.Wpf.Scripting
{
    /// <summary>
    /// A themed, resizable window that hosts a <see cref="DataList"/>'s list view, with a row count
    /// and a Close button.  The chrome mirrors <see cref="ScriptTextWindow"/>.
    /// </summary>
    internal partial class DataListWindow : Window
    {
        private readonly ListView _listView;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataListWindow"/> class.
        /// </summary>
        /// <param name="listView">The list view to host; it is released when the window closes.</param>
        public DataListWindow(ListView listView)
        {
            InitializeComponent();

            _listView = listView;
            Host.Content = listView;
            ((INotifyCollectionChanged)listView.Items).CollectionChanged += OnItemsChanged;
            UpdateCount();

            this.Loaded += (_, _) => _listView.Focus();
            this.PreviewKeyDown += OnPreviewKeyDown;
        }

        /// <summary>
        /// Detaches the list view so it can be shown again or hosted elsewhere.
        /// </summary>
        public void ReleaseContent()
        {
            ((INotifyCollectionChanged)_listView.Items).CollectionChanged -= OnItemsChanged;
            Host.Content = null;
        }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateCount();

        private void UpdateCount()
        {
            int count = _listView.Items.Count;
            CountTextBlock.Text = count == 1 ? "1 row" : $"{count:N0} rows";
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Allows the window to be dragged by its title bar.
        /// </summary>
        private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// Closes the window when the user presses <see cref="Key.Escape"/>.
        /// </summary>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                this.Close();
            }
        }
    }
}
