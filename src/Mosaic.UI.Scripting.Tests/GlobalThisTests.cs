using Xunit;
using Mosaic.UI.Wpf.Scripting.API;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class GlobalThisTests
{
    [Fact]
    public void TestGlobalThis1()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("globalThis", new GlobalThis(engine.GlobalScope));
        engine.SetValue("model", model);
        engine.SetValue("JSON", new JSONObject());
        engine.ExecuteScript(@"
model.a = globalThis.JSON
model.b = globalThis.model
var x = 3
model.c = globalThis.x
");
        Assert.Equal("3", ((JSONObject)model.a).stringify(3));
        Assert.Equal(model, model.b);
        Assert.Equal(3, model.c);
    }
}