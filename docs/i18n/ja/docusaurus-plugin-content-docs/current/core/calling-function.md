---
title: 関数の呼び出し
sidebar_position: 3
---

モジュールがエクスポートする関数を呼び出すことができます。

インスタンスに対して `TryGetExport` を呼び出し、`IInvocableFunction` インターフェースを取得します。このインターフェースを使用して関数を呼び出すことができます。

```csharp
var module = await moduleAsset.LoadModuleAsync();
var instance = new Instance(module, /* */);

if (!instance.TryGetExport("export name", out IInvocableFunction function)) return;

using var context = new ExecutionContext();

var result = CoreBinder.Instance.Invoke<int /* result type */>(context, function, 1, 1.0, 1.0f);
Debug.Log(result);
```

`CoreBinder.Instance.Invoke()` を使用すると、Boxing を行わずに任意長の引数を関数に渡すことができます。これは内部的に Source Generators を使用しています。

また、Source Generator を使用せずに低レベルな形式で関数を呼び出すこともできます。

```csharp
// set args
Span<StackValueItem> args = stackalloc StackValueItem[3];
args[0] = new StackValueItem(1); // i32
args[1] = new StackValueItem(1.0); // f64
args[2] = new StackValueItem(1.0f); // f32

// invoke
context.Invoke(function, args);

// take results
Span<StackValueItem> results = stackalloc StackValueItem[1];
context.TakeResults(results);
Debug.Log(results[0]);
```

