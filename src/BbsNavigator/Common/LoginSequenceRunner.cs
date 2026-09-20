/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Models;

namespace BbsNavigator.Common;

/// <summary>
/// Validates and executes explicit login steps, stopping on timeout or cancellation.
/// </summary>
internal static class LoginSequenceRunner
{
    internal static void Validate(IReadOnlyList<LoginStep> steps)
    {
        if (steps.Count == 0) throw new ArgumentException("Add at least one login step.");
        foreach (var step in steps)
        {
            if (!Enum.IsDefined(step.Action)) throw new ArgumentException("Choose a valid login action.");
            if (step.Action == LoginAction.WaitForText && string.IsNullOrWhiteSpace(step.Text))
                throw new ArgumentException("Each WaitForText step needs prompt text.");
            if (step.Action is LoginAction.WaitForText or LoginAction.Delay && step.Seconds is < 1 or > 300)
                throw new ArgumentException("Prompt timeouts and delays must be between 1 and 300 seconds.");
        }
    }

    internal static async Task RunAsync(IReadOnlyList<LoginStep> steps, BbsCredentials? credentials,
        LoginPromptBuffer prompts, Func<string, CancellationToken, Task> send,
        Action<string> status, CancellationToken cancellationToken)
    {
        Validate(steps);
        if (credentials == null && steps.Any(s => s.Action is LoginAction.SendUsername or LoginAction.SendPassword))
            throw new InvalidOperationException("Save credentials before using username or password login steps.");

        for (int index = 0; index < steps.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var step = steps[index];
            status($"Login step {index + 1}/{steps.Count}: {step.Action}");
            if (step.Action == LoginAction.WaitForText)
                await prompts.WaitAsync(step.Text, TimeSpan.FromSeconds(step.Seconds), cancellationToken);
            else if (step.Action == LoginAction.Delay)
                await Task.Delay(TimeSpan.FromSeconds(step.Seconds), cancellationToken);
            else
            {
                string text = step.Action switch
                {
                    LoginAction.SendUsername => credentials!.UserName,
                    LoginAction.SendPassword => credentials!.Password,
                    LoginAction.Enter => "\r",
                    _ => TerminalText.Normalize(step.Text)
                };
                await send(text, cancellationToken);
            }
        }
    }
}
