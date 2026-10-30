// Guest library of Wukuro for MoonBit. The bindings of `wukuro:core` are generated into `bindings/`.

///|
pub using @types {
  type Context,
  type Variable,
  type Action,
  type Task,
  type Object,
  type Argument,
  type Text,
  type TextId,
  type UpdateModeKind,
}

///|
pub using @functions {log, delay, delay_frame, schedule}

///|
/// A value that can be shown to the player as a text: a string written directly in the script, or a localized text
/// created by `l10n`.
pub trait IntoText {
  into_text(Self) -> Text
}

///|
pub impl IntoText for Text with into_text(self) {
  self
}

///|
pub impl IntoText for String with into_text(self) {
  Text::Plain(self)
}

///|
/// Creates a reference to a localized text from a table name and a key.
///
/// `source` is the source text at the time of localization. It is not used at runtime and is kept only to make the
/// script readable.
pub fn l10n(table : String, key : String, source? : String) -> Text {
  ignore(source)
  Text::Localized(TextId::{ table, key })
}

///|
/// Stops the script with the message, which is reported to the host.
///
/// Use this instead of `abort` and `panic` of MoonBit, whose messages are not reported.
pub fn[T] fail(message : String) -> T {
  @functions.panic(message)
  abort(message)
}

///|
/// An object of the type `T`. Use `ObjectOf[T]` as a parameter of an entry point to let the inspector accept only
/// objects of the type. `T` is a marker type with the attribute `#wukuro.object_type("<C# type name>")`.
pub struct ObjectOf[T] {
  object : Object
}

///|
#doc(hidden)
pub fn[T] ObjectOf::from_object(object : Object) -> ObjectOf[T] {
  { object, }
}

///|
/// The object without its type.
pub fn[T] ObjectOf::object(self : ObjectOf[T]) -> Object {
  self.object
}
