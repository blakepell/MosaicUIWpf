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

using Cysharp.Text;

// ReSharper disable InconsistentNaming

namespace Mosaic.UI.Wpf.Logging
{
    /// <summary>
    /// Log entry model.
    /// </summary>
    public partial class LogEntry : ObservableObject
    {
        /// <summary>
        /// The time the log entry occurred.
        /// </summary>
        [ObservableProperty]
        private DateTime _time = DateTime.Now;

        /// <summary>
        /// The severity of the log entry.
        /// </summary>
        [ObservableProperty]
        private LogSeverity _severity = LogSeverity.Info;

        /// <summary>
        /// The descriptive message of the log entry.
        /// </summary>
        [ObservableProperty]
        private string? _message;

        /// <summary>
        /// The IP Address if the log entry has one attached.
        /// </summary>
        [ObservableProperty]
        private string? _ipAddress = Constants.NotAvailable;

        /// <summary>
        /// The username who is associated with the log entry.
        /// </summary>
        [ObservableProperty]
        private string? _username = Constants.NotAvailable;

        /// <summary>
        /// If the same log entry has been repeated at the end of the list, a counter so consolidate multiple entries.
        /// </summary>
        [ObservableProperty]
        private int _count = 1;

        /// <summary>
        /// An optional reference ID that can be used to identify related records.
        /// </summary>
        [ObservableProperty]
        private string? _referenceId;

        /// <summary>
        /// A context menu that should be shown for this log entry.
        /// </summary>
        [ObservableProperty]
        private ContextMenu? _contextMenu;

        /// <summary>
        /// An object that will be copied into the clipboard 
        /// </summary>
        public object? CopyObject;

        /// <summary>  
        /// Gets or sets the exception associated with the log entry.  
        /// </summary>  
        public Exception? Exception { get; set; }

        /// <summary>
        /// Returns an informative string of the data contained within the LogEntry.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            using (var sb = ZString.CreateStringBuilder())
            {
                sb.Append($"{Severity}: {Message}");
                return sb.ToString();
            }
        }
    }
}
