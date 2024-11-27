---
title: バインディング
sidebar_position: 4
---

C# のメソッドを WebAssembly の関数としてインポートすることができます。

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

この方法は、デリゲート型に合わせて Source Generator が内部的なマーシャリングのコードを自動生成することで、Boxing なしに関数の呼び出しを行うことができます。

Source Generator を使用しない低レベルな方法も使用可能です：

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