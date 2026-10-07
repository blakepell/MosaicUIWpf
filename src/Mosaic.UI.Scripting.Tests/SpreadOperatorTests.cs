using Xunit;
using System.Collections.Generic;
using Mosaic.UI.Wpf.Scripting.API;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class SpreadOperatorTests
{
    [Fact]
    public void SpreadOperator()
    {
        var engine = new ScriptEngine();
        dynamic model = new List<double>();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
max = (...args) => {
    for (const item of args) {
        model.Add(item)
    }
}
max(-1, 5, 11, 3)
max(...[-1, 5, 11, 3])
");
        Assert.Equal(new List<double>
        {
            -1, 5, 11, 3,-1, 5, 11, 3
        }, model);
    }

    [Fact]
    public void SpreadArray()
    {
        var engine = new ScriptEngine();
        dynamic model = new List<double>();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var arr = [1, ...[2,3], 4]
function max(...args) {
    for (const item of args) {
        model.Add(item)
    }
}
max(...arr)
");
        Assert.Equal(new List<double>
        {
            1,2,3,4
        }, model);
    }

    [Fact]
    public void EnumerableObjectSpread()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        model.a = "aa";
        model.b = "bb";
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var c = [...model]
model.c = c
");
        Assert.Equal("bb", model.c[1].Value);
    }
}