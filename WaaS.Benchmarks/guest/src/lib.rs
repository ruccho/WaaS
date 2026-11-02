//! The workloads as a component that also calls the host.

wit_bindgen::generate!({
    path: "wit",
    world: "benchmark",
});

mod workloads;

use waas::benchmark::host;

struct Benchmark;

impl Guest for Benchmark {
    fn compute_fib(n: u32) -> f64 {
        workloads::compute_fib(n)
    }

    fn compute_sieve(n: u32) -> f64 {
        workloads::compute_sieve(n)
    }

    fn compute_mandelbrot(n: u32) -> f64 {
        workloads::compute_mandelbrot(n)
    }

    fn compute_strings(n: u32) -> f64 {
        workloads::compute_strings(n)
    }

    fn compute_sort(n: u32) -> f64 {
        workloads::compute_sort(n)
    }

    fn compute_map(n: u32) -> f64 {
        workloads::compute_map(n)
    }

    fn ffi_nop(n: u32) -> f64 {
        for _ in 0..n {
            host::nop();
        }
        n as f64
    }

    fn ffi_add(n: u32) -> f64 {
        let mut value = 0;
        for _ in 0..n {
            value = host::add(value, 1);
        }
        value as f64
    }

    fn ffi_string_in(n: u32) -> f64 {
        let mut total = 0u64;
        for _ in 0..n {
            total += host::take_string("hello, world") as u64;
        }
        total as f64
    }

    fn ffi_string_out(n: u32) -> f64 {
        let mut total = 0u64;
        for _ in 0..n {
            total += host::get_string().len() as u64;
        }
        total as f64
    }

    fn ffi_struct(n: u32) -> f64 {
        let mut value = host::Vec3 {
            x: 0.0,
            y: 0.0,
            z: 0.0,
        };
        for _ in 0..n {
            value = host::transform(value);
        }
        (value.x + value.y + value.z) as f64
    }

    fn ffi_resource(n: u32) -> f64 {
        let counter = host::get_counter();
        let mut value = 0;
        for _ in 0..n {
            value = counter.add(1);
        }
        value as f64
    }

    fn callback_add(a: i32, b: i32) -> i32 {
        a + b
    }

    fn callback_string(value: String) -> i32 {
        value.len() as i32
    }
}

export!(Benchmark);
