//! The workloads as a core module without imports, for the runtimes outside of .NET.

#[path = "../../guest/src/workloads.rs"]
mod workloads;

#[no_mangle]
pub extern "C" fn compute_fib(n: u32) -> f64 {
    workloads::compute_fib(n)
}

#[no_mangle]
pub extern "C" fn compute_sieve(n: u32) -> f64 {
    workloads::compute_sieve(n)
}

#[no_mangle]
pub extern "C" fn compute_mandelbrot(n: u32) -> f64 {
    workloads::compute_mandelbrot(n)
}

#[no_mangle]
pub extern "C" fn compute_strings(n: u32) -> f64 {
    workloads::compute_strings(n)
}

#[no_mangle]
pub extern "C" fn compute_sort(n: u32) -> f64 {
    workloads::compute_sort(n)
}

#[no_mangle]
pub extern "C" fn compute_map(n: u32) -> f64 {
    workloads::compute_map(n)
}
