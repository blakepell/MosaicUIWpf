using Xunit;
using System.Collections.Generic;
using Mosaic.UI.Wpf.Scripting.API;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class NamespaceTests
{
    [Fact]
    public void TestSingleLevelNamespace()
    {
        var engine = new ScriptEngine();
        engine.AddNamespace("System", new HashSet<string>
        {
            "System.Int32"
        }, false);

        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var a = System.Int32.Parse('3')
model.a = a
model.b = 9
model.b = System.Collections.ArrayList
"); 
        Assert.Equal(3, model.a);
        Assert.Null(model.b);
    }

    [Fact]
    public void TestEntireNamespace()
    {
        var engine = new ScriptEngine();
        engine.AddNamespace("System", null, true);
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var a = new System.Collections.Generic.Dictionary(System.String, System.Int32)
a.Add('key1', 13)
model.a = a
");
        Assert.Equal(13, model.a["key1"]);
    }

    [Fact]
    public void TestRestrictedNamespace()
    {
        var engine = new ScriptEngine();
        var whitelist = new HashSet<string> {
            "System.Int32",
            "System.String",
            "System.Collections.Generic.Dictionary"
        };

        engine.AddNamespace("System", whitelist, true);
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var a = new System.Collections.Generic.Dictionary(System.String, System.Int32)
a.Add('key1', 13)
model.a = a
model.b = System.Double
");
        Assert.Equal(13, model.a["key1"]);
        Assert.Null(model.b);
    }
}