// Workloads of the benchmark. Keep the behavior identical to guest/src/workloads.rs and lua/benchmark.lua:
// the harnesses compare the checksums of the implementations.

///|
const MODULUS : UInt64 = 1000000007

///|
fn fib(n : UInt) -> UInt {
  if n < 2 {
    n
  } else {
    fib(n - 1) + fib(n - 2)
  }
}

///|
pub fn compute_fib(n : UInt) -> Double {
  fib(n).to_double()
}

///|
pub fn compute_sieve(n : UInt) -> Double {
  let n = n.reinterpret_as_int()
  let composite = FixedArray::make(n + 1, false)
  let mut count = 0
  for i in 2..=n {
    if !composite[i] {
      count += 1
      let mut j = i.to_int64() * i.to_int64()
      while j <= n.to_int64() {
        composite[j.to_int()] = true
        j += i.to_int64()
      }
    }
  }
  count.to_double()
}

///|
pub fn compute_mandelbrot(n : UInt) -> Double {
  let n = n.reinterpret_as_int()
  let size = n.to_double()
  let mut total = 0
  for y in 0..<n {
    let ci = 2.0 * y.to_double() / size - 1.0
    for x in 0..<n {
      let cr = 2.0 * x.to_double() / size - 1.5
      let mut zr = 0.0
      let mut zi = 0.0
      let mut i = 0
      while i < 50 && zr * zr + zi * zi <= 4.0 {
        let t = zr * zr - zi * zi + cr
        zi = 2.0 * zr * zi + ci
        zr = t
        i += 1
      }
      total += i
    }
  }
  total.to_double()
}

///|
pub fn compute_strings(n : UInt) -> Double {
  let n = n.reinterpret_as_int()
  let mut total : UInt64 = 0
  for i in 0..<n {
    let second = i.to_int64() * 7919 % 65536
    let s = "item-\{i}-\{second}"
    let upper = s.to_upper()
    for word in upper.split("-") {
      total += word.length().to_uint64()
    }
    for c in upper {
      total = (total * 31 + c.to_int().to_uint64()) % MODULUS
    }
  }
  total.to_double()
}

///|
fn next_random(x : UInt64) -> UInt64 {
  x * 16807 % 2147483647
}

///|
pub fn compute_sort(n : UInt) -> Double {
  let n = n.reinterpret_as_int()
  let mut x : UInt64 = 42
  let values = FixedArray::make(n, (0 : UInt64))
  for i in 0..<n {
    x = next_random(x)
    values[i] = x
  }
  values.sort()
  let mut total : UInt64 = 0
  for i in 0..<n {
    total = (total + values[i] % 65536 * (i + 1).to_uint64()) % MODULUS
  }
  total.to_double()
}

///|
pub fn compute_map(n : UInt) -> Double {
  let n = n.reinterpret_as_int()
  let mut x : UInt64 = 42
  let counts : Map[UInt64, UInt64] = Map::new()
  for _ in 0..<n {
    x = next_random(x)
    let key = x % 4096
    counts[key] = counts.get(key).unwrap_or(0) + 1
  }
  let mut total : UInt64 = 0
  for key in 0..<4096 {
    let k = key.to_uint64()
    match counts.get(k) {
      Some(count) => total = (total * 31 + k * count) % MODULUS
      None => ()
    }
  }
  total.to_double()
}
