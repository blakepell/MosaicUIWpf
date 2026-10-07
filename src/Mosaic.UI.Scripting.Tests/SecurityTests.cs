using Xunit;
using System;
using System.Reflection;
using Mosaic.UI.Wpf.Scripting.API;
using Mosaic.UI.Wpf.Scripting.Options;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class SecurityTests
{
    [Fact]
    public void TryToUseReflectionAPI()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.SetValue("args", new[] { typeof(DateTime) });
        engine.ExecuteScript(@"
try {
    var m = model.GetType().Assembly
    model.m = m
    model.a = m.GetType('System.DateTime')
    var activator = m.GetType('System.Activator');
    var createInstance = activator.GetMember('CreateInstance')[3]
    var newDateTime = createInstance.Invoke(null, args);
    model.newDateTime = newDateTime
}
catch(err) {
    model.b = err
}
");
        Assert.Same(Undefined.Value, model["a"]);
        Assert.Same(Undefined.Value, model["m"]);
        Assert.IsAssignableFrom<ScriptException>(model.b);
        Assert.Same(Undefined.Value, model["newDateTime"]);
    }

    [Fact]
    public void EnableReflectionAPI()
    {
        var engine = new ScriptEngine();
        engine.Options.SecurityPolicy = SecurityPolicy.EnableReflection;
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.SetValue("args", new[] { typeof(DateTime) });
        engine.ExecuteScript(@"
try {
    var m = (typeof '').GetType().Assembly
    model.m = m
    model.a = m.GetType('System.DateTime')
    var activator = m.GetType('System.Activator');
    var createInstance = activator.GetMember('CreateInstance')[3]
    var newDateTime = createInstance.Invoke(null, args);
    model.newDateTime = newDateTime
}
catch(err) {
    throw err
}
");
        Assert.NotNull(model.a);
        Assert.Equal(typeof(DateTime), model.a);
        Assert.Equal(new DateTime(), model.newDateTime);
        Assert.IsAssignableFrom<Assembly>(model.m);
    }
}