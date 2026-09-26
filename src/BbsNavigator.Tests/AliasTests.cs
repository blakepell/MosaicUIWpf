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
using System.Text.Json;
using Xunit;

namespace BbsNavigator.Tests;

public class AliasTests
{
    [Fact]
    public void ParseSplitsNameAndKeepsQuotedArgumentsTogether()
    {
        Assert.True(AliasInput.TryParse("  tell  \"Joe Smith\" hello there ", out string name, out var arguments, out string remainder));
        Assert.Equal("tell", name);
        Assert.Equal(new[] { "Joe Smith", "hello", "there" }, arguments);
        Assert.Equal("\"Joe Smith\" hello there", remainder);
    }

    [Fact]
    public void ParseRejectsBlankInputAndAllowsNoArguments()
    {
        Assert.False(AliasInput.TryParse("   ", out _, out _, out _));
        Assert.True(AliasInput.TryParse("look", out string name, out var arguments, out string remainder));
        Assert.Equal("look", name);
        Assert.Empty(arguments);
        Assert.Equal(string.Empty, remainder);
    }

    [Fact]
    public void SplitKeepsEmptyQuotedArgumentAndRunsUnterminatedQuoteToEnd()
    {
        Assert.Equal(new[] { "a", "", "b c" }, AliasInput.Split("a \"\" \"b c"));
    }

    [Fact]
    public void ExpandReplacesPlaceholdersAndBlanksMissingArguments()
    {
        string script = "let a = \"%1\"; let b = \"%2\"; let c = \"%3\"; let all = \"%0\"; let pct = 10 %% 3;";
        string expanded = AliasInput.Expand(script, new[] { "one", "two words" }, "one \"two words\"");
        Assert.Equal("let a = \"one\"; let b = \"two words\"; let c = \"\"; let all = \"one \\\"two words\\\"\"; let pct = 10 % 3;", expanded);
    }

    [Fact]
    public void ExpandEscapesValuesForStringLiterals()
    {
        string expanded = AliasInput.Expand("\"%1\"", new[] { "it's a \\ \"test\"`" }, string.Empty);
        Assert.Equal("\"it\\'s a \\\\ \\\"test\\\"\\`\"", expanded);
    }

    [Fact]
    public void AliasesRoundTripOnProfileJson()
    {
        var profile = new BbsProfile { Name = "Test" };
        profile.Aliases.Add(new Alias { AliasExpression = "gg", Command = "term.SendLine(\"get gold\");", Group = "loot", SortOrder = 2, Enabled = false, Count = 5 });

        var copy = JsonSerializer.Deserialize<BbsProfile>(JsonSerializer.Serialize(profile))!;

        Alias alias = Assert.Single(copy.Aliases);
        Assert.Equal(profile.Aliases[0].Id, alias.Id);
        Assert.Equal("gg", alias.AliasExpression);
        Assert.Equal("term.SendLine(\"get gold\");", alias.Command);
        Assert.Equal("loot", alias.Group);
        Assert.Equal(2, alias.SortOrder);
        Assert.False(alias.Enabled);
        Assert.Equal(5, alias.Count);
    }

    [Fact]
    public void CloneIsIndependentAndCopyFromKeepsId()
    {
        var original = new Alias { AliasExpression = "a", Command = "x" };
        var clone = (Alias)original.Clone();
        Assert.Equal(original.Id, clone.Id);

        clone.Command = "y";
        Assert.Equal("x", original.Command);

        var other = new Alias { AliasExpression = "b", Command = "z" };
        original.CopyFrom(other);
        Assert.Equal("z", original.Command);
        Assert.NotEqual(other.Id, original.Id);
    }
}
