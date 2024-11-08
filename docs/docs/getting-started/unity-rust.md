---
title: Unity and Rust Tutorial
sidebar_position: 2
---

# Getting Started

Rust を WebAssembly にコンパイルして Unity で動かします。

1. [Rust ツールチェイン](https://www.rust-lang.org/ja/learn/get-started)をインストールし、`wasm32-unknown-unknown`ターゲットを追加します。

```
rustup target add wasm32-unknown-unknown
```

2. Unity プロジェクトに WaaS をインストールします。

Package Manager から以下の git URL を追加します。

```
https://github.com/ruccho/WaaS.git?path=/Waas.Unity/Packages/com.ruccho.waas
```

3. Unity プロジェクトに `main.rs` を作成します：

```rust
#[no_mangle]
pub extern "C" fn add(a: i32, b: i32) {
    a + b
}
```

4. C# スクリプトから `add()` 関数を実行します：

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