# WaaS Debugger

Debugs WebAssembly scripts running on [WaaS](https://github.com/ruccho/WaaS) in the Unity editor with breakpoints and steps.

1. In Unity, enable **Assets > Enable Debugger for WaaS**, and **Assets > Build Rust Scripts with Debug Information for WaaS** for Rust scripts.
2. In VS Code, install `waas-debug-<version>.vsix` attached to the [release](https://github.com/ruccho/WaaS/releases) with **Extensions: Install from VSIX...**, add the configuration **WaaS: Attach to Unity** to `launch.json`, and start debugging.

The debugger in the Unity editor listens on the port 4711 of the local host by default.
