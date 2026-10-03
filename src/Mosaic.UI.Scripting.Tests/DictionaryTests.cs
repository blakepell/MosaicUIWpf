using Xunit;
using System.Collections.Generic;
using Mosaic.UI.Scripting.API;

namespace Mosaic.UI.Scripting.Test;

public sealed class DictionaryTests
{
    [Fact]
    public void TestDictionary1()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        var dic = new Dictionary<string, int>
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        };
        engine.SetValue("dic", dic);
        engine.ExecuteScript(@"
model.a1 = dic.a
model.a2 = dic['a']
dic.a = 3
model.a3 = dic.a
dic['a'] = 4
model.a4 = dic.a
model.a5 = dic.ContainsKey('b')
dic.Remove('b')
model.a6 = dic.ContainsKey('b')
");
        Assert.Equal(1, model.a1);
        Assert.Equal(1, model.a2);
        Assert.Equal(3, model.a3);
        Assert.Equal(4, model.a4);
        Assert.True(model.a5);
        Assert.False(model.a6);
    }
}