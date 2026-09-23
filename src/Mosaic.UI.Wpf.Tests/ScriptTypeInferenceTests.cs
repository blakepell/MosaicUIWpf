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
using Mosaic.UI.Wpf.Scripting;
using Xunit;

namespace Mosaic.UI.Wpf.Tests;

public class ScriptTypeInferenceTests
{
    [Theory]
    [InlineData("var sb = new StringBuilder();\nsb.|", typeof(StringBuilder))]
    [InlineData("let sb = new StringBuilder()\nsb.Append('x').|", typeof(StringBuilder))]
    [InlineData("new StringBuilder().|", typeof(StringBuilder))]
    [InlineData("let p = panels.CreateTool('who', \"Who's Online\");\np.|", typeof(Panel))]
    [InlineData("let p = panels.CreateTool('who', 'Who', 'left');\np.|", typeof(Panel))]
    [InlineData("panels.Get('who')?.|", typeof(Panel))]
    [InlineData("let t = panels.Get('who').Title;\nt.|", typeof(string))]
    [InlineData("const b = new System.Text.StringBuilder();\nb.|", typeof(StringBuilder))]
    [InlineData("let a = panels.Get('a'), b = panels.Items;\nb.|", typeof(List<Panel>))]
    [InlineData("for (const item of panels.Items) {\n    item.|", typeof(Panel))]
    [InlineData("let first = panels.Items[0];\nfirst.|", typeof(Panel))]
    [InlineData("let p = await panels.LoadAsync();\np.|", typeof(Panel))]
    [InlineData("let p = (await panels.LoadAsync());\np.|", typeof(Panel))]
    [InlineData("let p = panels.Untyped();\np.|", typeof(Panel))]
    [InlineData("let p;\np = panels.Get('x');\np.|", typeof(Panel))]
    [InlineData("let s = 'text';\ns.|", typeof(string))]
    [InlineData("let n = panels.Count;\nn.|", typeof(int))]
    [InlineData("let p = panels.CreateTool('who', 'Who is Online');\np.SetText('Users online: 3');\np.| // comment", typeof(Panel))]
    [InlineData("let p = panels.CreateTool('who', 'Who's Online');\np.SetText('Users online: 3');\np.| // comment", typeof(Panel))]
    [InlineData("let p = panels.CreateTool('who', 'Who's Online');\nlet sb = new StringBuilder();\nsb.|", typeof(StringBuilder))]
    [InlineData("app.Run(() => {\n    let sb = new StringBuilder();\n    sb.|", typeof(StringBuilder))]
    [InlineData("let sb = new StringBuilder(); // a comment.\nsb.|", typeof(StringBuilder))]
    public void VariablesResolveToTheirInferredType(string source, Type expected)
    {
        var target = Resolve(source);
        Assert.Equal(expected, target?.Type);
        Assert.False(target?.IsStatic);
    }

    [Theory]
    [InlineData("{ let sb = new StringBuilder(); }\nsb.|")]
    [InlineData("let panels = 5;\npanels.|")]
    [InlineData("function f(panels) {\n    panels.|")]
    [InlineData("items.forEach((x, panels) => {\n    panels.|")]
    [InlineData("try { } catch (panels) {\n    panels.|")]
    [InlineData("let sb = new StringBuilder() + 'x';\nsb.|")]
    [InlineData("let sb = new StringBuilder();\n// sb.|")]
    [InlineData("let sb = new StringBuilder();\nlet s = 'sb.|")]
    [InlineData("let v = panels.Nothing();\nv.|")]
    [InlineData("unknown.|")]
    public void UnknownOrShadowedValuesResolveToNothing(string source)
    {
        Assert.Null(Resolve(source));
    }

    [Fact]
    public void RegisteredAliasesKeepTheirStaticAndInstanceMembers()
    {
        var environment = CreateEnvironment();
        var type = ScriptTypeInference.ResolveTarget(environment, "StringBuilder.", "StringBuilder".Length);
        Assert.True(type?.IsStatic);
        var module = ScriptTypeInference.ResolveTarget(environment, "panels.", "panels".Length);
        Assert.Same(environment.Registrations["panels"], module?.Registration);

        var members = ScriptCompletion.GetMembers(Resolve("let sb = new StringBuilder();\nsb.|")!.Value);
        Assert.Contains(members, m => m.Text == "Append");
        Assert.Contains(members, m => m.Text == "Length");
        Assert.DoesNotContain(members, m => m.Text == "ReferenceEquals");
        Assert.Contains(ScriptCompletion.GetMembers(Resolve("let p = panels.CreateTool('a', 'b');\np.|")!.Value), m => m.Text == "Title");
    }

    [Fact]
    public void SignaturesResolveOnInferredVariables()
    {
        var environment = CreateEnvironment();
        var target = Resolve("let sb = new StringBuilder();\nsb.|");
        var signatures = ScriptCompletion.GetSignatures(environment, new ScriptCallContext(0, 0, "sb", "Insert", false), target);
        Assert.NotEmpty(signatures);
        Assert.All(signatures, s => Assert.Equal("StringBuilder", s.ReturnType));
    }

    [Fact]
    public void VariablesInScopeAreListed()
    {
        const string source = "let sb = new StringBuilder();\n{ let inner = 1; }\nfunction f(a) {\n    let p = panels.Get('x');\n";
        var variables = ScriptTypeInference.GetVariables(CreateEnvironment(), source, source.Length);
        Assert.Equal(typeof(StringBuilder), variables["sb"]?.Type);
        Assert.Equal(typeof(Panel), variables["p"]?.Type);
        Assert.True(variables.ContainsKey("a"));
        Assert.Null(variables["a"]);
        Assert.False(variables.ContainsKey("inner"));
    }

    private static ScriptValueType? Resolve(string source)
    {
        int caret = source.IndexOf('|');
        string text = source.Remove(caret, 1);
        // The caret follows the dot (or ?.); the target ends at the dot.
        return ScriptTypeInference.ResolveTarget(CreateEnvironment(), text, caret - 1);
    }

    private static ScriptEnvironment CreateEnvironment()
    {
        var environment = new ScriptEnvironment();
        environment.RegisterObject("panels", new PanelCommands());
        return environment;
    }

    public class Panel
    {
        public string Title { get; set; } = "";
    }

    [ScriptModule(Name = "panels")]
    public class PanelCommands
    {
        public int Count => 0;
        public List<Panel> Items { get; } = [];
        public Panel CreateTool(string id, string title) => new();
        public Panel CreateTool(string id, string title, string side) => new();
        public Panel? Get(string id) => null;
        public Task<Panel> LoadAsync() => Task.FromResult(new Panel());
        [ScriptModuleMethod(ReturnType = typeof(Panel))]
        public object Untyped() => new Panel();
        public void Nothing() { }
    }
}
