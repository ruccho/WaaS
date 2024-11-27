---
title: インスタンス化
sidebar_position: 2
---

ロードしたモジュールを使用するには、インスタンス化が必要です。  
この際、モジュールが要求するオブジェクトをインポートする必要があります。

```csharp
var module = await moduleAsset.LoadModuleAsync();

var instance = new Instance(module, new Imports()
{
    {
        "module name", new ModuleExports()
        {
            { "key", new Memory(new Limits(1)) }
        }
    }
});
```

### 関数のインポート

:::info
詳しくは[バインディング](./bindings.md)を確認してください。
:::

### 線形メモリのインポート

```csharp
var module = await moduleAsset.LoadModuleAsync();

var instance = new Instance(module, new Imports()
{
    {
        "module name", new ModuleExports()
        {
            { "key", new Memory(new Limits(1)) }
        }
    }
});
```


また、ほかのモジュールのインスタンスがエクスポートするオブジェクトをまとめてインポートすることもできます。

```csharp
var instance0 = new Instance(module0, new Imports());

var instance1 = new Instance(module1, new Imports()
{
    { "module 0", instance0 }
});
```
