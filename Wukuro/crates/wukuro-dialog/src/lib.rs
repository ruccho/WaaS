



pub use bindings::wukuro::dialog::{functions, types};

// the generated module `wukuro` is kept private so that `use wukuro_dialog::*` does not shadow the `wukuro` crate
mod bindings {
    wit_bindgen::generate!({
        inline: r#"
            package wukuro:dialog-inline;

            world imports {
                import wukuro:dialog/functions;
                import wukuro:dialog/types;
            }
        "#,
        path: ["wit/wukuro-core", "wit/wukuro-dialog"],
        world: "wukuro:dialog-inline/imports",
        with: {
            "wukuro:core/types": ::wukuro::wukuro::core::types,
            "wukuro:dialog/types": generate,
            "wukuro:dialog/functions": generate,
        },
    });
}


pub use ::wukuro::{IntoText, l10n};
pub use types::MessageInfo;

impl MessageInfo {
    /// Creates a message of the speaker `T` with its default face. The other settings can be changed by the methods
    /// chained to it, such as [`MessageInfo::face`].
    ///
    /// ```ignore
    /// d.show(MessageInfo::new::<Alice>("Hello").face(&AliceFace::Smile).forbid_skip(true));
    /// ```
    pub fn new<T: DialogSpeaker>(text: impl IntoText) -> Self {
        MessageInfo {
            speaker: T::get_name(),
            face: T::FaceKind::default().get_name(),
            text: text.into_text(),
            auto: false,
            forbid_skip: false,
            forbid_accel: false,
            styles: Vec::new(),
        }
    }

    pub fn face(mut self, face: &impl DialogFace) -> Self {
        self.face = face.get_name();
        self
    }

    /// Whether the message proceeds automatically without input.
    pub fn auto(mut self, auto: bool) -> Self {
        self.auto = auto;
        self
    }

    /// Whether the player cannot skip the message.
    pub fn forbid_skip(mut self, forbid_skip: bool) -> Self {
        self.forbid_skip = forbid_skip;
        self
    }

    /// Whether the player cannot accelerate the message.
    pub fn forbid_accel(mut self, forbid_accel: bool) -> Self {
        self.forbid_accel = forbid_accel;
        self
    }

    /// Adds a host-specific display style of the message.
    pub fn style(mut self, style: &str) -> Self {
        self.styles.push(style.to_string());
        self
    }
}

pub struct DialogSession<'a> {
    module: &'a types::DialogModule,
}

impl<'a> DialogSession<'a> {
    pub fn new(module: &'a types::DialogModule, provider: Option<&str>) -> Self {
        module.open_dialog(provider);
        DialogSession { module }
    }

    /// Shows a message and waits until the player proceeds.
    pub fn show(&self, message: MessageInfo) {
        self.module.show_message(&message);
    }

    /// Shows a message of the speaker `T` with its default face. The same as
    /// `show(MessageInfo::new::<T>(text))`.
    pub fn msg<T: DialogSpeaker>(&self, text: impl IntoText) {
        self.show(MessageInfo::new::<T>(text));
    }

    pub fn opts<I>(&self, options: I) -> i32
    where
        I: IntoIterator,
        I::Item: IntoText,
    {
        let a: Vec<types::Text> = options.into_iter().map(IntoText::into_text).collect();
        self.module.show_options(&a)
    }
}

impl Drop for DialogSession<'_> {
    fn drop(&mut self) {
        self.module.close_dialog();
    }
}

pub trait DialogSpeaker {
    type FaceKind: DialogFace + Default;
    fn get_name() -> String;
}

pub trait DialogFace {
    fn get_name(&self) -> String;
}

/// A speaker for messages without a speaker, such as narrations and signs. Its name is empty.
pub struct NoSpeaker;

impl DialogSpeaker for NoSpeaker {
    type FaceKind = NoSpeakerFace;

    fn get_name() -> String {
        String::new()
    }
}

#[derive(Debug, Default, Clone, Copy, PartialEq, Eq)]
pub enum NoSpeakerFace {
    #[default]
    Default,
}

impl DialogFace for NoSpeakerFace {
    fn get_name(&self) -> String {
        String::new()
    }
}


pub struct FadeSession<'a> {
    module: &'a types::DialogModule,
    dur_out: f32,
}

impl<'a> FadeSession<'a> {
    pub fn new(module: &'a types::DialogModule, dur_in: f32, dur_out: f32) -> Self {
        module.fade_in(dur_in);
        FadeSession { module, dur_out }
    }
    pub fn new_default(module: &'a types::DialogModule) -> Self {
        module.fade_in(0.2);
        FadeSession { module, dur_out: 0.2 }
    }
}

impl Drop for FadeSession<'_> {
    fn drop(&mut self) {
        self.module.fade_out(self.dur_out);
    }
}
