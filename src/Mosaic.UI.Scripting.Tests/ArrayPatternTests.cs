using Xunit;
using Mosaic.UI.Wpf.Scripting.API;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class ArrayPatternTests
{
    [Fact]
    public void MissingElements()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var [,,third] = ['foo', 'bar', 'baz'];
model.baz = third
");
        Assert.Same(Undefined.Value, model["foo"]);
        Assert.Equal("baz", model.baz);
    }

    [Theory]
    [InlineData("var")]
    [InlineData("let")]
    [InlineData("const")]
    [InlineData("")]
    public void MultipleDeclarationWithRest(string variableKind)
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@$"
{variableKind} [a, b, ...rest] = [10, 20, 30, 40, 50], d = 2;
model.a = a
model.b = b
model.rest = rest
model.d = d
");
        Assert.Equal(10, model.a);
        Assert.Equal(20, model.b);
        Assert.Equal(30, model.rest[0]);
        Assert.Equal(40, model.rest[1]);
        Assert.Equal(50, model.rest[2]);
        Assert.Equal(2, model.d);
    }

    [Fact]
    public void DefaultValueAssignmentPattern()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var f = (x=1) => x * x
model.a = f()
model.b = f(2)
model.c = f(3,4)
model.d = f(null,5)
");
        Assert.Equal(1, model.a);
        Assert.Equal(4, model.b);
        Assert.Equal(9, model.c);
        Assert.Equal(0, model.d);
    }

    [Theory]
    [InlineData("var")]
    [InlineData("let")]
    [InlineData("const")]
    [InlineData("")]
    public void NestedAssignment(string variableKind)
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@$"
{variableKind} [a, [[b], c]] = [1, [[2], 3]]
model.a = a
model.b = b
model.c = c
");
        Assert.Equal(1, model.a);
        Assert.Equal(2, model.b);
        Assert.Equal(3, model.c);
    }

    [Fact]
    public void ArrayInitializationWithVariables()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var a = 1, b = 2, c = [3, 4]
var d = [a, b, ...c, 5]
model.d = d
");
        Assert.Equal(1, model.d[0]);
        Assert.Equal(2, model.d[1]);
        Assert.Equal(3, model.d[2]);
        Assert.Equal(4, model.d[3]);
        Assert.Equal(5, model.d[4]);
    }

    [Fact]
    public void ArrayVariableUpdateVariableEdgeCase()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var a = 1, b = 2, c = 3
d = [a, b, c]
b = 3
model.d = d
");
        Assert.Equal(2, model.d[1]);
    }

    [Fact]
    public void ObjectVariableUpdateVariableEdgeCase()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var a = 1, b = 2, c = 3
d = {a, b, c}
b = 3
model.d = d
");
        Assert.Equal(2, model.d.b);
    }

    [Fact]
    public void ArrayExpressionWithRest()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("JSON", new JSONObject());
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var a = [1,2,3]
var b = [5,...a,6,7]
model.b = JSON.stringify(b)
");
        Assert.Equal("[5,1,2,3,6,7]", model.b);
    }
}