using Xunit;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mosaic.UI.Wpf.Scripting.API;
using Mosaic.UI.Wpf.Scripting.Interop;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class AwaitTests
{
    public async ValueTask<int> GenericValueTask()
    {
        await Task.Delay(1);
        return 33;
    }

    public async ValueTask SimpleValueTask()
    {
        await Task.Delay(1);
        return;
    }

    public async Task<int> GenericTask()
    {
        await Task.Delay(1);
        return 33;
    }

    public async Task SimpleTask()
    {
        await Task.Delay(1);
        return;
    }

    public async IAsyncEnumerator<int> GetAsyncEnumerator()
    {
        yield return 3;
        await Task.Delay(1);
        yield return 1;
        await Task.Delay(1);
        yield return 2;
        await Task.Delay(1);
        yield return 3;
        await Task.Delay(1);
        yield return 3;
    }

    [Fact]
    public void GenericValueTaskAwait()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("test", this);
        engine.SetValue("model", model);
        engine.ExecuteScriptAsync(@"
model.result1 = await test.GenericValueTask();
").Wait();
        Assert.Equal(33, model.result1);
        engine.ExecuteScript(@"
model.result2 = await test.GenericValueTask();
");
        Assert.Equal(33, model.result2);
    }

    [Fact]
    public void SimpleValueTaskAwait()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("test", this);
        engine.SetValue("model", model);
        engine.ExecuteScriptAsync(@"
model.result1 = await test.SimpleValueTask();
").Wait();
        Assert.Null(model.result1);
        engine.ExecuteScript(@"
model.result2 = await test.SimpleValueTask();
");
        Assert.Null(model.result2);
    }

    [Fact]
    public void GenericTaskAwait()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("test", this);
        engine.SetValue("model", model);
        engine.ExecuteScriptAsync(@"
model.result1 = await test.GenericTask();
").Wait();
        Assert.Equal(33, model.result1);
        engine.ExecuteScript(@"
model.result2 = await test.GenericTask();
");
        Assert.Equal(33, model.result2);
    }

    [Fact]
    public void SimpleTaskAwait()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.SetValue("test", this);
        engine.SetValue("model", model);
        engine.ExecuteScriptAsync(@"
model.result1 = await test.SimpleTask();
").Wait();
        Assert.Null(model.result1);
        engine.ExecuteScript(@"
model.result2 = await test.SimpleTask();
");
        Assert.Null(model.result2);
    }

    [Fact]
    public void AsyncEnumerator()
    {
        var engine = new ScriptEngine(new ScriptEngineSetup
        {
            MemberInfoProvider = new CustomMemberInfoProvider()
        });
        dynamic model = new JsObject();
        engine.SetValue("test", this);
        engine.SetValue("model", model);
        engine.ExecuteScriptAsync(@"
let enumerator = test.GetAsyncEnumerator();
var i = 0
while(1) {
    const hasNext = await enumerator.MoveNextAsync();
    if (!hasNext) break;
    ++i;
}
model.result1 = i;
").Wait();
        Assert.Equal(5, model.result1);
        engine.ExecuteScript(@"
enumerator = test.GetAsyncEnumerator();
var i = 0
while(1) {
    const hasNext = await enumerator.MoveNextAsync();
    if (!hasNext) break;
    ++i;
}
model.result2 = i;
");
        Assert.Equal(5, model.result2);
    }

    [Fact]
    public void CustomAwaitHandler()
    {
        var engine = new ScriptEngine(new ScriptEngineSetup { AwaitExpressionHandler = new CustomAwaitExpressionHandler() });
        dynamic model = new JsObject();
        engine.SetValue("test", this);
        engine.SetValue("model", model);
        engine.ExecuteScriptAsync(@"
model.result1 = await test.GenericTask();
").Wait();
        Assert.Equal(33, model.result1);
        engine.ExecuteScript(@"
model.result2 = await test.GenericTask();
");
        Assert.Equal(33, model.result2);
    }

    [Fact]
    public void CustomAwaitHandler2()
    {
        var engine = new ScriptEngine(new ScriptEngineSetup { AwaitExpressionHandler = new CustomAwaitExpressionHandler() });
        dynamic model = new JsObject();
        engine.SetValue("test", this);
        engine.SetValue("model", model);
        engine.ExecuteScriptAsync(@"
var t = test.GenericTask();
model.result1 = await t;
").Wait();
        Assert.Equal(33, model.result1);
        engine.ExecuteScript(@"
var t = test.GenericTask();
model.result2 = await t;
");
        Assert.Equal(33, model.result2);
    }

    public async Task<int> DelayedRunner(Func<TaskData, Task<int>> action, int milliseconds)
    {
        await Task.Delay(milliseconds);

        var taskData = new TaskData();
        var ret = await action(taskData);
        return ret;
    }

    [Fact]
    public void AwaitChain1()
    {
        var engine = new ScriptEngine(new ScriptEngineSetup { AwaitExpressionHandler = new CustomAwaitExpressionHandler() });
        dynamic model = new JsObject();
        engine.SetValue("test", this);
        engine.SetValue("model", model);
        engine.ExecuteScriptAsync(@"
var handler = async function(taskdata)
{
    return taskdata.TestMethod();
}
var t = await test.DelayedRunner(handler, 3);
model.result1 = t;
").Wait();
        Assert.Equal(33, model.result1);
        engine.ExecuteScript(@"
var handler = async function(taskdata)
{
    return taskdata.TestMethod();
}
var t = await test.DelayedRunner(handler, 3);
model.result2 = await t;
");
        Assert.Equal(33, model.result2);
    }
}

public class TaskData
{
    public async Task<int> TestMethod()
    {
        await Task.Delay(11);
        return 33;
    }
}

class CustomAwaitExpressionHandler : IAwaitExpressionHandler
{
    public async Task<object> HandleAwaitExpression(object awaitObject, CancellationToken token)
    {
        if (awaitObject is Task<int> task)
        {
            return await task;
        }
        return awaitObject;
    }
}

class CustomMemberInfoProvider : IMemberInfoProvider
{
    public MemberInfo[] GetInstanceMembers(object instance, string memberName)
    {
        // Handle special case for auto generated async enumerators or
        // explicitly defined interface members using interface map.
        // MoveNextAsync is not accessible through the instance's type
        // and it is not public.
        // https://github.com/dotnet/roslyn/issues/71406

        var type = instance.GetType();
        var interfaces = type.GetTypeInfo().GetInterfaces();
        var list = new List<MemberInfo>();
        foreach (var itype in interfaces)
        {
            list.AddRange(itype.GetMember(memberName, BindingFlags.Public | BindingFlags.Instance));
        }
        list.AddRange(type.GetMember(memberName, BindingFlags.Public | BindingFlags.Instance));
        return list.ToArray();
    }

    public MemberInfo[] GetStaticMembers(Type type, string memberName)
    {
        return type.GetMember(memberName, BindingFlags.Public | BindingFlags.Static);
    }
}