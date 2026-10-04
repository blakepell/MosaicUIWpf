/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Xunit;
using System;
using System.Collections.Generic;
using System.Text;
using Mosaic.UI.Scripting.API;
using Mosaic.UI.Scripting.ErrorHandling;
using Mosaic.UI.Scripting.Interop;
using Mosaic.UI.Scripting.Options;

namespace Mosaic.UI.Scripting.Test;

public sealed class ImportsTests
{
    [Fact]
    public void ShortNameResolvesThroughImport()
    {
        var engine = new ScriptEngine();
        engine.Imports("System.Text");
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var sb = new StringBuilder()
sb.Append('hello')
model.a = sb.ToString()
model.b = sb
");
        Assert.Equal("hello", model.a);
        Assert.IsType<StringBuilder>(model.b);
    }

    [Fact]
    public void LongNameStillWorksAlongsideImport()
    {
        var engine = new ScriptEngine();
        engine.AddNamespace("System", null, true);
        engine.Imports("System.Text");
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
model.a = new System.Text.StringBuilder('long').ToString()
model.b = new StringBuilder('short').ToString()
");
        Assert.Equal("long", model.a);
        Assert.Equal("short", model.b);
    }

    [Fact]
    public void WithoutImportNameIsNotDefined()
    {
        var engine = new ScriptEngine();
        engine.Options.AllowUndefinedReferenceAccess = false;
        Assert.Throws<TopazException>(() => engine.ExecuteScript("var sb = new StringBuilder()"));
    }

    [Fact]
    public void GenericTypesTakeTypeArguments()
    {
        var engine = new ScriptEngine();
        engine.Imports("System", "System.Collections.Generic");
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var dic = new Dictionary(String, Int32)
dic.Add('hello', 1)
dic.Add('world', 2)
var list = new List(Int32)
list.Add(5)
model.a = dic['hello'] + dic['world']
model.b = list
");
        Assert.Equal(3, model.a);
        Assert.IsType<List<int>>(model.b);
    }

    [Fact]
    public void HostProvidedTypeAliasesWorkAsGenericArguments()
    {
        var engine = new ScriptEngine();
        engine.Imports("System.Collections.Generic");
        engine.AddType<string>("string");
        engine.AddType<int>("int");
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var dic = new Dictionary(string, int)
dic.Add('a', 7)
model.a = dic
");
        Assert.IsType<Dictionary<string, int>>(model.a);
        Assert.Equal(7, model.a["a"]);
    }

    [Fact]
    public void ArityIsChosenFromTypeArgumentCount()
    {
        var engine = new ScriptEngine();
        engine.Imports("System");
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
model.a = new Tuple(String, Int32, 'x', 1)
model.b = new Tuple(Int32, 9)
model.c = Tuple.Create('static', 2)
");
        Assert.IsType<Tuple<string, int>>(model.a);
        Assert.IsType<Tuple<int>>(model.b);
        // Static members go to the non-generic System.Tuple.
        Assert.Equal("static", ((System.Runtime.CompilerServices.ITuple)model.c)[0]);
    }

    [Fact]
    public void FirstImportWinsOnAmbiguity()
    {
        var engine = new ScriptEngine();
        engine.Imports("System.Timers", "System.Threading");
        Assert.True(engine.TryResolveImport("Timer", out var proxy));
        Assert.Equal(typeof(System.Timers.Timer), proxy.ProxiedType);

        engine.ClearImports();
        engine.Imports("System.Threading", "System.Timers");
        Assert.True(engine.TryResolveImport("Timer", out proxy));
        Assert.Equal(typeof(System.Threading.Timer), proxy.ProxiedType);
    }

    [Fact]
    public void ScriptVariablesShadowImports()
    {
        var engine = new ScriptEngine();
        engine.Imports("System.Text");
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
model.a = new StringBuilder('x').ToString()
{
    let StringBuilder = 'shadowed'
    model.b = StringBuilder
}
model.c = new StringBuilder('y').ToString()
");
        Assert.Equal("x", model.a);
        Assert.Equal("shadowed", model.b);
        Assert.Equal("y", model.c);
    }

    [Fact]
    public void ImportedNameCanBeDeclaredAfterUse()
    {
        // Imports are not defined as globals, so a later global declaration does not collide.
        var engine = new ScriptEngine();
        engine.Imports("System.Text");
        engine.ExecuteScript("var a = new StringBuilder()");
        engine.ExecuteScript("let StringBuilder = 5");
        Assert.Equal(5, engine.GetValue("StringBuilder"));
    }

    [Fact]
    public void WhitelistRestrictsImport()
    {
        var engine = new ScriptEngine();
        engine.Imports("System.Collections.Generic", new HashSet<string>
        {
            "System.Collections.Generic.List"
        });
        Assert.True(engine.TryResolveImport("List", out _));
        Assert.False(engine.TryResolveImport("Dictionary", out _));
    }

    [Fact]
    public void ReflectionImportsRequireSecurityPolicy()
    {
        var engine = new ScriptEngine();
        engine.Imports("System.Reflection");
        Assert.False(engine.TryResolveImport("Assembly", out _));

        engine.Options.SecurityPolicy = SecurityPolicy.EnableReflection;
        Assert.True(engine.TryResolveImport("Assembly", out _));
    }

    [Fact]
    public void ImportedNamespacesAreReportedInOrder()
    {
        var engine = new ScriptEngine();
        engine.Imports("System.Text", "System.IO");
        engine.Imports("System.Text");
        Assert.Equal(new[] { "System.Text", "System.IO" }, engine.ImportedNamespaces);
    }

    [Fact]
    public void TypeGroupProxyForwardsStaticMembersToNonGenericType()
    {
        var engine = new ScriptEngine();
        engine.Imports("System");
        Assert.True(engine.TryResolveImport("Action", out var proxy));
        var group = Assert.IsType<TypeGroupProxy>(proxy);
        Assert.Equal(typeof(Action), group.ProxiedType);
        Assert.Contains(typeof(Action<,>), group.Types);
    }
}
