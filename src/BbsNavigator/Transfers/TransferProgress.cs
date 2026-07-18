/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace BbsNavigator.Transfers
{
    /// <summary>
    /// Describes a point-in-time snapshot of a file transfer used for progress reporting.
    /// </summary>
    /// <param name="FileName">The name of the file currently being transferred.</param>
    /// <param name="BytesTransferred">The number of file bytes moved so far for the current file.</param>
    /// <param name="FileSize">The current file's size in bytes, or -1 when unknown.</param>
    /// <param name="FileIndex">The one-based index of the current file in the batch.</param>
    /// <param name="Message">A short human readable status such as "Sending block 12".</param>
    public readonly record struct TransferSnapshot(
        string FileName,
        long BytesTransferred,
        long FileSize,
        int FileIndex,
        string Message);

    /// <summary>
    /// Summarizes a completed transfer session.
    /// </summary>
    /// <param name="Files">The file names that completed successfully.</param>
    /// <param name="TotalBytes">The total number of file bytes moved.</param>
    /// <param name="Elapsed">The wall clock duration of the session.</param>
    public readonly record struct TransferResult(IReadOnlyList<string> Files, long TotalBytes, TimeSpan Elapsed);

    /// <summary>
    /// The exception thrown when a transfer fails or is aborted by either side.
    /// </summary>
    public sealed class TransferException : Exception
    {
        /// <summary>
        /// Initializes the exception with a description of the failure.
        /// </summary>
        /// <param name="message">A description of the failure.</param>
        public TransferException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes the exception with a description and the underlying cause.
        /// </summary>
        /// <param name="message">A description of the failure.</param>
        /// <param name="innerException">The underlying cause.</param>
        public TransferException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
