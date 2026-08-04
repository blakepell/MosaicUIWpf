/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Transfers;

namespace BbsNavigator.Networking
{
    /// <summary>
    /// Defines the transfer link a connection hands to a file transfer protocol. Disposing
    /// the channel returns the connection to normal terminal operation.
    /// </summary>
    public interface IBbsBinaryChannel : ITransferLink, IDisposable
    {
    }
}
