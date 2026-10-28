wit_bindgen::generate!({
    path: "wit",
    world: "abi-test",
});

use waas::abi_test::host::{self, Color, Permissions, Shape};

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
}

export!(AbiTest);
