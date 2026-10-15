/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using BbsNavigator.Common;
using BbsNavigator.Models;
using Xunit;

namespace BbsNavigator.Tests;

/// <summary>
/// Verifies trigger captures, line framing, cached patterns, persistence, and collection mutation.
/// </summary>
public class TriggerTests
{
    /// <summary>
    /// Verifies the army pattern supports named and numbered captures in the same script.
    /// </summary>
    [Fact]
    public void ExpansionSupportsNamedArmyCapturesAlongsideNumberedGroups()
    {
        const string pattern = @"(?<army>\w+)\s+Army\s+(?<units>\d+)\s+(?<x>\d+)\s*,\s*(?<y>\d+)\s+(?<sector>\d+)";
        const string line = "Northern Army 150 12, 34 7";
        Match match = Regex.Match(line, pattern);
        string expanded = TriggerInput.Expand(
            "term.Echo(\"%army: %units units at %x,%y in sector %sector | %1 | %0\");",
            line, match);
        Assert.Equal(
            "term.Echo(\"Northern: 150 units at 12,34 in sector 7 | Northern | Northern Army 150 12, 34 7\");",
            expanded);
    }

    /// <summary>
    /// Verifies complete name boundaries, case sensitivity, missing captures, and percent escaping.
    /// </summary>
    [Fact]
    public void NamedExpansionUsesWholeNamesAndDoesNotExpandInsertedPlaceholders()
    {
        const string line = "%units/second/third";
        Match match = Regex.Match(line, @"(?<army>[^/]+)/(?<army_name>[^/]+)/(?<army2>[^/]+)(?<optional>!)?");
        Assert.Equal("%units|second|third||||%army|%",
            TriggerInput.Expand("%army|%army_name|%army2|%Army|%missing|%optional|%%army|%%", line, match));
    }

    /// <summary>
    /// Verifies named captures use the same escaping as numbered captures in executed scripts.
    /// </summary>
    [Fact]
    public async Task NamedCapturesRemainLiteralWhenScriptsExecute()
    {
        var environment = new Mosaic.UI.Wpf.Controls.Scripting.ScriptEnvironment();
        var values = new List<string>();
        environment.RegisterObject("probe", values);
        const string line = "'\"\\\t`"+ "$" + "{1 + 1}%army";
        Match match = Regex.Match(line, "(?<army>.*)");
        await environment.ExecuteAsync(TriggerInput.Expand(
            "probe.Add(\"%army\"); probe.Add('%army');", line, match));
        Assert.Equal(new[] { line, line }, values);
    }

    /// <summary>
    /// Checks multi-digit placeholders, entire-line semantics, and non-recursive expansion.
    /// </summary>
    [Fact]
    public void ExpansionUsesWholeLineAndMultiDigitGroupsWithoutRecursiveReplacement()
    {
        const string line = "prefix abcdefghij suffix";
        Match match = Regex.Match(line, "(a)(b)(c)(d)(e)(f)(g)(h)(i)(j)");
        Assert.Equal("prefix abcdefghij suffix|a|j||%1|%", TriggerInput.Expand("%0|%1|%10|%99|%%1|%%", line, match));
        Match percent = Regex.Match("%2", "(.*)");
        Assert.Equal("%2", TriggerInput.Expand("%1", "%2", percent));
    }

    /// <summary>
    /// Checks that received text remains literal data inside JavaScript strings and templates.
    /// </summary>
    [Fact]
    public void ExpansionEscapesQuotesControlsAndTemplateInterpolation()
    {
        const string value = "'\"\\\n\t\u2028`"+ "$" + "{term.SendLine('bad')}";
        string expanded = TriggerInput.Expand("\"%0\"", value, Regex.Match(value, ".*"));
        Assert.Equal(value, JsonSerializer.Deserialize<string>(expanded));
        Assert.DoesNotContain("$" + "{", expanded);
        Assert.DoesNotContain("`", expanded);
    }

    /// <summary>
    /// Verifies escaped captures execute as literal strings in the actual script environment.
    /// </summary>
    [Fact]
    public async Task ExpandedCapturesRemainLiteralWhenScriptsExecute()
    {
        var environment = new Mosaic.UI.Wpf.Controls.Scripting.ScriptEnvironment();
        var values = new List<string>();
        environment.RegisterObject("probe", values);
        const string line = "'\"\\\t`"+ "$" + "{1 + 1}%1";
        Match match = Regex.Match(line, "(.*)");
        await environment.ExecuteAsync(TriggerInput.Expand("probe.Add(\"%0\"); probe.Add('%1');", line, match));
        Assert.Equal(new[] { line, line }, values);
    }

    /// <summary>
    /// Checks streaming escape sequences and line endings across chunk boundaries.
    /// </summary>
    [Fact]
    public void BufferWaitsForCompleteLinesAndStripsSplitAnsiSequences()
    {
        var buffer = new TriggerLineBuffer();
        var lines = new List<string>();
        buffer.Append("\x1b[3", lines.Add);
        buffer.Append("1mhel", lines.Add);
        Assert.Empty(lines);
        buffer.Append("lo\x1b[0m\r", lines.Add);
        buffer.Append("\nworld\n\r\x1b]title", lines.Add);
        buffer.Append("\x1b\\last\b!\n", lines.Add);
        Assert.Equal(new[] { "hello", "world", "las!" }, lines);
    }

    /// <summary>
    /// Checks that oversized lines cannot fire a trigger using a truncated suffix.
    /// </summary>
    [Fact]
    public void BufferDiscardsOversizedLinesAndResetDropsPartialInput()
    {
        var buffer = new TriggerLineBuffer();
        var lines = new List<string>();
        buffer.Append(new string('x', 65537) + "match\nnext\npartial", lines.Add);
        buffer.Reset();
        buffer.Append("fresh\n", lines.Add);
        Assert.Equal(new[] { "next", "fresh" }, lines);
    }

    /// <summary>
    /// Checks per-instance caching and recovery after invalid pattern edits.
    /// </summary>
    [Fact]
    public void RegexIsReusedUntilPatternChanges()
    {
        var trigger = new Trigger { Pattern = "one" };
        var regex = trigger.GetRegex(out var error);
        Assert.Null(error);
        Assert.Same(regex, trigger.GetRegex(out _));
        Assert.Equal(TimeSpan.FromMilliseconds(250), regex!.MatchTimeout);
        trigger.Pattern = "[";
        Assert.Null(trigger.GetRegex(out error));
        Assert.NotNull(error);
        trigger.Pattern = "two";
        Assert.NotSame(regex, trigger.GetRegex(out error));
        Assert.Null(error);
    }

    /// <summary>
    /// Checks snapshot lifetime, stable priority order, one-shot behavior, and disabled triggers.
    /// </summary>
    [Fact]
    public void SnapshotSurvivesDeletingAndAddingTriggersDuringExecution()
    {
        var later = new Trigger { Name = "later", Pattern = "line", Command = "%0", Priority = 2 };
        var first = new Trigger { Name = "first", Pattern = "(line)", Command = "%1", Priority = 1, DisableAfterTriggered = true };
        var equal = new Trigger { Name = "equal", Pattern = "line", Command = "%0", Priority = 1 };
        var disabled = new Trigger { Pattern = "line", Command = "%0", Enabled = false };
        ObservableCollection<Trigger> triggers = [later, first, equal, disabled];
        var calls = new List<string>();
        TriggerProcessor.Process(triggers, "line", (trigger, code) =>
        {
            calls.Add(trigger.Name);
            Assert.Equal("line", code);
            triggers.Clear();
            triggers.Add(new Trigger { Name = "new", Pattern = "line", Command = "unexpected" });
        }, error => Assert.Fail(error));
        Assert.Equal(new[] { "first", "equal", "later" }, calls);
        Assert.False(first.Enabled);
        Assert.Equal(1, first.Count);
        Assert.Equal(0, disabled.Count);
    }

    /// <summary>
    /// Verifies a timed-out regex is disabled while subsequent triggers still run.
    /// </summary>
    [Fact]
    public void TimeoutDisablesOnlyTheFailingTrigger()
    {
        var slow = new Trigger { Name = "slow", Pattern = "^(a+)+$", Command = "unexpected", Priority = 0 };
        var next = new Trigger { Name = "next", Pattern = "!$", Command = "expected", Priority = 1 };
        ObservableCollection<Trigger> triggers = [slow, next];
        var errors = new List<string>();
        var calls = new List<string>();
        TriggerProcessor.Process(triggers, new string('a', 10000) + "!",
            (trigger, _) => calls.Add(trigger.Name), errors.Add);
        Assert.False(slow.Enabled);
        Assert.Equal(0, slow.Count);
        Assert.Single(errors);
        Assert.Equal(new[] { "next" }, calls);
    }

    /// <summary>
    /// Checks trigger persistence and default compatibility with older profiles.
    /// </summary>
    [Fact]
    public void ProfileJsonRoundTripsTriggersWithoutRegexOrMatchState()
    {
        var profile = new BbsProfile { TriggersEnabled = false };
        var trigger = new Trigger { Name = "test", Pattern = "(hello)", Command = "term.SendLine(\"%1\");", Priority = 7, DisableAfterTriggered = true };
        trigger.GetRegex(out _);
        profile.Triggers.Add(trigger);
        string json = JsonSerializer.Serialize(profile);
        var copy = JsonSerializer.Deserialize<BbsProfile>(json)!;
        Assert.False(copy.TriggersEnabled);
        var restored = Assert.Single(copy.Triggers);
        Assert.Equal(trigger.Id, restored.Id);
        Assert.Equal(trigger.Command, restored.Command);
        Assert.Equal(7, restored.Priority);
        Assert.True(restored.DisableAfterTriggered);
        Assert.Matches(restored.GetRegex(out _)!, "hello");
        var old = JsonSerializer.Deserialize<BbsProfile>("{}")!;
        Assert.True(old.TriggersEnabled);
        Assert.Empty(old.Triggers);
    }
}
