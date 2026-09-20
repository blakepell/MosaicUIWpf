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
using BbsNavigator.Models;
using System.IO;
using System.Text;
using Xunit;

namespace BbsNavigator.Tests;

public class BbsNavigatorWorkflowTests
{
    [Fact]
    public async Task LoginWaitsForSplitColoredPromptsBeforeSendingCredentials()
    {
        var prompts = new LoginPromptBuffer();
        var sent = new List<string>();
        var steps = new List<LoginStep>
        {
            new() { Action = LoginAction.WaitForText, Text = "name:" },
            new() { Action = LoginAction.SendUsername },
            new() { Action = LoginAction.Enter },
            new() { Action = LoginAction.WaitForText, Text = "password:" },
            new() { Action = LoginAction.SendPassword }
        };
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var usernameSent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task login = LoginSequenceRunner.RunAsync(steps, new("test-user", "secret"), prompts, (text, _) =>
        {
            sent.Add(text);
            if (text == "\r") usernameSent.SetResult();
            return Task.CompletedTask;
        }, _ => { }, deadline.Token);
        Assert.Empty(sent);
        prompts.Append("\x1b[3");
        prompts.Append("1mNa");
        prompts.Append("me:\x1b[0m");
        await usernameSent.Task.WaitAsync(deadline.Token);
        Assert.DoesNotContain("secret", sent);
        prompts.Append("Pass");
        prompts.Append("word:");
        await login;
        Assert.Equal(new[] { "test-user", "\r", "secret" }, sent);
    }

    [Fact]
    public async Task LoginCancellationPreventsFollowingPasswordStep()
    {
        using var cts = new CancellationTokenSource();
        var sent = new List<string>();
        Task login = LoginSequenceRunner.RunAsync(new List<LoginStep>
        {
            new() { Action = LoginAction.WaitForText, Text = "Password:" },
            new() { Action = LoginAction.SendPassword }
        }, new("user", "secret"), new(), (s, _) => { sent.Add(s); return Task.CompletedTask; }, _ => { }, cts.Token);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => login);
        Assert.Empty(sent);
    }

    [Fact]
    public async Task MissingPromptTimesOutAndConsumedPromptCannotMatchTwice()
    {
        var prompts = new LoginPromptBuffer();
        prompts.Append("Name:");
        await prompts.WaitAsync("name:", TimeSpan.FromSeconds(1), CancellationToken.None);
        await Assert.ThrowsAsync<TimeoutException>(() => prompts.WaitAsync("name:", TimeSpan.FromMilliseconds(30), CancellationToken.None));
    }

    [Fact]
    public async Task PaceCancellationStopsWithoutSplittingUnicodeOrSendingRemainingText()
    {
        using var cts = new CancellationTokenSource();
        var sent = new List<string>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TerminalText.SendAsync("😀\r\nsecond", 0, 0, (text, _) =>
        {
            sent.Add(text);
            if (text.Contains('\r')) cts.Cancel();
            return Task.CompletedTask;
        }, (_, _) => { }, cts.Token));
        Assert.Equal("😀\r", string.Concat(sent));
    }

    [Fact]
    public async Task ExtraLineDelayCanBeCanceledBeforeTheNextLine()
    {
        using var cts = new CancellationTokenSource();
        var sent = new List<string>();
        var newlineSent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task sending = TerminalText.SendAsync("a\nb", 0, 10000, (text, _) =>
        {
            sent.Add(text);
            if (text.Contains('\r')) newlineSent.SetResult();
            return Task.CompletedTask;
        }, (_, _) => { }, cts.Token);
        await newlineSent.Task;
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sending);
        Assert.Equal("a\r", string.Concat(sent));
    }

    [Fact]
    public void ComposerWrappingPreservesBlankLinesAndUnicode()
    {
        string wrapped = TerminalText.Wrap("one two three\n\n😀😀😀", 7);
        Assert.Equal(new[] { "one two", "three", "", "😀😀😀" }, TerminalText.Normalize(wrapped).Split('\r'));
        Assert.Equal("unmodified\ntext", TerminalText.Wrap("unmodified\ntext", 0));
    }

    [Fact]
    public void EncodingValidationDetectsLossInsteadOfSilentlyReplacingCharacters()
    {
        Assert.NotEmpty(TerminalText.EncodingProblem("hello 😀", Encoding.Latin1));
        Assert.Empty(TerminalText.EncodingProblem("hello 😀", Encoding.UTF8));
    }

    [Fact]
    public void DraftSurvivesReopeningAndIsIsolatedPerBoard()
    {
        string folder = Path.Combine(Path.GetTempPath(), "bbs-draft-test-" + Guid.NewGuid().ToString("N"));
        Guid board = Guid.NewGuid();
        try
        {
            new MessageDraftStore(folder, board).Save("first draft");
            new MessageDraftStore(folder, board).Save("updated draft\nsecond line");
            Assert.Equal("updated draft\nsecond line", new MessageDraftStore(folder, board).Load());
            Assert.Empty(new MessageDraftStore(folder, Guid.NewGuid()).Load());
            Assert.Empty(Directory.GetFiles(folder, "*.tmp", SearchOption.AllDirectories));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public async Task InvalidLaterStepIsRejectedBeforeAnyLoginTextIsSent()
    {
        bool sent = false;
        await Assert.ThrowsAsync<ArgumentException>(() => LoginSequenceRunner.RunAsync(new List<LoginStep>
        {
            new() { Action = LoginAction.SendText, Text = "hello" },
            new() { Action = LoginAction.WaitForText, Text = "" }
        }, null, new(), (_, _) => { sent = true; return Task.CompletedTask; }, _ => { }, CancellationToken.None));
        Assert.False(sent);
    }

    [Fact]
    public async Task LoginTimeoutDoesNotSendPassword()
    {
        bool sent = false;
        await Assert.ThrowsAsync<TimeoutException>(() => LoginSequenceRunner.RunAsync(new List<LoginStep>
        {
            new() { Action = LoginAction.WaitForText, Text = "Password:", Seconds = 1 },
            new() { Action = LoginAction.SendPassword }
        }, new("user", "secret"), new(), (_, _) => { sent = true; return Task.CompletedTask; }, _ => { }, CancellationToken.None));
        Assert.False(sent);
    }
}
