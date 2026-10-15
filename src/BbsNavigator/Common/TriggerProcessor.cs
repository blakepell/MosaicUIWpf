/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Buffers;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using BbsNavigator.Models;

namespace BbsNavigator.Common;

/// <summary>
/// Matches triggers using a temporary pooled snapshot of a dispatcher-owned observable collection.
/// </summary>
internal static class TriggerProcessor
{
    private static readonly IComparer<Trigger> TriggerPriorityComparer =
        Comparer<Trigger>.Create((left, right) => left.Priority.CompareTo(right.Priority));

    /// <summary>
    /// Starts each matching script in ascending priority order, isolating collection edits during callbacks.
    /// </summary>
    /// <param name="triggers">The collection owned by the calling dispatcher.</param>
    /// <param name="line">The complete received line.</param>
    /// <param name="execute">The callback that starts a script with already expanded source.</param>
    /// <param name="reportError">The callback that reports a disabled expression after a timeout.</param>
    internal static void Process(ObservableCollection<Trigger> triggers, string line,
        Action<Trigger, string> execute, Action<string> reportError)
    {
        // No await occurs between copying and returning this snapshot. All list edits and model
        // updates use this dispatcher; scripts receive expanded source rather than mutable matches.
        int count = triggers.Count;
        if (count == 0) return;
        Trigger[] snapshot = ArrayPool<Trigger>.Shared.Rent(count);
        try
        {
            triggers.CopyTo(snapshot, 0);
            // Stable insertion sort preserves list order among equal priorities.
            for (int i = 1; i < count; i++)
            {
                Trigger current = snapshot[i];
                int j = i - 1;
                while (j >= 0 && TriggerPriorityComparer.Compare(snapshot[j], current) > 0)
                {
                    snapshot[j + 1] = snapshot[j];
                    j--;
                }
                snapshot[j + 1] = current;
            }

            for (int i = 0; i < count; i++)
            {
                Trigger trigger = snapshot[i];
                if (!trigger.Enabled || string.IsNullOrWhiteSpace(trigger.Command)) continue;
                Regex? regex = trigger.GetRegex(out _);
                if (regex == null) continue;

                try
                {
                    Match match = regex.Match(line);
                    if (!match.Success) continue;
                    string code = TriggerInput.Expand(trigger.Command, line, match);
                    if (trigger.DisableAfterTriggered) trigger.Enabled = false;
                    trigger.Count++;
                    execute(trigger, code);
                }
                catch (RegexMatchTimeoutException)
                {
                    // A pathological expression should not stall every subsequent line.
                    trigger.Enabled = false;
                    reportError($"Trigger '{trigger.Name}' disabled: regular expression timed out.");
                }
            }
        }
        finally
        {
            ArrayPool<Trigger>.Shared.Return(snapshot, clearArray: true);
        }
    }
}
