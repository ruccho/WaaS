---
title: Unity と Rust ではじめる
sidebar_position: 2
---

Rust を WebAssembly にコンパイルして Unity で動かします。

#### 1. [Rust ツールチェイン](https://www.rust-lang.org/ja/learn/get-started)をインストールし、`wasm32-unknown-unknown`ターゲットを追加します。

```sh
rustup target add wasm32-unknown-unknown
```

#### 2. Unity プロジェクトに [WaaS をインストール](./installation.md)します。

#### 3. Unity プロジェクト内に `main.rs` を作成します：

```rust
#[no_mangle]
pub extern "C" fn add(a: i32, b: i32) {
    a + b
}
```

#### 4. `add()` 関数を実行する C# スクリプト `Test.cs` を作成します：

```csharp
using System;
using UnityEngine;
using WaaS.Runtime;
using WaaS.Unity;

public class Test : MonoBehaviour
{
    [SerializeField] private ModuleAsset moduleAsset; // imported main.rs

    private void Start()
    {
        // モジュールのロード
        var module = moduleAsset.LoadModule();

        // インスタンス化
        var instance = new Instance(module, new Imports());

        // エクスポートされた関数の取得
        var function = instance.ExportInstance.Items["add"] as IInvocableFunction;

        // ExecutionContext の作成
        using var context = new ExecutionContext();

        // 引数
        Span<StackValueItem> arguments = stackalloc StackValueItem[2];
        arguments[0] = new StackValueItem(1);
        arguments[1] = new StackValueItem(2);
        
        // 実行
        context.Invoke(function, arguments);
        
        // 戻り値の取得
        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);

        Debug.Log($"Completed: 1 + 2 = {results[0].ExpectValueI32()}"); // Completed: 1 + 2 = 3
    }
}
```

#### 5. `Test.cs` を GameObject にアタッチし、`Module Asset` に `main.rs` をアサインして実行します。

```
> Completed: 1 + 2 = 3
```