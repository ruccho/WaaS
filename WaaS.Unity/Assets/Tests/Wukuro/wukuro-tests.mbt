// Entry points run by the tests of Wukuro (Editor/WukuroTests.cs). The same as wukuro-tests.rs.

///|
#wukuro.object_type("UnityEngine.Transform, UnityEngine.CoreModule")
pub struct TestTransform {}

///|
/// stores the arguments to variables after transforming them
#wukuro.entry
pub fn store_arguments(
  cx : @wukuro.Context,
  count : Int,
  label : String,
  flag : Bool,
) -> Unit {
  cx.get_variable("count").set(TyS32(count * 2))
  cx.get_variable("label").set(TyString("\{label}!"))
  cx.get_variable("flag").set(TyBool(!flag))
}

///|
/// increments the variable `counter`, which persists across runs
#wukuro.entry
pub fn increment(cx : @wukuro.Context) -> Unit {
  let variable = cx.get_variable("counter")
  let count = match variable.get() {
    Some(TyS32(value)) => value
    _ => 0
  }
  variable.set(TyS32(count + 1))
}

///|
#wukuro.entry
pub fn run_action(_cx : @wukuro.Context, callback : @wukuro.Action) -> Unit {
  callback.run()
}

///|
/// fails with the message
#wukuro.entry
pub fn panics(_cx : @wukuro.Context, message : String) -> Unit {
  @wukuro.fail(message)
}

///|
#wukuro.entry
pub fn typed_object(
  _cx : @wukuro.Context,
  _target : @wukuro.ObjectOf[TestTransform],
  _untyped : @wukuro.Object,
) -> Unit {

}

///|
#wukuro.entry
pub fn talk(cx : @wukuro.Context) -> Unit {
  let module = @dialog.get_dialog_module(cx)
  let chosen = @dialog.dialog(module, provider="test", fn(d) {
    d.msg(@dialog.no_speaker, "hello")
    d.show(
      @dialog.message(
        @dialog.no_speaker,
        @wukuro.l10n("table", "key", source="source"),
      )
      .auto(true)
      .forbid_skip(true)
      .style("whisper"),
    )
    d.opts(["first", "second"])
  })
  cx.get_variable("chosen").set(TyS32(chosen))
}

///|
#wukuro.entry
pub fn small_integers(
  _cx : @wukuro.Context,
  _small : Int16,
  _unsigned_small : UInt16,
) -> Unit {

}
