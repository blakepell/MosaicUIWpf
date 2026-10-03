using Xunit;
using Mosaic.UI.Scripting.API;

namespace Mosaic.UI.Scripting.Test;

public sealed class VariableTypeTests
{
    [Fact]
    public void TestVariableTypes()
    {
        var engine = new ScriptEngine();
        engine.Options.LiteralNumbersAreConvertedToDouble = false;
        engine.Options.NumbersAreConvertedToDoubleInArithmeticOperations = false;
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
model.a = 0
model.b = 1
model.c = 1.1
model.d = 2147483647
model.e = 2147483647 + 1
model.f = 2147483647 * 2147483647
model.g = -2147483647
model.h = -2147483647-1
model.i = -2147483647-2
model.j = 2147483647 * 2147483647 * 2147483647 * 2147483647
model.k = 2 ** 31
model.l = 2 ** 63
model.m = 2 ** 65
");         
        Assert.True(model.a is int);
        Assert.True(model.b is int);
        Assert.True(model.c is double);
        Assert.True(model.d is int);
        Assert.True(model.e is long);
        Assert.True(model.f is long);
        Assert.True(model.g is int);
        Assert.True(model.h is int);
        Assert.True(model.i is long);
        Assert.True(model.j is double);
        Assert.True(model.k is double);
        Assert.True(model.l is double);
        Assert.True(model.m is double);
    }

    public void TestVariableTypesDouble()
    {
        var engine = new ScriptEngine();
        engine.Options.LiteralNumbersAreConvertedToDouble = false;
        engine.Options.NumbersAreConvertedToDoubleInArithmeticOperations = true;
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
model.a = 0
model.b = 1
model.c = 1.1
model.d = 2147483647
model.e = 2147483647 + 1
model.f = 2147483647 * 2147483647
model.g = -2147483647
model.h = -2147483647-1
model.i = -2147483647-2
model.j = 2147483647 * 2147483647 * 2147483647 * 2147483647
model.k = 2 ** 31
model.l = 2 ** 63
model.m = 2 ** 65
");
        Assert.True(model.a is int);
        Assert.True(model.b is int);
        Assert.True(model.c is double);
        Assert.True(model.d is double);
        Assert.True(model.e is double);
        Assert.True(model.f is double);
        Assert.True(model.g is double);
        Assert.True(model.h is double);
        Assert.True(model.i is double);
        Assert.True(model.j is double);
        Assert.True(model.k is double);
        Assert.True(model.l is double);
        Assert.True(model.m is double);
    }
}