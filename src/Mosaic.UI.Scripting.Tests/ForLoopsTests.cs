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
}