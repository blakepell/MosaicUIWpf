using Xunit;
using System;
using System.Net.Http;
using Mosaic.UI.Wpf.Scripting.API;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class AsyncFunctionTests
{
    [Fact]
    public void HttpGetAsync()
    {
        var engine = new ScriptEngine();
        dynamic model = new JsObject();
        engine.AddType<HttpClient>("HttpClient");
        engine.AddType(typeof(Console), "Console");
        engine.SetValue("model", model);
        engine.AddType<Uri>("Uri");
        var task = engine.ExecuteScriptAsync(@"
async function httpGet(url) {
    try {
        var httpClient = new HttpClient()
        var response = await httpClient.GetAsync(url)
        return await response.Content.ReadAsStringAsync()
    }
    catch (err) {
        Console.WriteLine('Caught Error:\n' + err)
    }
    finally {
        httpClient.Dispose()
    }
}
const html = model.html = await httpGet('http://example.com')
Console.WriteLine(html);
");
        task.Wait();
        Assert.NotNull(model.html);
        Assert.True(model.html.GetType() == typeof(string));
        Assert.True(model.html.StartsWith("<!doctype html>"));
    }
}