wit_bindgen::generate!({
    path: "wit",
    world: "host-call-bench",
});

use waas::host_call_bench::host;

struct HostCallBench;

impl Guest for HostCallBench {
    fn run_nop(n: u32) {
        for _ in 0..n {
            host::nop();
        }
    }

    fn run_string(n: u32, value: String) {
        for _ in 0..n {
            host::take_string(&value);
        }
    }

    fn run_add_one(n: u32) -> u32 {
        let mut value = 0;
        for _ in 0..n {
            value = host::add_one(value);
        }
        value
    }

    fn run_loop(n: u32) -> u32 {
        let mut value = 0u32;
        for i in 0..n {
            value = std::hint::black_box(value.wrapping_add(i));
        }
        value
    }
}

export!(HostCallBench);
