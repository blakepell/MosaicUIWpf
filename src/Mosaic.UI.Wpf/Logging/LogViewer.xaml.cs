/*
 * ApexGateUI
 *
 * @project lead      : Blake Pell
 * @company           : ApexGate
 * @website           : https://www.apexgate.net
 * @website           : https://www.blakepell.com
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : Closed Source
 */

using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using Clipboard = System.Windows.Clipboard;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;
using TextDataFormat = System.Windows.TextDataFormat;

namespace Mosaic.UI.Wpf.Logging
{
    /// <summary>
    /// LogViewer control displayed in a formatted ListView.
    /// </summary>
    public partial class LogViewer : UserControl
    {
        /// <summary>
        /// If the copy log entry functionality is enabled.
        /// </summary>
        public static readonly DependencyProperty CopyEnabledProperty = DependencyProperty.Register(
            nameof(CopyEnabled), typeof(bool), typeof(LogViewer), new PropertyMetadata(true));

        /// <summary>
        /// If the copy log entry functionality is enabled.
        /// </summary>
        public bool CopyEnabled
        {
            get => (bool)GetValue(CopyEnabledProperty);
            set => SetValue(CopyEnabledProperty, value);
        }

        /// <summary>
        /// If the IP Address column is visible.
        /// </summary>
        public static readonly DependencyProperty IpAddressVisibleProperty = DependencyProperty.Register(
            nameof(IpAddressVisible), typeof(bool), typeof(LogViewer), new PropertyMetadata(true));

        /// <summary>
        /// If the IP Address column is visible.
        /// </summary>
        public bool IpAddressVisible
        {
            get => (bool)GetValue(IpAddressVisibleProperty);
            set => SetValue(IpAddressVisibleProperty, value);
        }

        /// <summary>
        /// If the Username column is visible.
        /// </summary>
        public static readonly DependencyProperty UsernameVisibleProperty = DependencyProperty.Register(
            nameof(UsernameVisible), typeof(bool), typeof(LogViewer), new PropertyMetadata(true));

        /// <summary>
        /// If the Username column is visible.
        /// </summary>
        public bool UsernameVisible
        {
            get => (bool)GetValue(UsernameVisibleProperty);
            set => SetValue(UsernameVisibleProperty, value);
        }

        /// <summary>
        /// If the Count column is visible.
        /// </summary>
        public static readonly DependencyProperty CountVisibleProperty = DependencyProperty.Register(
            nameof(CountVisible), typeof(bool), typeof(LogViewer), new PropertyMetadata(true));

        /// <summary>
        /// If the Count column is visible.
        /// </summary>
        public bool CountVisible
        {
            get => (bool)GetValue(CountVisibleProperty);
            set => SetValue(CountVisibleProperty, value);
        }

        /// <summary>
        /// If the Date/Time column is visible.
        /// </summary>
        public static readonly DependencyProperty DateTimeVisibleProperty = DependencyProperty.Register(
            nameof(DateTimeVisible), typeof(bool), typeof(LogViewer), new PropertyMetadata(true));

        /// <summary>
        /// If the Date/Time column is visible.
        /// </summary>
        public bool DateTimeVisible
        {
            get => (bool)GetValue(DateTimeVisibleProperty);
            set => SetValue(DateTimeVisibleProperty, value);
        }

        /// <summary>
        /// If the Time only column is visible.
        /// </summary>
        public static readonly DependencyProperty TimeVisibleProperty = DependencyProperty.Register(
            nameof(TimeVisible), typeof(bool), typeof(LogViewer), new PropertyMetadata(false));

        /// <summary>
        /// If the Time only column is visible.
        /// </summary>
        public bool TimeVisible
        {
            get => (bool)GetValue(TimeVisibleProperty);
            set => SetValue(TimeVisibleProperty, value);
        }

        /// <summary>
        /// The number of unread notifications while the window was not visible.
        /// </summary>
        public static readonly DependencyProperty NotificationCountProperty = DependencyProperty.Register(
            nameof(NotificationCount), typeof(int), typeof(LogViewer), new PropertyMetadata(default(int)));

        /// <summary>
        /// The number of unread notifications while the window was not visible.
        /// </summary>
        public int NotificationCount
        {
            get => (int)GetValue(NotificationCountProperty);
            set => SetValue(NotificationCountProperty, value);
        }

        /// <summary>
        /// If the log viewer is selected in its host docking surface.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(LogViewer), new PropertyMetadata(default(bool), OnIsSelectedChanged));

        /// <summary>
        /// If the log viewer is selected in its host docking surface.
        /// </summary>
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        /// <summary>
        /// LogViewer control displayed in a formatted ListView.
        /// </summary>
        public LogViewer()
        {
            if (!AppServices.IsRegistered(this.GetType()))
            {
                AppServices.AddSingleton(this);
            }

            this.DataContext = this;
            InitializeComponent();

            Logger.ContextMenu = LogViewerContextMenu;
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LogViewer { IsSelected: true } logViewer)
            {
                logViewer.NotificationCount = 0;
            }
        }

        /// <summary>
        /// Removes the specified column.
        /// </summary>
        /// <param name="columnName"></param>
        public void RemoveColumn(string columnName)
        {
            this.ListView.RemoveHeader(columnName);
        }

        /// <summary>
        /// Clears all the log entries from the list box.
        /// </summary>
        public static ICommand ClearCommand { get; } = new RelayCommand(Clear);

        /// <summary>
        /// Clears all the log entries from the list box.
        /// </summary>
        public static void Clear()
        {
            Logger.LogEntries.Clear();
        }

        /// <summary>
        /// Clears all the log entries from the list box.
        /// </summary>
        public static ICommand ClearAllCommand { get; } = new RelayCommand(ClearAll);

        /// <summary>
        /// Clears all the log entries from the list box.
        /// </summary>
        public static void ClearAll()
        {
            Logger.LogEntries.Clear();
        }

        /// <summary>
        /// Copies the selected item to the clipboard.
        /// </summary>
        public static ICommand CopyCommand { get; } = new RelayCommand<ListView?>(Copy);

        /// <summary>
        /// Copies the selected item to the clipboard.
        /// </summary>
        /// <param name="lv"></param>
        public static void Copy(ListView? lv)
        {
            if (lv.SelectedItem is LogEntry { CopyObject: not null } logEntry)
            {
                if (logEntry.CopyObject is string str)
                {
                    Clipboard.SetText(str, TextDataFormat.Text);
                }
                else
                {
                    Clipboard.SetText(JsonSerializer.Serialize(logEntry.CopyObject));
                }
            }
        }

        /// <summary>
        /// Search the log viewer.
        /// </summary>
        public static ICommand SearchCommand { get; } = new RelayCommand(Search);

        /// <summary>
        /// Search the log viewer.
        /// </summary>
        public static void Search()
        {
            // TODO: Implement search functionality.  This was added to fix a binding error in Continuum.
        }

        /// <summary>
        /// Command to view the exception details of the selected log entry.
        /// </summary>
        public static ICommand ViewExceptionCommand { get; } = new RelayCommand<ListView?>(ViewException);

        /// <summary>
        /// Command to view the exception details of the selected log entry.
        /// </summary>
        public static void ViewException(ListView? lv)
        {
            if (lv == null)
            {
                return;
            }

            if (lv.SelectedItem is LogEntry { Exception: not null } logEntry)
            {
                if (logEntry.Exception is Exception ex)
                {
                    var dialog = new Window
                    {
                        Owner = Application.Current.MainWindow,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Title = "Exception Details",
                        Width = 900,
                        Height = 600,
                        Content = new TextBox
                        {
                            AcceptsReturn = true,
                            AcceptsTab = true,
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 12,
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                            IsReadOnly = true,
                            Text = ex.ToFormattedString(),
                            TextWrapping = TextWrapping.NoWrap,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                        }
                    };

                    dialog.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Fire off an event if a LogEntry is the source of a double click.
        /// </summary>
        public new event EventHandler<LogEntry>? OnMouseDoubleClick;

        /// <summary>
        /// Fire off an event if a LogEntry is the source of a double click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListItem_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            // Log for the LogEntry object to bubble up to the point of contact.
            if (e.Source is ListViewItem { Content: LogEntry entry })
            {
                OnMouseDoubleClick?.Invoke(this, entry);
                return;
            }

            // Probably won't ever get here but in case it does.
            if (e.Source is LogEntry le)
            {
                OnMouseDoubleClick?.Invoke(this, le);
                return;
            }
        }
    }
}
