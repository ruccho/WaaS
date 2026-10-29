// Dialogs of Wukuro. The bindings of `wukuro:dialog` are generated into `bindings/`.

///|
pub using @types {type DialogModule, type MessageInfo}

///|
pub using @functions {get_dialog_module}

///|
/// A speaker of messages, implemented by the types of speakers.
pub(open) trait DialogSpeaker {
  name(Self) -> String
  default_face(Self) -> String
}

///|
/// A face of a speaker, implemented by the types of faces.
pub(open) trait DialogFace {
  name(Self) -> String
}

///|
pub impl DialogFace for String with name(self) {
  self
}

///|
/// A speaker for messages without a speaker, such as narrations and signs. Its name is empty.
pub(all) enum NoSpeaker {
  NoSpeaker
}

///|
pub impl DialogSpeaker for NoSpeaker with name(_) {
  ""
}

///|
pub impl DialogSpeaker for NoSpeaker with default_face(_) {
  ""
}

///|
/// The value of `NoSpeaker`.
pub let no_speaker : NoSpeaker = NoSpeaker

///|
/// A message to show. Create it with `message` and change the settings by the methods chained to it.
///
/// ```mbt nocheck
/// d.show(@dialog.message(@dialog.no_speaker, "Hello").forbid_skip(true))
/// ```
pub struct Message {
  speaker : String
  face : String
  text : @wukuro.Text
  auto : Bool
  forbid_skip : Bool
  forbid_accel : Bool
  styles : Array[String]
}

///|
/// Creates a message of the speaker `S` with its default face.
pub fn[S : DialogSpeaker, T : @wukuro.IntoText] message(speaker : S, text : T) -> Message {
  {
    speaker: speaker.name(),
    face: speaker.default_face(),
    text: text.into_text(),
    auto: false,
    forbid_skip: false,
    forbid_accel: false,
    styles: [],
  }
}

///|
pub fn[F : DialogFace] Message::face(self : Message, face : F) -> Message {
  { ..self, face: face.name() }
}

///|
/// Whether the message proceeds automatically without input.
pub fn Message::auto(self : Message, auto : Bool) -> Message {
  { ..self, auto, }
}

///|
/// Whether the player cannot skip the message.
pub fn Message::forbid_skip(self : Message, forbid_skip : Bool) -> Message {
  { ..self, forbid_skip, }
}

///|
/// Whether the player cannot accelerate the message.
pub fn Message::forbid_accel(self : Message, forbid_accel : Bool) -> Message {
  { ..self, forbid_accel, }
}

///|
/// Adds a host-specific display style of the message.
pub fn Message::style(self : Message, style : String) -> Message {
  { ..self, styles: [..self.styles, style] }
}

///|
pub fn Message::to_info(self : Message) -> MessageInfo {
  {
    speaker: self.speaker,
    face: self.face,
    text: self.text,
    auto: self.auto,
    forbid_skip: self.forbid_skip,
    forbid_accel: self.forbid_accel,
    styles: self.styles,
  }
}

///|
/// An open dialog, passed to the function of `dialog`.
pub struct DialogSession {
  priv module : DialogModule
}

///|
/// Opens a dialog, runs the function and closes the dialog.
pub fn[R] dialog(module : DialogModule, provider? : String, f : (DialogSession) -> R) -> R {
  module.open_dialog(provider)
  let result = f({ module, })
  module.close_dialog()
  result
}

///|
/// Shows a message and waits until the player proceeds.
pub fn DialogSession::show(self : DialogSession, message : Message) -> Unit {
  self.module.show_message(message.to_info())
}

///|
/// Shows a message of the speaker `S` with its default face. The same as `show(message(speaker, text))`.
pub fn[S : DialogSpeaker, T : @wukuro.IntoText] DialogSession::msg(
  self : DialogSession,
  speaker : S,
  text : T,
) -> Unit {
  self.show(message(speaker, text))
}

///|
/// Shows options and waits until the player chooses one. Returns the index of the chosen option.
pub fn[T : @wukuro.IntoText] DialogSession::opts(self : DialogSession, options : Array[T]) -> Int {
  self.module.show_options(options.map(fn(o) { o.into_text() }))
}

///|
/// Fades in, runs the function and fades out.
pub fn[R] fade(
  module : DialogModule,
  duration_in? : Float = 0.2,
  duration_out? : Float = 0.2,
  f : () -> R,
) -> R {
  module.fade_in(duration_in)
  let result = f()
  module.fade_out(duration_out)
  result
}
