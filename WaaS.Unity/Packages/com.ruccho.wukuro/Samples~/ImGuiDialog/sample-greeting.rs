use wukuro::entry;
use wukuro::types::{Argument, Context};
use wukuro_dialog::functions::*;
use wukuro_dialog::*;

/// A speaker of the sample. The host receives its name as a string.
pub struct Guide;

impl DialogSpeaker for Guide {
    type FaceKind = GuideFace;

    fn get_name() -> String {
        "Guide".to_string()
    }
}

#[derive(Default)]
pub enum GuideFace {
    #[default]
    Normal,
    Smile,
}

impl DialogFace for GuideFace {
    fn get_name(&self) -> String {
        match self {
            GuideFace::Normal => "normal",
            GuideFace::Smile => "smile",
        }
        .to_string()
    }
}

#[entry]
fn greeting(cx: &Context, player_name: String) {
    let dialog = get_dialog_module(cx);
    let d = DialogSession::new(&dialog, None);

    // the variable is kept in the MemoryVariables of SampleEventRunner while the scene is playing
    let visits = match cx.get_variable("visits").get() {
        Some(Argument::TyS32(value)) => value,
        _ => 0,
    };
    cx.get_variable("visits").set(Argument::TyS32(visits + 1));

    d.msg::<Guide>(format!("Hello, {player_name}!"));
    d.show(MessageInfo::new::<Guide>("This message is shown by Wukuro.").face(&GuideFace::Smile));

    match d.opts(["Tell me more", "Bye"]) {
        0 => d.msg::<Guide>("Scripts are written in Rust and run as WebAssembly components."),
        _ => d.show(MessageInfo::new::<Guide>("See you!").auto(true)),
    }
}
