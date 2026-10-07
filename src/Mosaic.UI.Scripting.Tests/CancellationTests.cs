using Xunit;
using System;
using System.Threading;

namespace Mosaic.UI.Wpf.Scripting.Test;

public sealed class CancellationTests
{
    [Fact]
    public void CancelInfiniteWhileLoop()
    {
        var engine = new ScriptEngine();
        var thrown = 0;
        Func<CancellationTokenSource> getSource = 
            () => new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        
        try
        {
            using var source = getSource();
            engine.ExecuteScript(@"
while(true) { }
",
source.Token);
        }
        catch (OperationCanceledException)
        {
            ++thrown;
        }

        try
        {
            using var source = getSource();
            engine.ExecuteScript(@"
while(true);
",
source.Token);
        }
        catch (OperationCanceledException)
        {
            ++thrown;
        }

        Assert.Equal(thrown, 2);
    }
}