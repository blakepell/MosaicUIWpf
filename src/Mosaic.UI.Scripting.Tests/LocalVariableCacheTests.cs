using Xunit;
using System;
using System.Threading.Tasks;
using Mosaic.UI.Wpf.Scripting.API;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class LocalVariableCacheTests
{
    [Fact]
    public void TestIdentifierSharing()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.AddType(typeof(Action), "Action");
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
function f1(p1) {
    model.a = p1
}
function f2(p1) {
    model.b = p1
}
f1(3)
f2(5)
");
        Assert.Equal(3, model.a);
        Assert.Equal(5, model.b);
    }

    [Fact]
    public void TestIdentifierSharingParallel()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.AddType(typeof(Action), "Action");
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
function f1(p1) {
    model.a = p1
}
function f2(p1) {
    model.b = p1
}
f1(3)
f2(5)
");
        var random = new Random();
        Parallel.For(0, 10000, (x) =>
        {
            if (random.Next(0, 1) == 1)
                engine.InvokeFunction("f1", default, 3);
            else
                engine.InvokeFunction("f2", default, 5);
            Assert.Equal(3, model.a);
            Assert.Equal(5, model.b);
        });
        Assert.Equal(3, model.a);
        Assert.Equal(5, model.b);
    }

    public sealed class TestModel
    {
        public int Value = 0;
    }

    [Fact]
    public void TestVariableAdjustmentInScope()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.AddType(typeof(Action), "Action");
        engine.SetValue("model", model);
        engine.SetValue("a1", new TestModel());
        engine.SetValue("a2", new TestModel());
        engine.ExecuteScript(@"
for (var test of [a1,a2])
{
    test.Value++;
    test = a2;
    test.Value++;
}
model.a = a1.Value
model.b = a2.Value
");
        Assert.Equal(1, model.a);
        Assert.Equal(3, model.b);
    }

    [Fact]
    public void TestLetVariableAdjustmentInScope()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.AddType(typeof(Action), "Action");
        engine.SetValue("model", model);
        engine.SetValue("a1", new TestModel());
        engine.SetValue("a2", new TestModel());
        engine.ExecuteScript(@"
for (let test of [a1,a2])
{
    test.Value++;
    test = a2;
    test.Value++;
}
model.a = a1.Value
model.b = a2.Value
");
        Assert.Equal(1, model.a);
        Assert.Equal(3, model.b);
    }

    [Fact]
    public void TestNestedFunctionScopeVariables()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.AddType(typeof(Action), "Action");
        engine.SetValue("model", model);
        engine.SetValue("a1", new TestModel());
        engine.SetValue("a2", new TestModel());
        engine.ExecuteScript(@"
var list = []
var a = 5
function f1() {
 function f2() {
   list.push(a)
 }
 f2()
 var  a = 8
 f2()
}
f1()
a=4;
f1()
model.list = list
");
        Assert.Equal(4, model.list.length);
        // v8 list[0] is undefined.
        // to achieve same behavior we have to process variable declarations first.
        // for performance reasons we don't do that.
        // TODO: if same bahaviour is required, add an optional
        // pre-process and mark all variables once before script execution.
        Assert.Equal(5, model.list[0]); 
        Assert.Equal(8, model.list[1]);
        Assert.Equal(4, model.list[2]);
        Assert.Equal(8, model.list[3]);
    }
}