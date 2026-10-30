# Changelog

WaaS and its derived distributions (the NuGet packages, the Unity packages and the Rust crates) are released together with the same version.

## [Unreleased]

### Added

- WebAssembly features: multi-value, fixed-width SIMD, non-trapping float-to-int conversions and the data count section.
- Execution limits of `ExecutionContext`: `Fuel`, `FuelPerResume`, `MaxMemoryPages` and `CancellationToken`.
- WebAssembly stack traces of traps (`WasmStackTrace`), with the function names in the `name` section.
- MoonBit Importer, which imports `*.mbt` files as scripts, and **MoonBit Bindings Settings**, which generate MoonBit bindings of WIT interfaces.
- wit2waas: inline interfaces of worlds.

### Changed

- The stack value (`StackValueItem`) is 24 bytes to hold v128 values.
- The sub-opcodes of prefixed instructions are decoded as LEB128 (`OpCodeAttribute.OpCode1` is `uint?`).
- The messages of `TrapException` and `WasmException` include the WebAssembly stack trace.
- The formatters created by reflection are preserved from managed code stripping.
- The native editor library generates MoonBit bindings, and binds `spectest.print_char` (`println` of MoonBit) to a no-op when componentizing.
- Instantiating a component validates the labels of records, variants, enums and flags in the imports (ignoring the case and `-`/`_`), and the names and sorts of the imports and exports of imported components.
- `LinkException` on instantiation reports the path of the mismatched import with the expected and provided types.

### Fixed

- Comparing component types and instance types no longer throws `NotImplementedException`.
- A failure to resolve a type while validating an import is reported as a mismatch of the import instead of an empty `LinkException`.

## [0.3.0]

### Added

- Generate the WIT definitions of the C# interfaces declared with `[ComponentInterface]`, and export them as WIT files in Unity with **WIT Export Settings**.
- `ComponentAsset.GetCustomSections` to read the custom sections of components and the core modules in them.
- Rust Importer: dependencies **via Workspace**, and tracking of files outside `Assets` and `Packages` (such as exported WIT files) as dependencies of Rust scripts.
- Rust Importer: componentization without embedding WIT metadata, for modules that already carry their component type (such as modules built with wit-bindgen).
- Record the type of components in WIT when importing.

### Changed

- Unity 6000.0 or later is required.
- Resource handles are managed with a handle table per component instance.
- The capacity of linear memory grows geometrically.
- The native editor library is built with wit-component 0.262.0.

### Fixed

- The layout of variants in the Canonical ABI.
- The lifetimes of the handles of host resources.
- Lowering the results of host functions into the memory of components.
- Native stack exhaustion when passing call results between frames.
- The generation of types for results and names with multiple words.
- Crates of deleted Rust scripts broke the builds of the other scripts.
