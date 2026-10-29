# Changelog

WaaS and its derived distributions (the NuGet packages, the Unity packages and the Rust crates) are released together with the same version.

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
