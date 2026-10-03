using Xunit;
using Mosaic.UI.Scripting.API;

namespace Mosaic.UI.Scripting.Test;

public sealed class WhileLoopTests
{
    [Fact]
    public void WhileLoop()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
let i = 0
while (i < 10) {
    ++i;
    const i = 4;
}
var j = 0
while (j < 10) {
    ++j;
if (j < 6) continue;
else if (j == 6) break;
    j += 10;
    const i = 4;
}
model.i = i
model.j = j
");
        Assert.Equal(10, model.i);
        Assert.Equal(6, model.j);
    }

    [Fact]
    public void DoWhileLoop()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
let i = 0
do {
    ++i;
    const i = 4;
}
while (i < 10)
var j = 0
do {
    ++j;
if (j < 6) continue;
else if (j == 6) break;
    j += 10;
    const i = 4;
} while (j < 10)
model.i = i
model.j = j
");
        Assert.Equal(10, model.i);
        Assert.Equal(6, model.j);
    }
}