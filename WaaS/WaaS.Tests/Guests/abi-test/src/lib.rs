wit_bindgen::generate!({
    path: "wit",
    world: "abi-test",
});

use exports::waas::abi_test::guest_types;
use std::cell::Cell;
use waas::abi_test::host::{self, Color, Mixed, Permissions, Shape, Tagged};

static mut DESTROYED: u32 = 0;

struct Accumulator {
    total: Cell<i32>,
}

impl Drop for Accumulator {
    fn drop(&mut self) {
        unsafe { DESTROYED += 1 };
    }
}

impl guest_types::GuestAccumulator for Accumulator {
    fn new(start: i32) -> Self {
        Accumulator { total: Cell::new(start) }
    }

    fn add(&self, value: i32) {
        self.total.set(self.total.get() + value);
    }

    fn total(&self) -> i32 {
        self.total.get()
    }
}

impl guest_types::Guest for AbiTest {
    type Accumulator = Accumulator;

    fn destroyed_count() -> u32 {
        unsafe { DESTROYED }
    }

    fn consume(accumulator: guest_types::Accumulator) -> i32 {
        accumulator.get::<Accumulator>().total.get()
    }
}

struct AbiTest;

impl Guest for AbiTest {
    fn test_resource() -> i32 {
        let counter = host::make_counter(10);
        counter.increment();
        counter.increment();
        // pass the owned resource as a borrow
        let value = host::read_counter(&counter);
        // the counter is dropped here
        value + counter.get()
    }

    fn test_borrow_param(counter: &Counter) -> i32 {
        counter.increment();
        counter.get()
    }

    fn test_string(value: String) -> String {
        let echoed = host::echo(&value);
        format!("{echoed}|{}", echoed.chars().count())
    }

    fn test_list() -> i64 {
        let values: Vec<i32> = (1..=100).collect();
        host::sum(&values)
    }

    fn test_option() -> i32 {
        let some = host::increment_optional(Some(41)).unwrap_or(-1);
        let none = host::increment_optional(None).map_or(1000, |_| -1000);
        some + none
    }

    fn test_result() -> String {
        let ok = match host::checked_negate(5) {
            Ok(value) => value.to_string(),
            Err(message) => format!("unexpected error: {message}"),
        };
        let err = match host::checked_negate(i32::MIN) {
            Ok(value) => format!("unexpected ok: {value}"),
            Err(message) => message,
        };
        format!("{ok}/{err}")
    }

    fn test_record(value: Point) -> Point {
        Point {
            x: value.x * 2,
            y_coord: value.y_coord + 0.5,
        }
    }

    fn test_variant() -> f32 {
        host::shape_area(Shape::Circle(1.0))
            + host::shape_area(Shape::Rect(Point { x: 2, y_coord: 3.0 }))
            + host::shape_area(Shape::Empty)
    }

    fn test_enum_flags() -> String {
        let a = host::describe(Color::Red, Permissions::READ);
        let b = host::describe(Color::GreenBlue, Permissions::READ | Permissions::WRITE);
        format!("{a};{b}")
    }

    fn test_async() -> u32 {
        // each call suspends the guest until the host completes
        host::wait_ticks(3) + host::wait_ticks(4)
    }

    fn test_variant_result() -> String {
        (0..4)
            .map(|kind| match host::make_shape(kind) {
                Some(Shape::Circle(radius)) => format!("circle({radius})"),
                Some(Shape::Rect(point)) => format!("rect({},{})", point.x, point.y_coord),
                Some(Shape::Empty) => "empty".to_string(),
                None => "none".to_string(),
            })
            .collect::<Vec<_>>()
            .join(";")
    }

    fn test_mixed_variant() -> String {
        // host -> guest
        let lowered = (0..4)
            .map(|kind| {
                let tagged = host::make_tagged(kind);
                let value = match tagged.value {
                    Mixed::Small(v) => format!("small({v})"),
                    Mixed::Big(v) => format!("big({v})"),
                    Mixed::Text(v) => format!("text({v})"),
                    Mixed::Nothing => "nothing".to_string(),
                };
                format!("{value}+{}", tagged.after)
            })
            .collect::<Vec<_>>()
            .join(";");
        // guest -> host
        let lifted = [
            Mixed::Small(-7),
            Mixed::Big(1 << 40),
            Mixed::Text("abc".to_string()),
            Mixed::Nothing,
        ]
        .into_iter()
        .enumerate()
        .map(|(i, value)| host::describe_tagged(&Tagged { value, after: i as i32 * 10 }))
        .collect::<Vec<_>>()
        .join(";");
        format!("{lowered}|{lifted}")
    }

    fn test_forged_handle(handle: u32) -> i32 {
        let counter = unsafe { Counter::from_handle(handle) };
        let value = counter.get();
        std::mem::forget(counter);
        value
    }

    fn test_leak(value: i32) {
        std::mem::forget(host::make_counter(value));
    }

    fn test_panic(message: String) {
        panic!("{message}");
    }

    fn test_host_failure(message: String) {
        host::fail(&message);
    }
}

export!(AbiTest);
