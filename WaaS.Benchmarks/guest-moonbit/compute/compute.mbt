// The workloads as a core module without imports, for the runtimes outside of .NET.

///|
pub fn compute_fib(n : Int) -> Double {
  @workloads.compute_fib(n.reinterpret_as_uint())
}

///|
pub fn compute_sieve(n : Int) -> Double {
  @workloads.compute_sieve(n.reinterpret_as_uint())
}

///|
pub fn compute_mandelbrot(n : Int) -> Double {
  @workloads.compute_mandelbrot(n.reinterpret_as_uint())
}

///|
pub fn compute_strings(n : Int) -> Double {
  @workloads.compute_strings(n.reinterpret_as_uint())
}

///|
pub fn compute_sort(n : Int) -> Double {
  @workloads.compute_sort(n.reinterpret_as_uint())
}

///|
pub fn compute_map(n : Int) -> Double {
  @workloads.compute_map(n.reinterpret_as_uint())
}
