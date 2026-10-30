#![allow(unused)]
// Entry points run by the tests of Wukuro (Editor/WukuroTests.cs).

use ::wukuro::entry;
use ::wukuro::types::{Action, Argument, Context, Object};
use ::wukuro::{l10n, ObjectOf, ObjectType};
use wukuro_dialog::functions::*;
use wukuro_dialog::*;

pub struct TestTransform;

impl ObjectType for TestTransform {
    const TYPE_NAME: &'static str = "UnityEngine.Transform, UnityEngine.CoreModule";
}

/// stores the arguments to variables after transforming them
#[entry]
fn store_arguments(cx: &Context, count: i32, label: String, flag: bool) {
    cx.get_variable("count").set(Argument::TyS32(count * 2));
    cx.get_variable("label").set(Argument::TyString(format!("{label}!")));
    cx.get_variable("flag").set(Argument::TyBool(!flag));
}

/// increments the variable `counter`, which persists across runs
#[entry]
fn increment(cx: &Context) {
    let variable = cx.get_variable("counter");
    let count = match variable.get() {
        Some(Argument::TyS32(value)) => value,
        _ => 0,
    };
    variable.set(Argument::TyS32(count + 1));
}

#[entry]
fn run_action(cx: &Context, callback: &Action) {
    callback.run();
}

/// panics with the message
#[entry]
fn panics(cx: &Context, message: String) {
    panic!("{message}");
}

#[entry]
fn typed_object(cx: &Context, target: &ObjectOf<TestTransform>, untyped: &Object) {}

#[entry]
fn talk(cx: &Context) {
    let dialog = get_dialog_module(cx);
    let d = DialogSession::new(&dialog, Some("test"));
    d.msg::<NoSpeaker>("hello");
    d.show(
        MessageInfo::new::<NoSpeaker>(l10n!("table", "key", "source"))
            .auto(true)
            .forbid_skip(true)
            .style("whisper"),
    );
    let chosen = d.opts(["first", "second"]);
    cx.get_variable("chosen").set(Argument::TyS32(chosen));
}

#[entry]
fn small_integers(cx: &Context, small: i16, unsigned_small: u16) {}
