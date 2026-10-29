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

## Limiting execution

`ExecutionContext` has optional limits to stop runaway guest code, such as an infinite loop. All of them are disabled by default.

```csharp
using var context = new ExecutionContext
{
    // each executed instruction consumes one unit of fuel
    Fuel = 1_000_000,
    // or: reset the fuel at the start of an invocation and after each suspension on an async host function
    FuelPerResume = 100_000,
    // memory.grow fails (returns -1) once a memory would exceed this number of pages (64 KiB each)
    MaxMemoryPages = 256,
    // aborts the execution, also while it is suspended on an async host function
    CancellationToken = cancellationToken,
};
```

- When the fuel runs out, `FuelExhaustedException` (a `TrapException`) is thrown. As with other traps, it may be wrapped in a `WasmException` that tells where the execution stopped. The remaining fuel becomes 0, and the host can refill `Fuel` at any time to continue using the context.
- When cancellation is requested, `OperationCanceledException` is thrown. Cancellation is observed at branches and calls while the guest code is running, and immediately while the execution is suspended.
- In both cases the frames of the invocation are discarded, so the context can be used for another invocation.
- An asynchronous host function that is still running when the execution is canceled is not stopped by WaaS. Its later completion is ignored.
