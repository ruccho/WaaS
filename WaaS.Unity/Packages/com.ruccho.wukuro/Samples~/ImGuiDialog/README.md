# ImGuiDialog sample

A minimal sample that shows a conversation written in Rust with a dialog drawn with IMGUI.

- `sample-greeting.rs`: a script with the entry point `greeting`.
- `ImGuiDialogModule.cs`: an implementation of `IDialogModule` drawn with IMGUI.
- `SampleEventRunner.cs`: runs the entry point when the scene starts.

## Setup

1. Set the import settings of `sample-greeting.rs` and apply.
   - **Dependencies**: `wukuro`, `wukuro-dialog` and `wit-bindgen` via Registry, with the versions described in the documentation.
   - **Componentize**: enabled, with **WIT Directory** empty.
2. Create a GameObject in a scene and add `ImGuiDialogModule` and `SampleEventRunner`.
3. In `SampleEventRunner`, set `ImGuiDialogModule` to **Dialog**, set `sample-greeting.rs` to **Script** of **Entry**, select `greeting`, and enter **Player Name**.
4. Enter the play mode. Proceed the messages with a click or the Return key.

See the [documentation](https://ruccho.com/WaaS/wukuro) for details.
