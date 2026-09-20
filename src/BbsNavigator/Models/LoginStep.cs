/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace BbsNavigator.Models;

/// <summary>
/// Identifies an action in a prompt-aware login sequence.
/// </summary>
public enum LoginAction
{
    /// <summary>
    /// Waits for literal, case-insensitive prompt text.
    /// </summary>
    WaitForText,
    /// <summary>
    /// Sends literal text.
    /// </summary>
    SendText,
    /// <summary>
    /// Sends the encrypted profile's username.
    /// </summary>
    SendUsername,
    /// <summary>
    /// Sends the encrypted profile's password.
    /// </summary>
    SendPassword,
    /// <summary>
    /// Sends a terminal Enter key.
    /// </summary>
    Enter,
    /// <summary>
    /// Waits for the specified duration.
    /// </summary>
    Delay
}

/// <summary>
/// Stores one login action without copying credentials into the sequence.
/// </summary>
public sealed class LoginStep
{
    /// <summary>
    /// Gets or sets the action to execute.
    /// </summary>
    public LoginAction Action { get; set; }

    /// <summary>
    /// Gets or sets the literal prompt or text to send.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prompt timeout or delay, in seconds.
    /// </summary>
    public int Seconds { get; set; } = 30;
}
