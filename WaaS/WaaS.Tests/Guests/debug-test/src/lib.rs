// the lines are referred to by DebugAdapterTests

#[inline(never)]
fn square(x: u32) -> u32 {
    let result = x.wrapping_mul(x);
    result
}

#[no_mangle]
pub extern "C" fn compute(n: u32) -> u32 {
    let mut sum = 0u32;
    for i in 0..n {
        sum = sum.wrapping_add(square(i));
    }
    sum
}
