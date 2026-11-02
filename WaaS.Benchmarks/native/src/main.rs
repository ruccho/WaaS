//! Measures the compute workloads on runtimes outside of .NET. Writes the results as JSON to the standard output.
//!
//! Usage: waas-benchmark-native <rust module> <moonbit module>

use std::{ffi::CString, fs, os::raw::c_char, ptr, time::Instant};

const WARMUP_RUNS: usize = 3;
const SAMPLES: usize = 15;
const LOAD_RUNS: usize = 30;

const WORKLOADS: [(&str, u32); 6] = [
    ("compute_fib", 30),
    ("compute_sieve", 1_000_000),
    ("compute_mandelbrot", 200),
    ("compute_strings", 20_000),
    ("compute_sort", 200_000),
    ("compute_map", 500_000),
];

fn measure(mut run: impl FnMut() -> f64) -> (f64, Vec<f64>) {
    let mut checksum = 0.0;
    for _ in 0..WARMUP_RUNS {
        checksum = run();
    }

    let samples = (0..SAMPLES)
        .map(|_| {
            let start = Instant::now();
            checksum = run();
            start.elapsed().as_secs_f64() * 1000.0
        })
        .collect();
    (checksum, samples)
}

fn record(engine: &str, guest: &str, name: &str, n: u32, checksum: f64, samples: &[f64]) -> String {
    let samples: Vec<String> = samples.iter().map(|s| s.to_string()).collect();
    format!(
        "{{\"kind\":\"exec\",\"engine\":\"{engine}\",\"guest\":\"{guest}\",\"group\":\"compute\",\"name\":\"{name}\",\"n\":{n},\"checksum\":{checksum},\"samples\":[{}]}}",
        samples.join(",")
    )
}

fn load_record(engine: &str, guest: &str, milliseconds: &[f64]) -> String {
    let mut sorted = milliseconds.to_vec();
    sorted.sort_by(f64::total_cmp);
    format!(
        "{{\"kind\":\"load\",\"engine\":\"{engine}\",\"guest\":\"{guest}\",\"phase\":\"warm\",\"stage\":\"total\",\"ms\":{}}}",
        sorted[sorted.len() / 2]
    )
}

fn run_wasmtime(guest: &str, bytes: &[u8], records: &mut Vec<String>) {
    use wasmtime::{Engine, Instance, Module, Store};

    let engine = Engine::default();

    let loads: Vec<f64> = (0..LOAD_RUNS)
        .map(|_| {
            let start = Instant::now();
            let module = Module::new(&engine, bytes).unwrap();
            let mut store = Store::new(&engine, ());
            Instance::new(&mut store, &module, &[]).unwrap();
            start.elapsed().as_secs_f64() * 1000.0
        })
        .collect();
    records.push(load_record("wasmtime", guest, &loads));

    let module = Module::new(&engine, bytes).unwrap();
    let mut store = Store::new(&engine, ());
    let instance = Instance::new(&mut store, &module, &[]).unwrap();

    for (name, n) in WORKLOADS {
        let function = instance.get_typed_func::<u32, f64>(&mut store, name).unwrap();
        let (checksum, samples) = measure(|| function.call(&mut store, n).unwrap());
        records.push(record("wasmtime", guest, name, n, checksum, &samples));
    }
}

#[allow(non_snake_case)]
mod wasm3 {
    use std::os::raw::{c_char, c_void};

    pub type Result = *const c_char;

    extern "C" {
        pub fn m3_NewEnvironment() -> *mut c_void;
        pub fn m3_FreeEnvironment(environment: *mut c_void);
        pub fn m3_NewRuntime(environment: *mut c_void, stack: u32, user: *mut c_void) -> *mut c_void;
        pub fn m3_FreeRuntime(runtime: *mut c_void);
        pub fn m3_ParseModule(environment: *mut c_void, module: *mut *mut c_void, bytes: *const u8, size: u32) -> Result;
        pub fn m3_LoadModule(runtime: *mut c_void, module: *mut c_void) -> Result;
        pub fn m3_FindFunction(function: *mut *mut c_void, runtime: *mut c_void, name: *const c_char) -> Result;
        pub fn m3_Call(function: *mut c_void, argc: u32, args: *const *const c_void) -> Result;
        pub fn m3_GetResults(function: *mut c_void, count: u32, results: *const *mut c_void) -> Result;
    }

    pub fn check(result: Result) {
        if !result.is_null() {
            panic!("wasm3: {}", unsafe { std::ffi::CStr::from_ptr(result) }.to_string_lossy());
        }
    }
}

const WASM3_STACK: u32 = 8 * 1024 * 1024;

fn run_wasm3(guest: &str, bytes: &[u8], records: &mut Vec<String>) {
    use wasm3::*;

    unsafe {
        let environment = m3_NewEnvironment();

        // wasm3 compiles a function when it is first called, which is not included in this time
        let loads: Vec<f64> = (0..LOAD_RUNS)
            .map(|_| {
                let start = Instant::now();
                let runtime = m3_NewRuntime(environment, WASM3_STACK, ptr::null_mut());
                let mut module = ptr::null_mut();
                check(m3_ParseModule(environment, &mut module, bytes.as_ptr(), bytes.len() as u32));
                check(m3_LoadModule(runtime, module));
                let elapsed = start.elapsed().as_secs_f64() * 1000.0;
                m3_FreeRuntime(runtime);
                elapsed
            })
            .collect();
        records.push(load_record("wasm3", guest, &loads));

        let runtime = m3_NewRuntime(environment, WASM3_STACK, ptr::null_mut());
        let mut module = ptr::null_mut();
        check(m3_ParseModule(environment, &mut module, bytes.as_ptr(), bytes.len() as u32));
        check(m3_LoadModule(runtime, module));

        for (name, n) in WORKLOADS {
            let function_name = CString::new(name).unwrap();
            let mut function = ptr::null_mut();
            check(m3_FindFunction(&mut function, runtime, function_name.as_ptr() as *const c_char));

            // wasm3 compiles the function here, and cannot compile some functions of valid modules
            let argument = n;
            let arguments = [&argument as *const u32 as *const _];
            let result = m3_Call(function, 1, arguments.as_ptr());
            if !result.is_null() {
                let message = std::ffi::CStr::from_ptr(result).to_string_lossy();
                records.push(format!(
                    "{{\"kind\":\"unsupported\",\"engine\":\"wasm3\",\"guest\":\"{guest}\",\"name\":\"{name}\",\"message\":\"{message}\"}}"
                ));
                continue;
            }

            let (checksum, samples) = measure(|| {
                let argument = n;
                let arguments = [&argument as *const u32 as *const _];
                check(m3_Call(function, 1, arguments.as_ptr()));
                let mut result = 0f64;
                let results = [&mut result as *mut f64 as *mut _];
                check(m3_GetResults(function, 1, results.as_ptr()));
                result
            });
            records.push(record("wasm3", guest, name, n, checksum, &samples));
        }

        m3_FreeRuntime(runtime);
        m3_FreeEnvironment(environment);
    }
}

fn main() {
    let arguments: Vec<String> = std::env::args().collect();
    let mut records = Vec::new();

    for (guest, path) in [("rust", &arguments[1]), ("moonbit", &arguments[2])] {
        let bytes = fs::read(path).unwrap();
        run_wasmtime(guest, &bytes, &mut records);
        run_wasm3(guest, &bytes, &mut records);
    }

    println!("{{\"records\":[{}]}}", records.join(","));
}
