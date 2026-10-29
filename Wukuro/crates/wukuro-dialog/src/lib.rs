



pub use crate::wukuro::dialog::{types, functions};

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


/// A value that can be shown to the player as a text: a string written directly in the script, or a localized text
/// created by [`l10n!`].
pub trait IntoText {
    fn into_text(self) -> types::Text;
}

impl IntoText for types::Text {
    fn into_text(self) -> types::Text {
        self
    }
}

impl IntoText for &types::Text {
    fn into_text(self) -> types::Text {
        self.clone()
    }
}

impl IntoText for &str {
    fn into_text(self) -> types::Text {
        types::Text::Plain(self.to_string())
    }
}

impl IntoText for &&str {
    fn into_text(self) -> types::Text {
        types::Text::Plain(self.to_string())
    }
}

impl IntoText for String {
    fn into_text(self) -> types::Text {
        types::Text::Plain(self)
    }
}

impl IntoText for &String {
    fn into_text(self) -> types::Text {
        types::Text::Plain(self.clone())
    }
}

/// Creates a reference to a localized text from a table name and a key.
///
/// The optional third argument is the source text at the time of localization. It is not used at runtime and is kept
/// only to make the script readable.
#[macro_export]
macro_rules! l10n {
    ($table:expr, $key:expr $(, $source:expr)? $(,)?) => {
        $crate::types::Text::Localized($crate::types::TextId {
            table: ::std::string::ToString::to_string($table),
            key: ::std::string::ToString::to_string($key),
        })
    };
}

pub struct DialogSession<'a> {
    module: &'a types::DialogModule,
}

impl<'a> DialogSession<'a> {
    pub fn new(module: &'a types::DialogModule, provider: Option<&str>) -> Self {
        module.open_dialog(provider);
        DialogSession { module }
    }

    pub fn msg_full(&self, message: &types::MessageInfo) {
        self.module.show_message(message);
    }

    pub fn msg<T: DialogSpeaker>(&self, text: impl IntoText) {
        self.module.show_message(&types::MessageInfo {
            text: text.into_text(),
            speaker: T::get_name(),
            face: T::FaceKind::default().get_name(),
            auto: false,
            forbid_skip: false,
            forbid_accel: false,
            styles: Vec::new(),
        });
    }

    pub fn msg_face<T: DialogSpeaker>(&self, text: impl IntoText, face: &T::FaceKind) {
        self.module.show_message(&types::MessageInfo {
            text: text.into_text(),
            speaker: T::get_name(),
            face: face.get_name(),
            auto: false,
            forbid_skip: false,
            forbid_accel: false,
            styles: Vec::new(),
        });
    }

    /// Shows a message with host-specific display styles.
    pub fn msg_styled<T: DialogSpeaker>(&self, text: impl IntoText, face: &T::FaceKind, styles: &[&str]) {
        self.module.show_message(&types::MessageInfo {
            text: text.into_text(),
            speaker: T::get_name(),
            face: face.get_name(),
            auto: false,
            forbid_skip: false,
            forbid_accel: false,
            styles: styles.iter().map(|s| s.to_string()).collect(),
        });
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
