using Xunit;
using System.Collections;
using System.Text.RegularExpressions;
using Mosaic.UI.Scripting;
using Mosaic.UI.Scripting.API;

namespace Mosaic.UI.Scripting.Test;

public sealed class AddNamespaceTests
{
    [Fact]
    public void TestAddNamespace1()
    {
        var engine = new ScriptEngine();
        engine.AddNamespace("System", null, true);
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
        let sb = new System.Text.StringBuilder()
        sb.Append('1234')
        model.data = sb.ToString()
");
        Assert.Equal("1234", model.data);
    }

    [Fact]
    public void TestAddNamespace2()
    {
        var engine = new ScriptEngine();
        engine.AddNamespace("System.Text", null, true);
        dynamic model = new JsObject();
        engine.SetValue("model", model);

        engine.ExecuteScript(@"
        let sb = new System.Text.StringBuilder()
        sb.Append('1234')
        model.data = sb.ToString()
        model.reg = new System.Text.RegularExpressions.Regex('\w');
");
        Assert.Equal("1234", model.data);
        Assert.IsAssignableFrom<Regex>(model.reg);
    }

    [Fact]
    public void TestAddNamespace3()
    {
        var engine = new ScriptEngine();
        engine.AddNamespace("System.Text", null, false);
        dynamic model = new JsObject();
        engine.SetValue("model", model);

        engine.ExecuteScript(@"
        let sb = new System.Text.StringBuilder()
        sb.Append('1234')
        model.data = sb.ToString()
        model.reg = System.Text.RegularExpressions.Regex;
");
        Assert.Equal("1234", model.data);
        Assert.Null(model.reg);
    }

    [Fact]
    public void TestAddNamespace4()
    {
        var engine = new ScriptEngine();
        engine.AddNamespace("System.Text", null, true);
        dynamic model = new JsObject();
        engine.SetValue("model", model);

        engine.ExecuteScript(@"
        let sb = new System.Text.StringBuilder()
        sb.Append('1234')
        model.data = sb.ToString()
        model.appDomain = System.AppDomain;
");
        Assert.Equal("1234", model.data);
        Assert.Null(model.appDomain);
    }
}