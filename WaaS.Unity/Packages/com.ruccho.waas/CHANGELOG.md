# Changelog

WaaS and its derived distributions (the NuGet packages, the Unity packages, the Rust crates, the MoonBit module and the extension of VS Code) are released together with the same version.

## [Unreleased]

## [0.4.0] - 2026-10-31

### Breaking Changes

- `StackValueItem` is 24 bytes to hold v128 values. Code depending on its size or layout must be updated.
- `Table<T>` can only be `Table<IInvocableFunction>` (`funcref`) or `Table<object>` (`externref`).
- Some failures throw other exceptions, so that the traps of WebAssembly are always `TrapException` (usually wrapped in `WasmException`) and malformed modules are always `InvalidModuleException`:
  - Integer division by zero, the overflow of signed division, invalid conversions from floating-point numbers to integers and exceeding `MaxStackFrames` throw `TrapException`. Previously they threw `InvalidCodeException`, `OverflowException` or `InvalidOperationException`.
  - Malformed modules throw `InvalidModuleException` instead of exceptions such as `InvalidOperationException` and `ArgumentOutOfRangeException`, and a mismatched import of a global throws `InvalidModuleException` instead of `ArgumentException`.
  - A memory larger than an array throws `NotSupportedException` at instantiation.
- Modules and components are validated more strictly: the order of the sections, the exports and the start section of modules, and the labels of records, variants, enums and flags and the names and sorts of the imports and exports of imported components. Modules or components that were accepted by mistake may be rejected.
- The active data and element segments are copied in order at instantiation, and the segments copied before an out-of-bounds segment remain, as defined in WebAssembly 2.0. Previously nothing was copied when a segment was out of bounds.
- The control instructions (blocks and branches) cannot be executed with `Instruction.Execute` alone, and labels are no longer pushed to the stack.
- Calls between WebAssembly functions no longer push frames to `ExecutionContext`. `WasmStackFrame.Function` is the innermost function being executed.
- `OpCodeAttribute.OpCode1` is `uint?`, because the sub-opcodes of prefixed instructions are LEB128.
- `IConstantExpression` has a new method for the references to functions.
- The scripts of Rust Importer and MoonBit Importer are reimported once after the update.

### Added

- WebAssembly features: multi-value, fixed-width SIMD (except relaxed SIMD), non-trapping float-to-int conversions, the data count section, reference types (`funcref`, `externref`, the reference and table instructions and multiple tables) and the rest of bulk memory. WaaS satisfies WebAssembly Core Specification 2.0.
  - Objects of the host are passed as reference values with `StackValueItem.FromReference` and read with `StackValueItem.ExpectReference`. `Global.CreateReference` creates globals of reference types.
- Execution limits of `ExecutionContext`: `Fuel`, `FuelPerResume`, `MaxMemoryPages` and `CancellationToken`.
- Stack traces of WebAssembly on traps (`WasmStackTrace`), with the function names in the `name` section. The messages of `TrapException` and `WasmException` include them.
- A debugger of the scripts in the Unity editor through the Debug Adapter Protocol, with breakpoints, steps, pausing, the stack frames and the values of WebAssembly ([Debugging Scripts](https://ruccho.com/WaaS/unity-integration/debugging)).
  - **Assets > Enable Debugger for WaaS** starts the debugger in the editor, and **Assets > Build Rust Scripts with Debug Information for WaaS** builds the Rust scripts with the line tables of DWARF.
  - The extension of VS Code (`waas-debug-*.vsix`) is attached to the release.
  - `ExecutionContext.Debugger` (`IWasmDebugger`) and `DebugAdapterServer` can also be used outside Unity, and `Module.LineTable` reads the line tables of DWARF.
- MoonBit Importer, which imports `*.mbt` files and directories with `moon.pkg` as scripts, and **MoonBit Bindings Settings**, which generate MoonBit bindings of WIT interfaces.
- Rust Importer and MoonBit Importer reimport the scripts when the files outside the asset database that they were built from change, such as the sources of the crates referenced by the path. The files are checked while the editor is active if Auto Refresh is enabled, or with **Assets > Reimport Scripts with Changed Files for WaaS**.
- `[ComponentField]` can be applied to public fields, so that a type serialized by Unity can be a record.
- wit2waas: inline interfaces of worlds, and prebuilt binaries for Windows, macOS and Linux attached to each release.

### Changed

- The interpreter is several times faster, and calls of host functions from components are about twice as fast. `ExternalFunction.InvokeAsync` completes without suspension when the host function completes synchronously.
- `LinkException` on instantiation reports the path of the mismatched import with the expected and the provided types.
- The formatters created by reflection are preserved from managed code stripping.
- MoonBit Importer builds the scripts in a short directory under the temporary directory on Windows, so that the MoonBit compiler does not fail with the long paths of a project in a deep directory.

### Fixed

- Comparing component types and instance types threw `NotImplementedException`.
- A failure to resolve a type while validating an import was reported as an empty `LinkException`.
- The layout of variants whose payloads are aligned to 1 byte or have no payload (such as `option<u8>` and enums) in the memory.
- Lowering flags from C# `[Flags]` enums threw `InvalidOperationException`.
- C# tuples of more than 7 elements were formatted as nested tuples.
- Lowering a variant whose case is typed with a nullable type argument, such as `Option<int?>` holding `null`, threw `InvalidOperationException`.
- Rust Importer left the crate of a script when the script was deleted or **Crate Root** was unchecked, which broke `cargo check` of the Cargo workspace run by IDEs such as rust-analyzer.
- A Rust script that failed to compile was not reimported when the files it depends on changed, such as the WIT exported after the script was imported.
- `ComponentAsset.LoadComponentAsync` threw `NullReferenceException` when the asset was reimported while loading, such as when a script was edited in play mode.
- Stack overflow when reading and validating deeply nested blocks, which rustc emits for large `match` expressions.
- Validation of `br_table` in unreachable code rejected labels of different types.
- Importing a memory compared the limits declared by the imported memory instead of its current size.
- `call_indirect` with an index out of the table threw `IndexOutOfRangeException` instead of trapping.
- `i32.div_s` and `i64.div_s` trapped when the minimum value was divided by 1.
- `memory.copy`, `memory.fill` and `memory.grow` threw `OverflowException` for large operands, and the length of a memory larger than an array overflowed.
- Modules with duplicated export names or unknown indices in the exports and the start section failed at instantiation instead of loading.

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
