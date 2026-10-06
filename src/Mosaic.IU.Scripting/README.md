# Mosaic UI Scripting

Mosaic.UI.Scripting embeds a JavaScript-style scripting engine in .NET applications. Expose application data and .NET methods to scripts, evaluate expressions, and run scripts synchronously or asynchronously.

The library targets **.NET 10** and can be used independently of the Mosaic WPF controls. It does not require WPF.

## Installation

```shell
dotnet add package Mosaic.UI.Scripting
```

## Quick start

```csharp
using System;
using Mosaic.UI.Scripting;

var engine = new ScriptEngine();

// Supply values and a .NET callback to the script.
engine.SetValue("quantity", 3);
engine.SetValue("unitPrice", 12.5);
engine.SetValue("print", new Action<object>(Console.WriteLine));

engine.ExecuteScript(@"
    const total = quantity * unitPrice;
    print(total);

    function addTax(amount) {
        return amount * 1.1;
    }

    const totalWithTax = addTax(total);
");

Console.WriteLine(engine.GetValue("total")); // 37.5
var totalWithTax = engine.GetValue("totalWithTax");

// Evaluate an expression using the same engine scope.
var nextTotal = engine.ExecuteExpression("(quantity + 1) * unitPrice"); // 50
```

Use `ExecuteScriptAsync` and `ExecuteExpressionAsync` for asynchronous execution. Execution methods accept a `CancellationToken`. Host applications can also expose .NET types through `AddType`, and configure engine behavior through `ScriptEngineSetup` and `ScriptEngineOptions`.

## Language and interoperability

The runtime supports variables, functions, loops, arrays, objects, .NET interoperability, and extensions such as `include` and LINQ queries. It implements a subset of JavaScript with .NET-oriented behavior; it is not a browser or Node.js runtime. Browser globals and Node.js APIs are not supplied automatically.

Read the compatibility guide before relying on standard JavaScript semantics or built-in globals.

## Documentation

- [Language reference](https://github.com/blakepell/MosaicUIWpf/blob/main/src/Mosaic.IU.Scripting/Docs/README.md)
- [Compatibility and troubleshooting](https://github.com/blakepell/MosaicUIWpf/blob/main/src/Mosaic.IU.Scripting/Docs/Compatibility.md)
- [Working with application and .NET objects](https://github.com/blakepell/MosaicUIWpf/blob/main/src/Mosaic.IU.Scripting/Docs/Application-Objects.md)
- [Errors and asynchronous execution](https://github.com/blakepell/MosaicUIWpf/blob/main/src/Mosaic.IU.Scripting/Docs/Errors-and-Async.md)
- [Recipes](https://github.com/blakepell/MosaicUIWpf/blob/main/src/Mosaic.IU.Scripting/Docs/Recipes.md)

The package also includes the language reference in its `Docs` directory and XML API documentation alongside the assembly.

## Project

Mosaic UI Scripting is part of [Mosaic UI for WPF](https://github.com/blakepell/MosaicUIWpf), based on the Topaz engine by Ahmed Yasin Koculu, with development by Blake Pell.

Report problems and request features in the [issue tracker](https://github.com/blakepell/MosaicUIWpf/issues).
