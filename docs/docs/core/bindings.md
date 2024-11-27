---
title: Bindings
sidebar_position: 4
---

You can import C# methods as WebAssembly functions.

```csharp
var module = await moduleAsset.LoadModuleAsync();
var instance = new Instance(module, new Imports()
{
    {
        "module name", new ModuleExports()
        {
            {
                "some function",
                CoreBinder.Instance.ToExternalFunction((Func<int, long, float, double, int>)SomeFunction)
            }
        }
    }
});

static int SomeFunction(int a, long b, float c, double d)
{
    return default;
}
```

This method allows you to call functions without boxing by automatically generating marshalling code internally based on the delegate type.

A low-level method that does not use Source Generator is also available:

```csharp
var module = await moduleAsset.LoadModuleAsync();
var instance = new Instance(module, new Imports()
{
    {
        "module name", new ModuleExports()
        {
            {
                "some function", SomeFunction.Instance
            }
        }
    }
});

private class SomeFunction : ExternalFunction
{
    public static readonly SomeFunction Instance = new();

    public override FunctionType Type { get; } = new(new[]
    {
        ValueType.I32,
        ValueType.I64,
        ValueType.F32,
        ValueType.F64,
    }, new[]
    {
        ValueType.I32,
    });

    public override void Invoke(ExecutionContext context, ReadOnlySpan<StackValueItem> parameters, Span<StackValueItem> results)
    {
        results[0] = new StackValueItem(0);
    }
}
```
