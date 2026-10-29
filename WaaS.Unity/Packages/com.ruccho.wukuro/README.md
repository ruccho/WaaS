# Wukuro

Wukuro is an event scripting framework for Unity based on [WaaS](https://github.com/ruccho/WaaS). Events such as conversations, cutscenes and branches are written in Rust and run as WebAssembly components.

> [!NOTE]
> Wukuro is **very experimental** and breaking changes can be made.

## Documentation

See the [documentation](https://ruccho.com/WaaS/wukuro) ([日本語](https://ruccho.com/WaaS/ja/wukuro)) for the installation and usage.

## Overview

- Requires Unity 6000.0 or later, [UniTask](https://github.com/Cysharp/UniTask) and WaaS of the same version as Wukuro.
- Install the package with the git URL:

  ```
  https://github.com/ruccho/WaaS.git?path=/WaaS.Unity/Packages/com.ruccho.wukuro
  ```

- Rust scripts depend on the crates [`wukuro`](https://crates.io/crates/wukuro) and [`wukuro-dialog`](https://crates.io/crates/wukuro-dialog) of the same version as the package.
- A sample of a dialog implemented with IMGUI is available in the **Samples** of the Package Manager.

## License

[MIT](LICENSE.md)
