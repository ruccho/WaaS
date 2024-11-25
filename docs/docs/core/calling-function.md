---
title: Calling a function
sidebar_position: 3
---

You can call functions exported by a module.

Call `TryGetExport` on the instance to get the `IInvocableFunction` interface. You can use this interface to call the function.

```csharp
var module = await moduleAsset.LoadModuleAsync();
var instance = new Instance(module, /* */);

if (!instance.TryGetExport("export name", out IInvocableFunction function)) return;

using var context = new ExecutionContext();

var result = CoreBinder.Instance.Invoke<int /* result type */>(context, function, 1, 1.0, 1.0f);
Debug.Log(result);
```

Using `CoreBinder.Instance.Invoke()`, you can pass an arbitrary number of arguments to a function without boxing. This uses Source Generators internally.

You can also call functions in a low-level format without using Source Generator.

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


