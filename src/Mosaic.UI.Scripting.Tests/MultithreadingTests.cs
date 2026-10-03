using Xunit;
using System;
using System.Threading.Tasks;
using Mosaic.UI.Scripting.API;

namespace Mosaic.UI.Scripting.Test;

public sealed class MultithreadingTests
{
    [Fact]
    public void TestParallelLoop()
    {
        var engine = new ScriptEngine();
        dynamic model = new ConcurrentJsObject();
        engine.AddType(typeof(Parallel), "Parallel");
        engine.AddType(typeof(Action), "Action");
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var g = 0;
function f1(i) {
++g;
}
new Action(f1)
Parallel.For(0, 10000, f1)
var q = 0
Parallel.For(0, 10000, (x) => q = x)
Parallel.For(0, 10000, (x) => q = x)
model.g = g
model.q = q
");
        Assert.True(100 < model.g);
        Assert.True(100 < model.q);
    }
}