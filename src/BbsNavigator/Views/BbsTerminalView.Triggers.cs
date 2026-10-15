/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Common;
using Trigger = BbsNavigator.Models.Trigger;

namespace BbsNavigator.Views;

public partial class BbsTerminalView
{
    private readonly TriggerLineBuffer _triggerLines = new();
    private int _triggerGeneration;
    private Action<string>? _processTriggerLine;
    private Action<Trigger, string>? _executeTrigger;
    private Action<string>? _reportTriggerError;

    /// <summary>
    /// Processes received text on the dispatcher that owns the profile's observable trigger list.
    /// </summary>
    /// <param name="data">The received terminal text.</param>
    private void ProcessTriggerText(string data)
    {
        Dispatcher.VerifyAccess();
        if (_disposed || !_connection.IsConnected || !Profile.TriggersEnabled || _transferActive || Profile.Triggers.Count == 0)
        {
            _triggerLines.Reset();
            return;
        }

        _triggerLines.Append(data, _processTriggerLine ??= ProcessTriggerLine);
    }

    /// <summary>
    /// Starts scripts for a completed line using this session's shared environment and terminal bridge.
    /// </summary>
    /// <param name="line">The received line without escape sequences or its terminator.</param>
    private void ProcessTriggerLine(string line) => TriggerProcessor.Process(Profile.Triggers, line,
        _executeTrigger ??= (trigger, code) => _ = RunAliasAsync(trigger.Name, code, "Trigger"),
        _reportTriggerError ??= ShowTransientStatus);
}
