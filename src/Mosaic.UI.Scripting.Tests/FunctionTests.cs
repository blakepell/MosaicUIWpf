using Xunit;
using Mosaic.UI.Wpf.Scripting.API;
using Mosaic.UI.Wpf.Scripting.Options;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class FunctionTests
{
    [Fact]
    public void VariableCapturing()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var funcs = [0, 0, 0]
let i = 3
for (let i = 0; i < 3; ++i) {
    while(true) {
        funcs[i] = function() {
            model[i] = i
        }
        break
    }
}
for (let j = 0; j < 3; j++) {
  funcs[j] ()
}
");
        Assert.Equal(0, model["0"]);
        Assert.Equal(1, model["1"]);
        Assert.Equal(2, model["2"]);
    }

    [Fact]
    public void VariableCapturingConst()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var funcs = [0, 0, 0]
let i = 3
for (const i in funcs) {
    while(true) {
        funcs[i] = function() {
            model[i] = i
        }
        break
    }
}
for (let j = 0; j < 3; j++) {
  funcs[j] ()
}
");
        Assert.Equal(0, model["0"]);
        Assert.Equal(1, model["1"]);
        Assert.Equal(2, model["2"]);
    }

    [Fact]
    public void DefaultValueAssignment()
    {
        var engine = new ScriptEngine();
        engine.Options.NoUndefined = false;
        engine.Options.AllowUndefinedReferenceAccess = true;
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var model = model ?? {}
awesomeFunction = function (url='example.com',
{
  opt1 = true,
  opt2 = 'post',
  opt3 = 123
})
{
    model.url = url
    model.opt1 = opt1
    model.opt2 = opt2
    model.opt3 = opt3
}
awesomeFunction('test.com', {opt3: 'Charming'});
model
");
        Assert.Equal("test.com", model.url);
        Assert.Equal(true, model.opt1);
        Assert.Equal("post", model.opt2);
        Assert.Equal("Charming", model.opt3);
        engine.ExecuteExpression("awesomeFunction('beta.com')");
        Assert.Equal("beta.com", model.url);
        Assert.Equal(Undefined.Value, model.opt1);
        Assert.Equal(Undefined.Value, model.opt2);
        Assert.Equal(Undefined.Value, model.opt3);
    }

    [Fact]
    public void DoubleDefaultValueAssignment()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var model = model ?? {}
awesomeFunction = function (url='example.com',
{
  opt1 = true,
  opt2 = 'post',
  opt3 = 123
} = { opt1: 'xyz' })
{
    model.url = url
    model.opt1 = opt1
    model.opt2 = opt2
    model.opt3 = opt3
}
awesomeFunction('test.com',{opt3: 'Charming'});
model
");
        Assert.Equal("test.com", model.url);
        Assert.Equal(true, model.opt1);
        Assert.Equal("post", model.opt2);
        Assert.Equal("Charming", model.opt3);
        engine.ExecuteExpression("awesomeFunction('beta.com')");
        Assert.Equal("beta.com", model.url);
        Assert.Equal("xyz", model.opt1);
        Assert.Equal("post", model.opt2);
        Assert.Equal(123, model.opt3);
        engine.ExecuteExpression("awesomeFunction()");
        Assert.Equal("example.com", model.url);
        Assert.Equal("xyz", model.opt1);
        Assert.Equal("post", model.opt2);
        Assert.Equal(123, model.opt3);
        engine.ExecuteExpression("awesomeFunction(5, { opt1: 333 })");
        Assert.Equal(5, model.url);
        Assert.Equal(333, model.opt1);
        Assert.Equal("post", model.opt2);
        Assert.Equal(123, model.opt3);
    }

    [Fact]
    public void ClosureVariableSharing()
    {
        var engine = new ScriptEngine();
        engine.Options.AssignmentWithoutDefinitionBehavior
            = AssignmentWithoutDefinitionBehavior.DefineAsVarInGlobalScope;
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var model = model ?? {}
var a = 1
let b = 2
function f1(x)
{
    a = 2
    model.capturedB = b
    b = b + x
    model.modifiedCapturedB = b
    c = 8
    model.c = c
}
f1(1)
model.a = a
model.b = b
model.outerC = c
");
        Assert.Equal(2, model.a);
        Assert.Equal(2, model.capturedB);
        Assert.Equal(3, model.b);
        Assert.Equal(3, model.modifiedCapturedB);
        Assert.Equal(8, model.c);
        Assert.Equal(8, model.outerC);
    }

    [Fact]
    public void ClosureVariableSharing2()
    {
        var engine = new ScriptEngine();
        engine.Options.AssignmentWithoutDefinitionBehavior
            = AssignmentWithoutDefinitionBehavior.DefineAsVarInFirstChildOfGlobalScope;
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var model = model ?? {}
var a = 1
let b = 2
function f1(x)
{
    a = 2
    model.capturedB = b
    b = b + x
    model.modifiedCapturedB = b
    c = 8
    model.c = c
    function f2() {
        y = 9
        model.y = y
    }
    f2()
    model.outerY = y
}
f1(1)
model.a = a
model.b = b
model.outerC = c
model.globalY = y
");
        Assert.Equal(2, model.a);
        Assert.Equal(2, model.capturedB);
        Assert.Equal(3, model.b);
        Assert.Equal(3, model.modifiedCapturedB);
        Assert.Equal(8, model.c);
        Assert.Null(model.outerC);
        Assert.Equal(9, model.y);
        Assert.Equal(9, model.outerY);
        Assert.Null(model.globalY);
    }

    [Fact]
    public void ClosureVariableSharing3()
    {
        var engine = new ScriptEngine();
        engine.Options.AssignmentWithoutDefinitionBehavior
            = AssignmentWithoutDefinitionBehavior.DefineAsVarInFirstChildOfGlobalScope;
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var model = model ?? {}
var a = 1
function f1(x)
{
    function f2(y) {
        a = x + y
        model.a = a
        b = x * y
        return a + b
    }
    model.b = b
    return f2
}
model.p = f1(1)(3)
model.q = f1(2)(4)
model.a = a
");
        Assert.Equal(6, model.a);
        Assert.Null(model.b);
        Assert.Equal(7, model.p);
        Assert.Equal(14, model.q);
    }

    [Fact]
    public void FunctionInformation()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
function f1(a, b, c)
{
}
function f2(a, b=34, c)
{
}
model.f1 = f1
model.f2 = f2
");
        var f1 = model.f1 as IScriptFunction;
        var f2 = model.f2 as IScriptFunction;

        Assert.Equal("f1", f1.Name);
        Assert.Equal(3, f1.Length);
        Assert.Equal("a", f1[0]);
        Assert.Equal("b", f1[1]);
        Assert.Equal("c", f1[2]);

        Assert.Equal("f2", f2.Name);
        Assert.Equal(3, f2.Length);
        Assert.Equal("a", f2[0]);
        Assert.Equal("b", f2[1]);
        Assert.Equal("c", f2[2]);
        f1.Invoke(new(), 1, 2, 3);
        f2.Invoke(new(), 1, 2, 5.7);
        f1.InvokeAsync(new(), 1, true, 3, 5);
        f2.InvokeAsync(new(), "test", 2, 3);
    }

    [Fact]
    public void TestRestFunctionArguments()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
function sum(...arguments) {
    var result = 0,
        argumentIndex,
        argumentCount = arguments.length;

    for (argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++) {
        result += arguments[argumentIndex];
    }

    return result;
}
model.X = sum(3,5,7)
model.Y = sum()
");
        Assert.Equal(15, model.X);
        Assert.Equal(0, model.Y);
    }

    [Fact]
    public void TestRestFunctionArgumentsAsync()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScriptAsync(@"
function sum(...arguments) {
    var result = 0,
        argumentIndex,
        argumentCount = arguments.length;

    for (argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++) {
        result += arguments[argumentIndex];
    }

    return result;
}
model.X = sum(3,5,7)
model.Y = sum()
").Wait();
        Assert.Equal(15, model.X);
        Assert.Equal(0, model.Y);
    }
    [Fact]
    public void TestSpecialFunctionArguments()
    {
        var engine = new ScriptEngine();
        engine.Options.DefineSpecialArgumentsObjectOnEachFunctionCall = true;

        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
function sum() {
    var result = 0,
        argumentIndex,
        argumentCount = arguments.length;

    for (argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++) {
        result += arguments[argumentIndex];
    }

    return result;
}
model.X = sum(3,5,7)
model.Y = sum()
");
        Assert.Equal(15, model.X);
        Assert.Equal(0, model.Y);
    }

    [Fact]
    public void TestSpecialFunctionArgumentsAsync()
    {
        var engine = new ScriptEngine();
        engine.Options.DefineSpecialArgumentsObjectOnEachFunctionCall = true;

        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScriptAsync(@"
function sum() {
    var result = 0,
        argumentIndex,
        argumentCount = arguments.length;

    for (argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++) {
        result += arguments[argumentIndex];
    }

    return result;
}
model.X = await sum(3,5,7)
model.Y = await sum()
").Wait();
        Assert.Equal(15, model.X);
        Assert.Equal(0, model.Y);
    }
}