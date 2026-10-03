using Xunit;
using System;
using System.Collections;
using System.Text.Json;
using Mosaic.UI.Scripting.API;

namespace Mosaic.UI.Scripting.Test;

public sealed class BasicObjectTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TestObject1(bool useThreadSafeJsObjects)
    {
        var engine = new ScriptEngine();
        engine.Options.NoUndefined = true;
        engine.Options.UseThreadSafeJsObjects = useThreadSafeJsObjects;
        engine.AddType<DateTime>("DateTime");
        dynamic model = new JsObject();
        engine.SetValue("model", model);
        engine.ExecuteScript(@"
var a = {
    nested: {
        n1: 1,
        n2: 2,
        n3 : 3
    }
    a1: 1
    'a2': 2    
}
a.a3 = 3
a[new DateTime()] = 4
a[null] = 5
model.js = a
model.p1 = a.p1
");
        var js = model.js;
        var json = JsonSerializer.Serialize((object)js);
        Console.WriteLine(json);
        dynamic deserialized;
        if (useThreadSafeJsObjects)
        {
            deserialized = JsonSerializer.Deserialize<ConcurrentJsObject>(json);
            var keys = deserialized.Keys;
            var values = deserialized.Values;
            Assert.Equal(6, keys.Count);
            Assert.Equal(6, values.Count);
            AssertDictionaryEquivalent(js, deserialized);
        }
        else
        {
            deserialized = JsonSerializer.Deserialize<JsObject>(json);
            var keys = deserialized.Keys;
            var values = deserialized.Values;
            Assert.Equal(6, keys.Count);
            Assert.Equal(6, values.Count);
            AssertDictionaryEquivalent(js, deserialized);
        }

        Assert.Equal(1, js.a1);
        Assert.Equal(2, js.a2);
        Assert.Equal(3, js.a3);
        Assert.Equal(4, js[new DateTime()]);
        Assert.Equal(5, js[null]);
        Assert.Null(model.p1);
    }

    [Fact]
    public void TestInQuery()
    {
        var engine = new ScriptEngine();
        engine.ExecuteScript("let obj = { a: { b: '123' }}");
        Assert.Equal(true, engine.ExecuteExpression("'b' in obj.a"));
        Assert.Equal(true, engine.ExecuteExpression("obj.a.Contains('b')"));
        Assert.Equal(true, engine.ExecuteExpression("obj.a.hasOwn('b')"));
        Assert.Equal(true, engine.ExecuteExpression("obj.a.hasOwnProperty('b')"));
        Assert.Equal(false, engine.ExecuteExpression("'c' in obj.a"));
        Assert.Equal(false, engine.ExecuteExpression("obj.a.Contains('c')"));
        Assert.Equal(false, engine.ExecuteExpression("obj.a.hasOwn('c')"));
        Assert.Equal(false, engine.ExecuteExpression("obj.a.hasOwnProperty('c')"));
    }

    /// <summary>
    /// Recursively compares two dictionaries by key, treating numeric values of differing CLR types as equal
    /// when their values match (mirrors the dictionary comparison NUnit's AreEqual performed).
    /// </summary>
    private static void AssertDictionaryEquivalent(IDictionary expected, IDictionary actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach (var key in expected.Keys)
        {
            Assert.True(actual.Contains(key), $"Missing key: {key}");
            var expectedValue = expected[key];
            var actualValue = actual[key];

            if (expectedValue is IDictionary expectedChild && actualValue is IDictionary actualChild)
            {
                AssertDictionaryEquivalent(expectedChild, actualChild);
            }
            else if (IsNumeric(expectedValue) && IsNumeric(actualValue))
            {
                Assert.Equal(Convert.ToDouble(expectedValue), Convert.ToDouble(actualValue));
            }
            else
            {
                Assert.Equal(expectedValue, actualValue);
            }
        }
    }

    private static bool IsNumeric(object value) => value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
}
