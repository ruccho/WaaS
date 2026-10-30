# Changelog

Wukuro is released together with WaaS with the same version. The Unity package and the crates `wukuro`, `wukuro-macro` and `wukuro-dialog` of the same version are used together.

## [Unreleased]

- Scripts in MoonBit, with the MoonBit module `ruccho/wukuro` (`Wukuro/moonbit/wukuro`) and the generator of entry points for MoonBit Importer.
- Panics of Rust scripts are reported to the host as `WukuroPanicException` with the message. Scripts built with this version of the crates import `wukuro:core/functions.panic`, so they require the Unity package of the same version.

## [0.3.0]

- The first release.
