/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Text;

namespace BbsNavigator.Common;

/// <summary>
/// Matches prompts across network chunks, excluding streamed ANSI control sequences.
/// </summary>
internal sealed class LoginPromptBuffer
{
    private readonly object _gate = new();
    private readonly StringBuilder _text = new();
    private TaskCompletionSource _changed = NewSignal();
    private int _escapeState;

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal void Reset()
    {
        lock (_gate)
        {
            _text.Clear();
            _escapeState = 0;
        }
    }

    // A manual login can match the current prompt, but not previous screens in the session.
    internal void KeepCurrentLine()
    {
        lock (_gate)
        {
            int newline = _text.ToString().LastIndexOf('\n');
            if (newline >= 0) _text.Remove(0, newline + 1);
        }
    }

    internal void Append(string data)
    {
        lock (_gate)
        {
            foreach (char ch in data)
            {
                switch (_escapeState)
                {
                    case 1:
                        _escapeState = ch == '[' ? 2 : ch is ']' or 'P' or '^' or '_' ? 3 : 0;
                        break;
                    case 2:
                        if (ch is >= '@' and <= '~') _escapeState = 0;
                        break;
                    case 3:
                        if (ch == '\a') _escapeState = 0;
                        else if (ch == '\x1b') _escapeState = 4;
                        break;
                    case 4:
                        _escapeState = ch == '\\' ? 0 : 3;
                        break;
                    default:
                        if (ch == '\x1b') _escapeState = 1;
                        else if (ch == '\b' && _text.Length > 0) _text.Length--;
                        else if (!char.IsControl(ch) || ch is '\n' or '\t') _text.Append(ch);
                        break;
                }
            }

            if (_text.Length > 16384) _text.Remove(0, _text.Length - 16384);
            var changed = _changed;
            _changed = NewSignal();
            changed.TrySetResult();
        }
    }

    internal async Task WaitAsync(string prompt, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Task changed;
                lock (_gate)
                {
                    int index = _text.ToString().IndexOf(prompt, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        _text.Remove(0, index + prompt.Length);
                        return;
                    }
                    changed = _changed.Task;
                }
                await changed.WaitAsync(deadline.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("The expected login prompt did not arrive. Login stopped before sending the next step.");
        }
    }
}
