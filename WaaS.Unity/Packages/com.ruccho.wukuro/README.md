# Wukuro

Wukuro is an event scripting framework for Unity based on [WaaS](https://github.com/ruccho/WaaS). Event scripts are written in Rust and run as WebAssembly components.

> [!NOTE]
> Wukuro is **very experimental** and breaking changes can be made.

## Requirements

- Unity 6000.0 or later
- [WaaS](https://ruccho.com/WaaS) (`com.ruccho.waas`) with the Rust importer set up
- [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`)

## Installation

Install UniTask and WaaS first, then add the package with the git URL:

```
https://github.com/ruccho/WaaS.git?path=/WaaS.Unity/Packages/com.ruccho.wukuro
```

Rust scripts depend on the crates in [`Wukuro/crates`](../../../Wukuro/crates):

| Crate | Description |
| --- | --- |
| `wukuro` | Core types and functions, and the `#[entry]` attribute |
| `wukuro-macro` | Procedural macros used by `wukuro` |
| `wukuro-dialog` | Dialog functions (`wukuro:dialog`) |

Add them to the dependencies of Rust scripts in the settings of the Rust importer. `wit-bindgen` is also required because `#[entry]` expands to `wit_bindgen::generate!`.

## Scripts

A function with `#[entry]` becomes an entry point. A script can have multiple entry points.

```rust
use wukuro::entry;
use wukuro::functions::*;
use wukuro::types::Context;

#[entry]
fn hello(cx: &Context, count: i32, target: &wukuro::types::Object) {
    log(&format!("hello {count}"));
}
```

An entry point `foo_bar` is exported as the interface `wukuro-entry:foo-bar/entry` with a function `run`.

Parameters can be `&Context`, primitive types, `String`, `&Action`, `&Object` and `&ObjectOf<T>`. The values except `&Context` are given from the arguments set in the inspector, matched by the name in kebab-case.

## Running scripts

Refer to an entry point with `WukuroEntryReference`. The inspector lists the entry points of the script and shows the fields of the arguments.

```csharp
[SerializeField] private WukuroEntryReference entry = new();

private async UniTask PlayAsync(IVariables variables)
{
    var runner = new WukuroRunner();
    runner.AddModule(myModule);
    await runner.RunAsync(entry, variables);
}
```

`WukuroRuntimeArguments` passes arguments from C# at runtime. They take precedence over the arguments set in the inspector.

## Host functions

Functions and types provided to scripts are defined as C# interfaces with the attributes of WaaS (`ComponentInterface`, `ComponentApi`). WaaS generates the WIT of the interfaces, and `WitExportSettings` writes it out for the Rust crates. Register the implementation with `WukuroRunner.RegisterImportProvider`.

Modules implementing `IWukuroModule` are added to the runner with `WukuroRunner.AddModule` and obtained by host functions from the context of the script.

## Object types

Parameters of Unity objects can be constrained to a type.

1. Declare the type in C#:

   ```csharp
   [assembly: WukuroObjectType(typeof(UnityEngine.Transform))]
   ```

2. Generate the marker types for Rust with `WukuroObjectTypeExporter.GenerateRust`. Each marker type implements `wukuro::ObjectType`.

3. Use `&ObjectOf<T>` as the parameter:

   ```rust
   #[entry]
   fn shake(cx: &Context, target: &wukuro::ObjectOf<object_types::Transform>) { /* ... */ }
   ```

`#[entry]` records the type name in the custom section `wukuro-entry-types` of the component, and the inspector accepts only objects of the type. A GameObject is accepted if it has a component of the type.
