<div align="center">

<img src="docs/static/img/social-t.svg">

<h1>WaaS</h1>

<strong>
Stands for <i>WebAssembly as a Script</i>, <br>a language-independent scripting engine for Unity and .NET.
</strong>

<br>

[![Releases](https://img.shields.io/github/release/ruccho/WaaS.svg)](https://github.com/ruccho/WaaS/releases)

</div>

### Features

#### Language-independent

WaaS is an interpreter of [WebAssembly](https://webassembly.org/), which is a portable binary instruction format. [Find your favorite language](https://github.com/appcypher/awesome-wasm-langs) that supports WebAssembly output.

#### AOT safe

WaaS engine doesn't emit any native code at runtime. We can load and run wasm modules dynamically on AOT platform.

#### Coroutines

WaaS can import external asynchronous functions into a wasm module as synchronous functions. We can write asynchronous procedures with blocking style.

#### Interoperability

WaaS supports [WebAssembly Component Model](https://component-model.bytecodealliance.org/).  
Component Model allows C#-WASM FFI with rich type expressions such as records, lists and resources. 

> [!NOTE]
> WaaS is currently **very experimental** and breaking changes can be made.

## Documentation

[Official Documentation is here.](https://ruccho.com/WaaS)

## LICENSE

MIT

