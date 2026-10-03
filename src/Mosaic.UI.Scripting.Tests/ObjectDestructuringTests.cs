using Xunit;
using Mosaic.UI.Scripting.API;

namespace Mosaic.UI.Scripting.Test;

public sealed class ObjectDestructuringTests
{
    [Fact]
    public void DefineObjectWithVariables()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var a = 5, g = 9
var rest = [1,2,3], kest = [1,2,4]
var b = {
    a,
    ...rest,
    ...kest,
    g
}
model.b = b
");
        Assert.Equal(5, model.b.a);
        Assert.Equal(1, model.b["0"]);
        Assert.Equal(2, model.b["1"]);
        Assert.Equal(4, model.b["2"]);
        Assert.Equal(9, model.b.g);
    }

    [Fact]
    public void DefineVariablesWithObjectAssignmentPattern()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
({ a, b = 3 } = { a: 1 })
model.a = a
model.b = b;
({nan, y = 33} = NaN)
model.nan = nan
model.y = y;
({ a, b = 3, ...c } = { a: 1, c: [11,22,33] })
model.c = c
");
        Assert.Equal(1, model.a);
        Assert.Equal(3, model.b);
        Assert.Null(model.nan);
        Assert.Equal(33, model.y);
        Assert.Equal(11, model.c[0]);
        Assert.Equal(22, model.c[1]);
        Assert.Equal(33, model.c[2]);
    }

    [Fact]
    public void DefineVariablesWithObjectDestructuring()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var { a, b = 3 } = { a: 1 }
model.a = a
model.b = b;
var {nan, y = 33} = NaN
model.nan = nan
model.y = y;
var { a, b = 3, ...c } = { a: 1, c: [11,22,33] }
model.c = c
var { foo, bar } = { foo: 'lorem', bar: 'ipsum' };
model.foo = foo
model.bar = bar
");
        Assert.Equal(1, model.a);
        Assert.Equal(3, model.b);
        Assert.Null(model.nan);
        Assert.Equal(33, model.y);
        Assert.Equal(11, model.c[0]);
        Assert.Equal(22, model.c[1]);
        Assert.Equal(33, model.c[2]);
        Assert.Equal("lorem", model.foo);
        Assert.Equal("ipsum", model.bar);
    }

    [Fact]
    public void NestedObjectDestructuring()
    {
        var engine = new ScriptEngine();
        engine.Options.NoUndefined = false;
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var complicatedObj = {
  arrayProp: [
    'Zapp',
    { 
        second: 'Brannigan',
        third: {
            cool: 'deeper'
        }
    }
  ]
};
var { arrayProp: [first, { second, third: { cool } }] } = complicatedObj;
model.first = first
model.second = second
model.arrayProp = arrayProp
model.cool = cool
");
        Assert.Equal("Zapp", model.first);
        Assert.Equal("Brannigan", model.second);
        Assert.Equal(Undefined.Value, model.arrayProp);
        Assert.Equal("deeper", model.cool);
    }

    [Fact]
    public void TestMethodArgumentDestructuring()
    {
        var js = @"
let getFullName = ({firstName, lastName}) => `${firstName} ${lastName}`;

let person = {
    firstName: 'John',
    lastName: 'Bruno'
};

model.fullname = getFullName(person);
";
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(js);

        Assert.Equal("John Bruno", model.fullname);
    }

}