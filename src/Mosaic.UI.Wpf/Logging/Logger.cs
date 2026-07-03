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

using Argus.Collections;
using CommunityToolkit.Mvvm.Input;
using Cysharp.Text;
using Microsoft.Win32;

namespace Mosaic.UI.Wpf.Logging
{
    /// <summary>
    /// Logger class
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// The level at which log entries should be processed.
        /// </summary>
        public static LogSeverity LogLevel { get; set; } = LogSeverity.Info;

        /// <summary>
        /// Collection of in memory held log entries.
        /// </summary>
        public static CircularObservableCollection<LogEntry> LogEntries { get; set; }

        /// <summary>
        /// If duplicate entries should be consolidated.
        /// </summary>
        public static bool ConsolidateDuplicates { get; set; } = false;

        /// <summary>
        /// The default context menu if one exists.
        /// </summary>
        public static ContextMenu? ContextMenu { get; set; }

        /// <summary>
        /// Logger class
        /// </summary>
        static Logger()
        {
            LogEntries = new();
        }

        /// <summary>
        /// Sets the capacity for the circular buffer.
        /// </summary>
        /// <param name="capacity"></param>
        public static void SetCapacity(int capacity)
        {
            LogEntries.Capacity = capacity;
        }

        /// <summary>
        /// Makes a debug log entry.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ipAddress">The IP Address associated with the log entry if one exists.</param>
        /// <param name="username">The username of the associated log entry if one exists.</param>
        /// <param name="contextMenu"></param>
        public static void LogDebug(string message, string? ipAddress = Constants.NotAvailable, string? username = Constants.NotAvailable, ContextMenu? contextMenu = null)
        {
            Log(LogSeverity.Debug, message, ipAddress, username, contextMenu: contextMenu);
        }

        /// <summary>
        /// Makes an info log entry.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ipAddress">The IP Address associated with the log entry if one exists.</param>
        /// <param name="username">The username of the associated log entry if one exists.</param>
        /// <param name="contextMenu"></param>
        public static void LogInfo(string message, string? ipAddress = Constants.NotAvailable, string? username = Constants.NotAvailable, ContextMenu? contextMenu = null)
        {
            Log(LogSeverity.Info, message, ipAddress, username, contextMenu: contextMenu);
        }

        /// <summary>
        /// Makes a success log entry.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ipAddress">The IP Address associated with the log entry if one exists.</param>
        /// <param name="username">The username of the associated log entry if one exists.</param>
        /// <param name="contextMenu"></param>
        public static void LogSuccess(string message, string? ipAddress = Constants.NotAvailable, string? username = Constants.NotAvailable, ContextMenu? contextMenu = null)
        {
            Log(LogSeverity.Success, message, ipAddress, username, contextMenu: contextMenu);
        }

        /// <summary>
        /// Makes an error warning log entry.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ipAddress">The IP Address associated with the log entry if one exists.</param>
        /// <param name="username">The username of the associated log entry if one exists.</param>
        /// <param name="contextMenu"></param>
        public static void LogWarning(string message, string? ipAddress = Constants.NotAvailable, string? username = Constants.NotAvailable, ContextMenu? contextMenu = null)
        {
            Log(LogSeverity.Warning, message, ipAddress, username, contextMenu: contextMenu);
        }

        /// <summary>
        /// Makes an error log entry.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ipAddress">The IP Address associated with the log entry if one exists.</param>
        /// <param name="username">The username of the associated log entry if one exists.</param>
        /// <param name="contextMenu"></param>
        /// <param name="showMessageBox">If a message box with the Exception message should be shown.</param>
        public static void LogError(string message, string? ipAddress = Constants.NotAvailable, string? username = Constants.NotAvailable, ContextMenu? contextMenu = null, bool showMessageBox = false)
        {
            Log(LogSeverity.Error, message, ipAddress, username, contextMenu: contextMenu);

            if (showMessageBox)
            {
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Makes an error log entry.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="ipAddress">The IP Address associated with the log entry if one exists.</param>
        /// <param name="username">The username of the associated log entry if one exists.</param>
        /// <param name="contextMenu"></param>
        /// <param name="showMessageBox">If a message box with the Exception message should be shown.</param>
        public static void LogError(Exception ex, string? ipAddress = Constants.NotAvailable, string? username = Constants.NotAvailable, ContextMenu? contextMenu = null, bool showMessageBox = false)
        {
            Log(LogSeverity.Error, ex.Message, ipAddress, username, contextMenu: contextMenu, ex: ex);

            if (showMessageBox)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Makes the log entry of the specified type.
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        /// <param name="ipAddress">The IP Address associated with the log entry if one exists.</param>
        /// <param name="username">The username of the associated log entry if one exists.</param>
        /// <param name="copyObject"></param>
        /// <param name="contextMenu"></param>
        /// <param name="referenceId"></param>
        public static void Log(LogSeverity severity, string message, string? ipAddress = Constants.NotAvailable, string? username = Constants.NotAvailable, object? copyObject = null, ContextMenu? contextMenu = null, string? referenceId = null, Exception? ex = null)
        {
            if (severity < LogLevel)
            {
                return;
            }

            if (username == Constants.NotAvailable)
            {
                username = $"{Environment.UserDomainName}\\{Environment.UserName}";
            }

            // Invoke dispatcher
            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                var entry = new LogEntry
                {
                    Message = message,
                    Severity = severity,
                    IpAddress = ipAddress,
                    Username = username,
                    ContextMenu = contextMenu,
                    CopyObject = copyObject ?? message,
                    ReferenceId = referenceId,
                    Exception = ex
                };

                if (ConsolidateDuplicates)
                {
                    var lastEntry = Logger.LogEntries.LastOrDefault();

                    if (lastEntry != null && lastEntry.Message == entry.Message && lastEntry.Username == entry.Username && lastEntry.IpAddress == entry.IpAddress)
                    {
                        lastEntry.Time = DateTime.Now;
                        lastEntry.Count++;
                        return;
                    }
                }

                Logger.LogEntries.Add(entry);

                var logViewer = AppServices.GetService<LogViewer>();

                if (logViewer == null)
                {
                    return;
                }

                // If the log viewer isn't selected currently (or it's minimized) then increment
                // the pending notifications.
                if (!logViewer.IsSelected)
                {
                    logViewer.NotificationCount++;
                }

                logViewer.ListView.ScrollIntoView(entry);
            });
        }

        /// <summary>
        /// Starts a bulk update of entries.  If large amounts of entries are made at once this will keep
        /// the UI active and only notify the logger's ListView to update once at the end.
        /// </summary>
        public static void BeginUpdate()
        {
            LogEntries.BeginUpdate();
        }

        /// <summary>
        /// Ends a bulk update of entries.  Notification will be sent that the log entires collection has
        /// updated.
        /// </summary>
        public static void EndUpdate()
        {
            LogEntries.EndUpdate();
        }

        /// <summary>
        /// Clears all the log entries that are held in memory.
        /// </summary>
        public static ICommand ClearCommand { get; } = new RelayCommand(Clear);

        /// <summary>
        /// Clears all the log entries that are held in memory.
        /// </summary>
        public static void Clear()
        {
            Application.Current.Dispatcher.InvokeIfRequired(() =>
            {
                Logger.LogEntries.Clear();
            });
        }

        /// <summary>
        /// Saves the log entries to disk prompting the user to set the save file.
        /// </summary>
        public static ICommand SaveAsCommand { get; } = new AsyncRelayCommand(SaveAs);

        /// <summary>
        /// Saves the log entries to disk prompting the user to set the save file.
        /// </summary>
        public static async Task SaveAs()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Select Export File",
                Filter = "Text Files (*.txt)|*.txt|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) != true || string.IsNullOrWhiteSpace(dialog.FileName))
            {
                return;
            }

            using var sb = ZString.CreateStringBuilder();

            await using (var writer = new StreamWriter(dialog.FileName))
            {
                foreach (var logEntry in Logger.LogEntries)
                {
                    if (string.IsNullOrWhiteSpace(logEntry.Message))
                    {
                        continue;
                    }

                    sb.Append(logEntry.Time);
                    sb.Append(" :: ");
                    sb.Append(" [ ");
                    sb.Append(logEntry.Severity);
                    sb.Append(" ] :: ");
                    sb.Append(logEntry.Message);
                    sb.Append("\r\n");

                    await writer.WriteAsync(sb.ToString());

                    sb.Clear();
                }
            }
        }
    }
}
