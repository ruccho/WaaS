wit_bindgen::generate!({
    path: "wit",
    world: "abi-extended",
    additional_derives: [PartialEq, Clone],
    additional_derives_ignore: ["holder", "borrowing"],
});

use exports::waas::abi_extended::values as ex;
use std::cell::RefCell;
use std::fmt::Debug;
use waas::abi_extended::resources as res;
use waas::abi_extended::values as im;

thread_local! {
    static LAST_CALL: RefCell<String> = RefCell::new(String::new());
}

fn record(value: impl Debug) {
    LAST_CALL.with(|last| *last.borrow_mut() = format!("{value:?}"));
}

struct AbiExtended;

impl ex::Guest for AbiExtended {
    fn integers(
        a: u8,
        b: i8,
        c: u16,
        d: i16,
        e: u32,
        f: i32,
        g: u64,
        h: i64,
        i: bool,
    ) -> (u8, i8, u16, i16, u32, i32, u64, i64, bool) {
        let value = (a, b, c, d, e, f, g, h, i);
        record(value);
        value
    }

    fn small_integers(a: i8, b: i16, c: u8, d: u16) -> (i8, i16, u8, u16) {
        let value = (a, b, c, d);
        record(value);
        value
    }

    fn floats(a: f32, b: f64) -> (f32, f64) {
        record((a.to_bits(), b.to_bits()));
        (a, b)
    }

    fn chars(value: char) -> char {
        record(value as u32);
        value
    }

    fn text(value: String) -> String {
        record(value.chars().map(|c| c as u32).collect::<Vec<_>>());
        value
    }

    fn texts(values: Vec<String>) -> Vec<String> {
        if values.iter().map(|s| s.len()).sum::<usize>() <= 1024 {
            record(&values);
        } else {
            // large values are not formatted to keep the test fast
            record(values.iter().map(|s| s.len()).collect::<Vec<_>>());
        }
        values
    }

    fn bytes(values: Vec<u8>) -> Vec<u8> {
        if values.len() <= 1024 {
            record(&values);
        } else {
            record(values.len());
        }
        values
    }

    fn shorts(values: Vec<i16>) -> Vec<i16> {
        record(&values);
        values
    }

    fn outer_value(value: ex::Outer) -> ex::Outer {
        record(&value);
        value
    }

    fn trees(values: Vec<ex::Tree>) -> Vec<ex::Tree> {
        record(&values);
        values
    }

    fn flags_value(
        a: ex::SmallFlags,
        b: ex::MediumFlags,
        c: ex::LargeFlags,
    ) -> (ex::SmallFlags, ex::MediumFlags, ex::LargeFlags) {
        record((a.bits(), b.bits(), c.bits()));
        (a, b, c)
    }

    fn levels(values: Vec<ex::Level>) -> Vec<ex::Level> {
        record(&values);
        values
    }

    fn options(
        a: Option<u8>,
        b: Option<String>,
        c: Option<Option<i32>>,
    ) -> (Option<u8>, Option<String>, Option<Option<i32>>) {
        let value = (a, b, c);
        record(&value);
        value
    }

    fn results(
        a: Result<i8, u64>,
        b: Result<(), String>,
        c: Result<String, ()>,
        d: Result<(), ()>,
    ) -> (Result<i8, u64>, Result<(), String>, Result<String, ()>, Result<(), ()>) {
        let value = (a, b, c, d);
        record(&value);
        value
    }

    fn many_params(
        p0: i32,
        p1: i64,
        p2: f32,
        p3: f64,
        p4: u8,
        p5: i8,
        p6: u16,
        p7: i16,
        p8: String,
        p9: i32,
        p10: i32,
        p11: i32,
        p12: i32,
        p13: i32,
        p14: i32,
        p15: i32,
        p16: i32,
        p17: f64,
    ) -> String {
        let value = format_many_params(
            p0, p1, p2, p3, p4, p5, p6, p7, &p8, p9, p10, p11, p12, p13, p14, p15, p16, p17,
        );
        record(&value);
        value
    }

    fn long_tuple(
        value: (i32, i32, i32, i32, i32, i32, i32, u8, i64, String),
    ) -> (i32, i32, i32, i32, i32, i32, i32, u8, i64, String) {
        record(&value);
        value
    }
}

#[allow(clippy::too_many_arguments)]
fn format_many_params(
    p0: i32,
    p1: i64,
    p2: f32,
    p3: f64,
    p4: u8,
    p5: i8,
    p6: u16,
    p7: i16,
    p8: &str,
    p9: i32,
    p10: i32,
    p11: i32,
    p12: i32,
    p13: i32,
    p14: i32,
    p15: i32,
    p16: i32,
    p17: f64,
) -> String {
    format!(
        "{p0},{p1},{p2},{p3},{p4},{p5},{p6},{p7},{p8},{p9},{p10},{p11},{p12},{p13},{p14},{p15},{p16},{p17}"
    )
}

/// Collects the mismatches of the values sent to the host and returned from it.
struct Checker(Vec<String>);

impl Checker {
    fn check<T: PartialEq + Debug>(&mut self, name: &str, sent: T, received: T) {
        if sent != received {
            self.0
                .push(format!("{name}: sent {sent:?}, received {received:?}"));
        }
    }
}

fn inner(a: u8, b: Option<i16>, c: &[&str]) -> im::Inner {
    im::Inner {
        a,
        b,
        c: c.iter().map(|s| s.to_string()).collect(),
    }
}

fn test_values() -> Vec<String> {
    let mut checker = Checker(Vec::new());
    let c = &mut checker;

    for value in [
        (u8::MAX, i8::MIN, u16::MAX, i16::MIN, u32::MAX, i32::MIN, u64::MAX, i64::MIN, true),
        (0, -1, 0, -1, 0, -1, 0, -1, false),
        (0x80, i8::MAX, 0x8000, i16::MAX, 0x8000_0000, i32::MAX, 1 << 63, i64::MAX, true),
    ] {
        let (a, b, cc, d, e, f, g, h, i) = value;
        c.check("integers", value, im::integers(a, b, cc, d, e, f, g, h, i));
    }

    for value in [(-1i8, -1i16, u8::MAX, u16::MAX), (i8::MIN, i16::MIN, 0x80, 0x8000)] {
        c.check(
            "small-integers",
            value,
            im::small_integers(value.0, value.1, value.2, value.3),
        );
    }

    for (a, b) in [
        (0.0f32, -0.0f64),
        (-0.0, 0.0),
        (f32::INFINITY, f64::NEG_INFINITY),
        (f32::NEG_INFINITY, f64::INFINITY),
        (f32::from_bits(1), f64::from_bits(1)),
        (f32::MAX, f64::MIN),
        (f32::MIN_POSITIVE, f64::MAX),
        (f32::NAN, f64::NAN),
    ] {
        let (ra, rb) = im::floats(a, b);
        c.check(
            "floats",
            (a.to_bits(), b.to_bits()),
            (ra.to_bits(), rb.to_bits()),
        );
    }

    for value in ['a', '\u{E9}', '\0', '\u{D7FF}', '\u{E000}', '\u{1F600}', '\u{10FFFF}'] {
        c.check("chars", value, im::chars(value));
    }

    let long = "\u{3042}\u{1F600}".repeat(500);
    for value in [
        "",
        "hello",
        "\u{3053}\u{3093}\u{306B}\u{3061}\u{306F}",
        "\u{1F600}\u{1F44D}\u{1F3FD}",
        "a\0b",
        long.as_str(),
    ] {
        c.check("text", value.to_string(), im::text(value));
    }

    for value in [
        vec![],
        vec![
            "".to_string(),
            "a".to_string(),
            "\u{65E5}\u{672C}\u{8A9E}".to_string(),
            "\u{1F600}".to_string(),
        ],
    ] {
        c.check("texts", value.clone(), im::texts(&value));
    }

    let bytes = vec![0u8, 1, 127, 128, 255];
    c.check("bytes", bytes.clone(), im::bytes(&bytes));
    let shorts = vec![-1i16, i16::MIN, i16::MAX, 0, 1];
    c.check("shorts", shorts.clone(), im::shorts(&shorts));

    let outer = im::Outer {
        name: "outer\u{1F600}".to_string(),
        inner: inner(255, Some(-2), &["x", "yz"]),
        items: vec![inner(1, None, &[]), inner(2, Some(i16::MIN), &["\u{E9}"])],
        tree: im::Tree::Node(inner(3, Some(-1), &["leaf"])),
        maybe: Some(Err("error".to_string())),
        nested: vec![vec![], vec![-1, i64::MIN], vec![i64::MAX]],
    };
    c.check("outer-value", outer.clone(), im::outer_value(&outer));
    let outer = im::Outer {
        name: String::new(),
        inner: inner(0, None, &[]),
        items: vec![],
        tree: im::Tree::Empty,
        maybe: Some(Ok(u32::MAX)),
        nested: vec![],
    };
    c.check("outer-value", outer.clone(), im::outer_value(&outer));
    let outer = im::Outer {
        maybe: None,
        tree: im::Tree::Pair((u16::MAX, -1.5)),
        ..outer
    };
    c.check("outer-value", outer.clone(), im::outer_value(&outer));

    let trees = vec![
        im::Tree::Leaf(-1),
        im::Tree::Node(inner(4, Some(4), &["a", "b"])),
        im::Tree::Pair((0x8000, f64::MIN_POSITIVE)),
        im::Tree::Text("\u{30C6}\u{30AD}\u{30B9}\u{30C8}".to_string()),
        im::Tree::Empty,
        im::Tree::Leaf(i8::MIN),
    ];
    c.check("trees", trees.clone(), im::trees(&trees));

    for (a, b, cc) in [
        (
            im::SmallFlags::empty(),
            im::MediumFlags::empty(),
            im::LargeFlags::empty(),
        ),
        (
            im::SmallFlags::F0 | im::SmallFlags::F2,
            im::MediumFlags::F0 | im::MediumFlags::F11,
            im::LargeFlags::F0 | im::LargeFlags::F31,
        ),
        (
            im::SmallFlags::all(),
            im::MediumFlags::all(),
            im::LargeFlags::all(),
        ),
    ] {
        c.check("flags-value", (a, b, cc), im::flags_value(a, b, cc));
    }

    let levels = vec![im::Level::High, im::Level::Low, im::Level::Mid];
    c.check("levels", levels.clone(), im::levels(&levels));

    for (a, b, cc) in [
        (Some(255u8), Some("x".to_string()), Some(Some(-1))),
        (None, None, Some(None)),
        (Some(0), Some(String::new()), None),
    ] {
        c.check("options", (a, b.clone(), cc), im::options(a, b.as_deref(), cc));
    }

    for value in [
        (Ok(-1i8), Ok(()), Ok("ok".to_string()), Ok(())),
        (Err(u64::MAX), Err("error".to_string()), Err(()), Err(())),
    ] {
        let (a, b, cc, d) = value.clone();
        let b = b.as_ref().map(|_| ()).map_err(|e| e.as_str());
        let cc = cc.as_ref().map(|s| s.as_str()).map_err(|_| ());
        c.check("results", value, im::results(a, b, cc, d));
    }

    let expected = format_many_params(
        -1,
        i64::MIN,
        1.5,
        -2.25,
        255,
        -128,
        65535,
        -32768,
        "\u{65E5}\u{672C}",
        9,
        10,
        11,
        12,
        13,
        14,
        15,
        16,
        0.5,
    );
    let received = im::many_params(
        -1,
        i64::MIN,
        1.5,
        -2.25,
        255,
        -128,
        65535,
        -32768,
        "\u{65E5}\u{672C}",
        9,
        10,
        11,
        12,
        13,
        14,
        15,
        16,
        0.5,
    );
    c.check("many-params", expected, received);

    let value = (1, 2, 3, 4, 5, 6, 7, 0xFFu8, i64::MIN, "tail".to_string());
    let received = im::long_tuple((
        value.0, value.1, value.2, value.3, value.4, value.5, value.6, value.7, value.8, &value.9,
    ));
    c.check("long-tuple", value, received);

    checker.0
}

fn test_resources() -> String {
    let mut out = Vec::new();
    let a = res::make_blob(1);
    let blobs = res::make_blobs(&[2, 3, 4]);
    out.push(format!(
        "values={}",
        blobs.iter().map(|b| b.value()).sum::<i32>()
    ));
    out.push(format!(
        "borrowed={}",
        res::sum_borrowed(&[&a, &blobs[0], &blobs[2]])
    ));
    out.push(format!("taken={}", res::take_blobs(blobs)));
    let holder = res::swap_holder(res::Holder {
        name: "h".to_string(),
        blob: a,
        extra: Some(res::make_blob(5)),
    });
    out.push(format!(
        "holder={}:{}:{}",
        holder.name,
        holder.blob.value(),
        holder.extra.as_ref().map_or(-1, |b| b.value())
    ));
    out.push(format!(
        "multiply={}",
        res::multiply(&res::Borrowing {
            blob: &holder.blob,
            factor: 3
        })
    ));
    out.join(";")
}

impl Guest for AbiExtended {
    fn last_call() -> String {
        LAST_CALL.with(|last| last.borrow().clone())
    }

    fn test_values() -> Vec<String> {
        test_values()
    }

    fn test_resources() -> String {
        test_resources()
    }

    fn sum_owned(blobs: Vec<Blob>) -> i32 {
        blobs.iter().map(|b| b.value()).sum()
    }

}

export!(AbiExtended);
