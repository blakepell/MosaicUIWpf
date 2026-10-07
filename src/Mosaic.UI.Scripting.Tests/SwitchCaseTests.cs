using Xunit;
using System.Collections;
using Mosaic.UI.Wpf.Scripting.API;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class SwitchCaseTests
{
    [Fact]
    public void TestSwitchCase1()
    {
        var engine = new ScriptEngine();
        engine.Options.LiteralNumbersAreConvertedToDouble = false;
        engine.Options.NumbersAreConvertedToDoubleInArithmeticOperations = false;
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
function f1(key) {
    var result = 0
    switch (key) {
        case 1: result = 1; break
        case 2: result = 2; break
        default: result = 3
    }
    return result
}

function f2(key) {
    var result = 0
    var loop = true
    while(loop) {
        switch (key) {
            case 1: result = 1; loop = false; continue
            case 2: result = 2; loop = false; continue
            default: result = 3; loop = false; continue
        }
    }
    return result
}

var a = []
i = 0
a[i++] = f1(1) == 1
a[i++] = f1(2) == 2
a[i++] = f1('1') == 3

a[i++] = f2(1) == 1
a[i++] = f2(2) == 2
a[i++] = f2('1') == 3

model.a = a
");

        foreach (var item in (IEnumerable)(model.a))
            Assert.True((bool)item);
    }
}
