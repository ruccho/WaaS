# Wukuro crates

Rust crates for writing scripts of [Wukuro](https://github.com/ruccho/WaaS/tree/main/WaaS.Unity/Packages/com.ruccho.wukuro), an event scripting framework for Unity based on WaaS.

| Crate | Description |
| --- | --- |
| `wukuro` | Core types and functions, and the `#[entry]` attribute |
| `wukuro-macro` | Procedural macros used by `wukuro` |
| `wukuro-dialog` | Dialog functions (`wukuro:dialog`) |

The WIT files in `crates/*/wit` are generated from the C# definitions in the Unity package (`Definitions/`) with `WitExportSettings`. Do not edit them by hand.

Build the crates with:

```
cargo build --target wasm32-unknown-unknown
```
