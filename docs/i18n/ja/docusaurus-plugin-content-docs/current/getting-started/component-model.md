---
title: Component Model ではじめる
sidebar_position: 3
---

WaaS の **Component Model API** を使用すると、[WebAssembly Component Model](https://component-model.bytecodealliance.org/) に基づくコンポーネントを実行できます。

通常の WebAssembly モジュールでは引数や戻り値の型が数値に限られていましたが、Component Model では、より複雑な型を持つデータをやり取りすることができます。

- 整数型
- 浮動小数数点数型
- 論理値型
- 文字型
- 文字列型
- レコード型
- バリアント型
- リスト型
- タプル型
- フラグ型
- 列挙型
- `option`型
- `result`型
- リソース型

### 1. WITの作成

Component Modelでは、[**WIT**](https://component-model.bytecodealliance.org/design/wit.html) (WebAssembly Interface Type) というIDLを使って型や関数のシグネチャを事前に定義します。
以下は簡単な会話シーンで使うことを想定したWITの例です。

```wit
package my-game:my-sequencer;

world sequence {
    import env;
    export play: func();
}

interface env {
    show-message: func(speaker: string, message: string);
    show-options: func(options: list<string>) -> u32;
}
```

雰囲気だけ掴んでもらえば大丈夫ですが、`world sequence`はこのWebAssemblyコンポーネントのインポート・エクスポートする機能のセットを定義するものです。`interface env`はホスト環境側で実装しコンポーネントにインポートする機能です。

### 2. ゲスト言語側の作業

WITが作成できたら、WITから各ゲスト言語用のバインディングを生成します。これには[`wit-bindgen`](https://github.com/bytecodealliance/wit-bindgen)というツールを使います。現在はRust, C, Java, Go, C#, Moonbit向けのバインディング生成に対応しています。Rust向けには、WITからのコード生成をビルドパイプラインに組み込む[`cargo-component`](https://github.com/bytecodealliance/cargo-component)というツールが公開されているので、今回はこちらを使います。

```
cargo component new hello-world --lib
```

先ほどのWITを`wit/world.wit`に置いておきます。

以下は、Rust用に生成されたバインディングを利用して書かれたスクリプトの例です。`env`に定義した関数を呼び出しています。

```rust
#[allow(warnings)]
mod bindings;

use bindings::my_game::my_sequencer::env::*;
use bindings::Guest;

struct Component;

impl Guest for Component {
    fn play() {
        show_message("シグモ", "よければ……バッテリーを持ってきてくれないですか");
        match show_options(&["いいよ".to_string(), "だめ".to_string()]) {
            0 => show_message("シグモ", "ありがとう……"),
            1 => show_message("シグモ", "えっ……"),
            _ => {}
        }
        show_message("シグモ", "……");
    }
}

bindings::export!(Component with_types_in bindings);
```

`cargo-component`では、以下のコマンドでWITからのバインディング生成とコンポーネントの作成までやってくれます。

```
cargo component build --release --target wasm32-unknown-unknown
```

結果は`target/wasm32-unknown-unknown/release/hello_world.wasm`に出力されます。こちらをUnityプロジェクトにインポートしておきます。

### 3. C#側の作業

さて、コンポーネントを実際に動かす前に、`show-message`と`show-options`をC#側で実装する必要があります。これにはまずWaaSで提供している`wit2waas`ツールを使用して、WITをC#のインターフェースに変換します。すると、先ほどのWITから次のようなC#コードが生成されます。

```cs
// <auto-generated />
#nullable enable

namespace MyGame.MySequencer
{
    [global::WaaS.ComponentModel.Binding.ComponentInterface(@"env")]
    public partial interface IEnv
    {
        [global::WaaS.ComponentModel.Binding.ComponentApi(@"show-message")]
        global::System.Threading.Tasks.ValueTask ShowMessage(string @speaker, string @message);

        [global::WaaS.ComponentModel.Binding.ComponentApi(@"show-options")]
        global::System.Threading.Tasks.ValueTask<uint> ShowOptions(global::System.ReadOnlyMemory<string> @options);

    }
}
```

いろいろと属性がついていますが、これによってSource Generatorが具体的にWebAssemblyと値をやり取りするためのコードを生成します。ご覧の通りシンプルなものですので、WITからではなく手書きすることも十分できるでしょう。

次は、`IEnv`を実装しておきます。

```cs
class Env : IEnv
{
    public static readonly Env Instance = new();

    public async ValueTask ShowMessage(string speaker, string message)
    {
        Debug.Log($"{speaker}: {message}");
    }

    public async ValueTask<uint> ShowOptions(ReadOnlyMemory<string> options)
    {
        Debug.Log($"Options: {string.Join(", ", options.ToArray())}");
        return 0;
    }
}
```

これで準備が整ったので、実行します。`IEnv`を実装したクラスのインスタンスをコンポーネントに渡すと、コンポーネントの内部から`ShowMessage()`や`ShowOptions()`が呼び出されます。

```cs
[SerializeField] private ComponentAsset componentAsset;

var component = componentAsset.LoadComponent();

// コンポーネントのロード
var instance = component.Instantiate(null, new Dictionary<string, ISortedExportable>()
{
    // `env`実装のインポート
    { "my-game:my-sequencer/env", IEnv.CreateWaaSInstance(Env.Instance) }
});

using var context = new ExecutionContext();
var wrapper = new ISequence.Wrapper(instance, context);

// 実行
await wrapper.Play();
```