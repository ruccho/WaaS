<div align="center">

<img src="docs/static/img/social-t.svg">

<h1>WaaS</h1>

<p>
<strong>
Stands for <i>WebAssembly as a Script</i>, <br>a language-independent scripting engine for Unity and .NET.
</strong>
</p>

[![Releases](https://img.shields.io/github/release/ruccho/WaaS.svg)](https://github.com/ruccho/WaaS/releases)
[![NuGet Version](https://img.shields.io/nuget/v/WaaS.svg)](https://www.nuget.org/packages/WaaS)

</div>

### Language independent

Various languages that [support output to WebAssembly](https://github.com/appcypher/awesome-wasm-langs) such as Rust, Go, MoonBit and C/C++ can be used, allowing you to choose the language that best suits your needs.

### Component Model supported

WaaS supports the [Component Model](https://component-model.bytecodealliance.org/) and allows you to perform practical bindings.

### Safe

Without explicit permission, WebAssembly code cannot access the host environment's memory or functions.
You can also run untrusted scripts safely.

### IL2CPP / NativeAOT compatible

WaaS is an interpreter fully implemented in C# and does not require JIT or AOT compilation of WebAssembly itself. It can run anywhere Unity or .NET supports including platforms where JIT is prohibited, such as iOS.

## Use cases

### As a simple script in a game

It can be used as a script to describe events and AI in a game.
By creating scripts in small units, you can achieve fast hot reloads.

### As a user-created script

It can be used as a script to give complex behavior to user-created contents.

### As a plugin system

It can be used as a script to add plugins to applications created with Unity or .NET.

> [!NOTE]
> WaaS is currently **very experimental** and breaking changes can be made.

# Documentation

### See the [documentation](https://ruccho.com/WaaS) for installation and usage.