//! Workloads of the benchmark. Keep the behavior identical to `lua/benchmark.lua`:
//! the harness compares the checksums of both implementations.

wit_bindgen::generate!({
    path: "wit",
    world: "benchmark",
});

use std::collections::HashMap;
use waas::benchmark::host;

const MODULUS: u64 = 1_000_000_007;

struct Benchmark;

fn fib(n: u32) -> u32 {
    if n < 2 {
        n
    } else {
        fib(n - 1) + fib(n - 2)
    }
}

/// Park-Miller generator. Its intermediate values fit in 2^53.
fn next_random(x: u64) -> u64 {
    x * 16807 % 2147483647
}

impl Guest for Benchmark {
    fn compute_fib(n: u32) -> f64 {
        fib(n) as f64
    }

    fn compute_sieve(n: u32) -> f64 {
        let n = n as usize;
        let mut composite = vec![false; n + 1];
        let mut count = 0u32;
        for i in 2..=n {
            if !composite[i] {
                count += 1;
                // usize is 32-bit on wasm32, where i * i overflows
                let mut j = i as u64 * i as u64;
                while j <= n as u64 {
                    composite[j as usize] = true;
                    j += i as u64;
                }
            }
        }
        count as f64
    }

    fn compute_mandelbrot(n: u32) -> f64 {
        let mut total = 0u32;
        for y in 0..n {
            let ci = 2.0 * y as f64 / n as f64 - 1.0;
            for x in 0..n {
                let cr = 2.0 * x as f64 / n as f64 - 1.5;
                let (mut zr, mut zi) = (0.0f64, 0.0f64);
                let mut i = 0u32;
                while i < 50 && zr * zr + zi * zi <= 4.0 {
                    let t = zr * zr - zi * zi + cr;
                    zi = 2.0 * zr * zi + ci;
                    zr = t;
                    i += 1;
                }
                total += i;
            }
        }
        total as f64
    }

    fn compute_strings(n: u32) -> f64 {
        let mut total = 0u64;
        for i in 0..n {
            let s = format!("item-{}-{}", i, i as u64 * 7919 % 65536);
            let upper = s.to_uppercase();
            for word in upper.split('-') {
                total += word.len() as u64;
            }
            for b in upper.bytes() {
                total = (total * 31 + b as u64) % MODULUS;
            }
        }
        total as f64
    }

    fn compute_sort(n: u32) -> f64 {
        let mut x = 42u64;
        let mut values = Vec::with_capacity(n as usize);
        for _ in 0..n {
            x = next_random(x);
            values.push(x);
        }
        values.sort_unstable();
        let mut total = 0u64;
        for (i, value) in values.iter().enumerate() {
            total = (total + value % 65536 * (i as u64 + 1)) % MODULUS;
        }
        total as f64
    }

    fn compute_map(n: u32) -> f64 {
        let mut x = 42u64;
        let mut counts: HashMap<u64, u64> = HashMap::new();
        for _ in 0..n {
            x = next_random(x);
            *counts.entry(x % 4096).or_insert(0) += 1;
        }
        let mut total = 0u64;
        for key in 0..4096u64 {
            if let Some(count) = counts.get(&key) {
                total = (total * 31 + key * count) % MODULUS;
            }
        }
        total as f64
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
