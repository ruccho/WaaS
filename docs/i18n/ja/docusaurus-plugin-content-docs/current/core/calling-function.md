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

## 実行の制限

`ExecutionContext` には、無限ループなどで止まらなくなったゲストコードを停止するための制限を設定できます。いずれも既定では無効です。

```csharp
using var context = new ExecutionContext
{
    // 実行された命令ごとに 1 の燃料 (fuel) を消費する
    Fuel = 1_000_000,
    // または、呼び出しの開始時と非同期ホスト関数による中断からの再開時に燃料を再設定する
    FuelPerResume = 100_000,
    // メモリがこのページ数 (1 ページ 64 KiB) を超える場合、memory.grow は失敗する (-1 を返す)
    MaxMemoryPages = 256,
    // 非同期ホスト関数で中断している間も含めて実行を中止する
    CancellationToken = cancellationToken,
};
```

- 燃料が尽きると `FuelExhaustedException` (`TrapException` の派生クラス) がスローされます。ほかのトラップと同様に、停止した位置を示す `WasmException` に包まれる場合があります。残りの燃料は 0 になり、ホストはいつでも `Fuel` を再設定してコンテキストを使い続けることができます。
- キャンセルが要求されると `OperationCanceledException` がスローされます。ゲストコードの実行中は分岐と呼び出しの時点で、中断中は直ちにキャンセルが検知されます。
- いずれの場合も呼び出しのフレームは破棄されるため、同じコンテキストで別の呼び出しを行うことができます。
- キャンセル時に実行中の非同期ホスト関数は WaaS によって停止されません。その後の完了は無視されます。
