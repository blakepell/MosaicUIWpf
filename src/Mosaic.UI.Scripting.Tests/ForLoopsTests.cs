using Xunit;
using Mosaic.UI.Scripting.API;

namespace Mosaic.UI.Scripting.Test;

public sealed class ForLoopsTests
{
    [Fact]
    public void ForInLoop()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
let items = [1, 2, 3]
let index = 3
for (const index in items) {
    model[index] = items[index]
}
");
        Assert.Equal(1, model["0"]);
        Assert.Equal(2, model["1"]);
        Assert.Equal(3, model["2"]);
    }

    [Fact]
    public void ForOfLoop()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
let items = [51, 22, 33]
for (const item of items) {
    model[item] = item
}
");
        Assert.Equal(51, model["51"]);
        Assert.Equal(22, model["22"]);
        Assert.Equal(33, model["33"]);
    }

    [Fact]
    public void ForLoopConstVarDef()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
let items = [1, 2, 3]
let index = 3
for (const index in items) {
    const a = 3
    model[index] = items[index]
}
for (const index of items) {
    const a = 3
}
for (var i = 0 ; i < 3; ++i) {
    const a = 3
}
");
        Assert.Equal(1, model["0"]);
        Assert.Equal(2, model["1"]);
        Assert.Equal(3, model["2"]);
    }

    [Theory]
    [InlineData("foreach (let item in items)")]
    [InlineData("foreach (const item in items)")]
    [InlineData("foreach (var item in items)")]
    [InlineData("foreach (const item of items)")]
    [InlineData("foreach (const item = items)")]
    [InlineData("foreach(let item in items)")]
    public void ForEachLoopIteratesValues(string header)
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
let items = [51, 22, 33]
model.sum = 0
" + header + @" {
    model.sum += item
}
");
        Assert.Equal(106, model.sum);
    }

    [Fact]
    public void ForEachLoopOverDotNetCollection()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.SetValue("names", new System.Collections.Generic.List<string> { "a", "b", "c" });
        engine.ExecuteScript(@"
model.text = ''
foreach (let name in names) {
    model.text += name
}
");
        Assert.Equal("abc", model.text);
    }

    [Fact]
    public void ForEachLoopSingleStatementBodyBreakAndContinue()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
let items = [1, 2, 3, 4, 5]
model.count = 0
foreach (const item in items) model.count++
model.sum = 0
foreach (const item in items) {
    if (item == 2) continue
    if (item == 4) break
    model.sum += item
}
");
        Assert.Equal(5, model.count);
        Assert.Equal(4, model.sum);
    }

    [Fact]
    public void ForEachLoopReturnsFromFunction()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
function find(items) {
    foreach (const item in items) {
        if (item > 10) return item
    }
    return -1
}
model.found = find([1, 20, 30])
");
        Assert.Equal(20, model.found);
    }

    [Fact]
    public void ForEachIdentifierStillUsableAsFunction()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
function foreach(x) { model.called = x }
foreach (5)
");
        Assert.Equal(5, model.called);
    }
}
