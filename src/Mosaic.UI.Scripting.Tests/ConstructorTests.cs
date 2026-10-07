using Xunit;
using System;
using System.Collections.Generic;
using Mosaic.UI.Wpf.Scripting.API;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class ConstructorTests
{
    [Fact]
    public void DateTimeConstruction()
    {
        var engine = new ScriptEngine();
        engine.AddType<DateTime>("DateTime");
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
model.a = new DateTime(2021, 7, 21, 5, 5, 5, 'uTC')
model.a = model.a.AddTicks(5555);
model.b = DateTime.Parse('2021-11-11').ToString('dd/MM/yyyy HH:mm:ss')
model.c = (DateTime.Now.GetType()).ToString()
");
        Assert.Equal(
            new DateTime(2021, 7, 21, 5, 5, 5, DateTimeKind.Utc)
            .AddTicks(5555), model.a);
        Assert.Equal(
            "11/11/2021 00:00:00", model.b);
        Assert.Equal(
            "System.DateTime", model.c);
    }

    [Fact]
    public void GenericTypeConstruction()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        model["int"] = typeof(int);
        model["string"] = typeof(string);
        engine.SetValue("model", model);
        engine.AddType(typeof(Dictionary<,>), "GenericDictionary");

        engine.ExecuteScript(@"
var dic = model.dic = new GenericDictionary(model.string, model.int)
dic.Add('hello', 1)
dic.Add('dummy', 0)
dic.Add('world', 2)
dic.Remove('dummy')
");
        Assert.Equal(1, model.dic["hello"]);
        Assert.Equal(2, model.dic["world"]);
        Assert.False(model.dic.ContainsKey("dummy"));
    }
}