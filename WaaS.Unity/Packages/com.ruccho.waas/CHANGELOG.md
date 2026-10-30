# Changelog

WaaS and its derived distributions (the NuGet packages, the Unity packages and the Rust crates) are released together with the same version.

## [Unreleased]

### Added

- WebAssembly features: multi-value, fixed-width SIMD, non-trapping float-to-int conversions and the data count section.
- Execution limits of `ExecutionContext`: `Fuel`, `FuelPerResume`, `MaxMemoryPages` and `CancellationToken`.
- WebAssembly stack traces of traps (`WasmStackTrace`), with the function names in the `name` section.
- MoonBit Importer, which imports `*.mbt` files as scripts, and **MoonBit Bindings Settings**, which generate MoonBit bindings of WIT interfaces.
- wit2waas: inline interfaces of worlds.
- `[ComponentField]` can be applied to public fields, so that a type serialized by Unity can be a record.
- Prebuilt wit2waas binaries for Windows, macOS and Linux, attached to each GitHub release.
- Rust Importer and MoonBit Importer reimport the scripts when the files outside the asset database that they were built from change, such as the sources of the crates referenced by the path, by checking the timestamps and the sizes of the files while the editor is active (`ExternalFileDependency.Track`).
- WebAssembly features: reference types (`funcref`, `externref`, the reference and table instructions and multiple tables) and the rest of bulk memory (passive data and element segments, `memory.init`, `data.drop`, `table.init`, `elem.drop` and `table.copy`). WaaS satisfies WebAssembly Core Specification 2.0.
- `StackValueItem.FromReference` and `StackValueItem.ExpectReference` to pass objects as reference values, and `Global.CreateReference` to create globals of reference types.

### Changed

- The stack value (`StackValueItem`) is 24 bytes to hold v128 values.
- The sub-opcodes of prefixed instructions are decoded as LEB128 (`OpCodeAttribute.OpCode1` is `uint?`).
- The messages of `TrapException` and `WasmException` include the WebAssembly stack trace.
- The formatters created by reflection are preserved from managed code stripping.
- The native editor library generates MoonBit bindings, and binds `spectest.print_char` (`println` of MoonBit) to a no-op when componentizing.
- Instantiating a component validates the labels of records, variants, enums and flags in the imports (ignoring the case and `-`/`_`), and the names and sorts of the imports and exports of imported components.
- `LinkException` on instantiation reports the path of the mismatched import with the expected and provided types.
- Calls of host functions from components are about twice as fast. `ExternalFunction.InvokeAsync` is called after all the arguments are pushed, and completes without suspension when the host function completes synchronously.
- The interpreter runs the function bodies translated at validation with a single dispatch loop and superinstructions, which is several times faster. The fuel is consumed at the start of each basic block. The control instructions (blocks and branches) cannot be executed with `Instruction.Execute` alone, and labels are no longer pushed to the stack.
- Calls between WebAssembly functions are executed within the `WasmStackFrame` of the caller without pushing frames to `ExecutionContext`. `WasmStackFrame.Function` is the innermost function being executed, and `WasmStackTrace` still lists each function.
- `Table<T>` is limited to `Table<IInvocableFunction>` (`funcref`) and `Table<object>` (`externref`), and tables can grow. `Table.Size` is the current number of the elements.
- The active data and element segments are copied in the order of the segments at instantiation, and the segments copied before an out-of-bounds segment remain in the memories and the tables (as defined in WebAssembly 2.0). Previously no segment was copied when a segment was out of bounds.
- The sections of modules must appear in the order defined by the specification.
- A mismatched import of a global throws `InvalidModuleException` instead of `ArgumentException`.
- MoonBit Importer places the build output in a short directory under the temporary directory on Windows, so that the MoonBit compiler does not fail with the long paths of a project in a deep directory.

### Fixed

- Comparing component types and instance types no longer throws `NotImplementedException`.
- A failure to resolve a type while validating an import is reported as a mismatch of the import instead of an empty `LinkException`.
- The layout of variants whose payloads are aligned to 1 byte or have no payload (such as `option<u8>` and enums) in the memory: the payload was aligned to 2 bytes.
- Lowering flags from C# `[Flags]` enums (passing flags to components) threw `InvalidOperationException`.
- C# tuples of more than 7 elements: the elements after the 7th were formatted as a nested tuple instead of the elements of the same tuple.
- Lowering a variant whose case is typed with a nullable type argument, such as `Option<int?>` holding `null` (`option<option<s32>>`), threw `InvalidOperationException`.
- Rust Importer removes the crate of a script when the script is deleted or **Crate Root** is unchecked. A crate left for a deleted script broke `cargo check` of the Cargo workspace at the root of the project, which IDEs such as rust-analyzer run, and a crate left for a script that is no longer a crate root made IDEs analyze the script as another crate.
- A Rust script that failed to compile was not reimported when the files it depends on changed, such as the WIT exported after the script was imported, because the dependencies were not recorded on the failure.
- Stack overflow when reading and validating deeply nested blocks, which rustc emits for large `match` expressions. The blocks are read and validated without recursion.
- Validation of `br_table` in unreachable code rejected labels of different types.
- Importing a memory compared the limits declared by the imported memory instead of its current size.
- `call_indirect` with an index out of the table threw `IndexOutOfRangeException` instead of trapping.
- Integer division by zero, the overflow of signed division and the invalid conversions from floating-point numbers to integers threw `InvalidCodeException` or `OverflowException` instead of `TrapException`, and `i32.div_s` and `i64.div_s` trapped when the minimum value was divided by 1.
- `memory.copy` and `memory.fill` threw `OverflowException` when the range exceeded 2^32, and `memory.grow` threw `OverflowException` for more than 2^31 pages.
- A memory larger than an array throws `NotSupportedException` at instantiation, and the memory cannot grow beyond `Memory.MaxNumPages`. Previously the length of the memory overflowed.
- Malformed modules threw exceptions other than `InvalidModuleException`, such as `InvalidOperationException` for unknown opcodes, and modules with duplicated export names or unknown indices in the exports and the start section failed at instantiation instead of loading.

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
