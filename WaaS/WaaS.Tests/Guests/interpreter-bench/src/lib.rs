wit_bindgen::generate!({
    path: "wit",
    world: "interpreter-bench",
});

use std::collections::BTreeMap;

struct InterpreterBench;

impl Guest for InterpreterBench {
    fn run_strings(n: u32) -> u64 {
        let mut total = 0u64;
        for i in 0..n {
            let s = format!("item-{}-{}", i, i.wrapping_mul(7919));
            let upper = s.to_uppercase();
            for word in upper.split('-') {
                total = total.wrapping_add(word.len() as u64);
            }
            let hash = upper
                .bytes()
                .fold(0u64, |h, b| h.wrapping_mul(31).wrapping_add(b as u64));
            total = total.wrapping_add(hash);
        }
        total
    }

    fn run_lists(n: u32) -> u64 {
        let mut values: Vec<u32> = (0..n).map(|i| i.wrapping_mul(2654435761) >> 8).collect();
        values.sort_unstable();

        let mut total = 0u64;
        for i in 0..n {
            if values.binary_search(&(i << 4)).is_ok() {
                total += 1;
            }
        }

        let mut counts = BTreeMap::new();
        for value in &values {
            *counts.entry(value % 97).or_insert(0u32) += 1;
        }
        for (key, count) in counts {
            total = total.wrapping_mul(31).wrapping_add((key as u64) * (count as u64));
        }
        total
    }
}

export!(InterpreterBench);
