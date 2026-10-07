using Xunit;
using System.Collections.Generic;
using Mosaic.UI.Wpf.Scripting.API;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class GenericListTests
{
    [Fact]
    public void TestList1()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        var list = new List<int>()
        {
            1,2,3,4,5
        };
        engine.SetValue("list", list);
        engine.ExecuteScript(@"
model.a1 = list[0]
model.a2 = list['0']
list[1] = 8
model.a3 = list[1]
list.Add(9)
model.a4 = list[5]
model.a5 = list.Count
");
        Assert.Equal(1, model.a1);
        Assert.Equal(1, model.a2);
        Assert.Equal(8, model.a3);
        Assert.Equal(9, model.a4);
        Assert.Equal(6, model.a5);
    }
}