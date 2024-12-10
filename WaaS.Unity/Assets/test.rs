use crate::my_game::my_sequencer::env::show_message;

wit_bindgen::generate!({
    path: "../../../../../wit",
    world: "my-game:my-sequencer/sequence"
});

struct Sequence;

impl Guest for Sequence {
    fn play() {
        show_message("ぼく", "こんにちは！");
    }
}

export!(Sequence);