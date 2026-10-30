# Changelog

Wukuro is released together with WaaS with the same version. The Unity package and the crates `wukuro`, `wukuro-macro` and `wukuro-dialog` of the same version are used together.

## [Unreleased]

## [0.4.0] - 2026-10-31

### Breaking Changes

- Rust scripts built with the crates of this version import `wukuro:core/functions.panic`, so they require the Unity package of the same version. Use the same version for the Unity package and the crates (or the MoonBit module).

### Added

- Scripts in MoonBit, with the MoonBit module `ruccho/wukuro` published on mooncakes.io and the generator of entry points for MoonBit Importer (**Wukuro Entry Points**).
- Records and enums defined in C# (`[ComponentRecord]` structs and enums in a `[ComponentInterface]`) can be parameters of entry points. They are edited in the inspector and passed on to host functions taking the same C# types, and `ObjectOf<T>` in them references Unity objects. Values are given at runtime with `WukuroRuntimeArguments.AddValue`.
- Panics of Rust scripts and `abort` of MoonBit scripts are reported to the host as `WukuroPanicException` with the message.
- Support for running scripts in tests: `WukuroEntryPoints.CreateDefaultArguments` creates the arguments of an entry point with the default values, and `WukuroClock.Immediate` completes the waits of the scripts on the next frame.
- Scripts can be edited in play mode. The entry points started after the reimport run the new script.

### Changed

- Rust scripts no longer need `wit-bindgen` in their dependencies. It can be removed from the import settings and the presets.

## [0.3.0]

- The first release.
