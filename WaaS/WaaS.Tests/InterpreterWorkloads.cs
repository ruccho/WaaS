using System.Diagnostics;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

// C# definitions corresponding to Guests/interpreter-bench/wit/interpreter-bench.wit
[ComponentInterface(@"waas:interpreter-bench/interpreter-bench")]
public partial interface IInterpreterBenchExports
{
    [ComponentApi(@"run-strings")]
    ValueTask<ulong> RunStrings(uint n);

    [ComponentApi(@"run-lists")]
    ValueTask<ulong> RunLists(uint n);
}

/// <summary>
///     A guest workload to measure the performance of the interpreter.
/// </summary>
public sealed class InterpreterWorkload
{
    public InterpreterWorkload(string name, Func<ExecutionContext, ulong> run)
    {
        Name = name;
        Run = run;
    }

    public string Name { get; }

    /// <summary>
    ///     Runs the workload once and returns its checksum.
    /// </summary>
    public Func<ExecutionContext, ulong> Run { get; }

    public override string ToString()
    {
        return Name;
    }
}

public readonly record struct InterpreterWorkloadResult(
    string Name,
    ulong Checksum,
    ulong Instructions,
    double BestMilliseconds,
    double MedianMilliseconds)
{
    public double NanosecondsPerInstruction => BestMilliseconds * 1e6 / Instructions;

    public override string ToString()
    {
        return
            $"{Name,-10} best {BestMilliseconds,9:F2} ms  median {MedianMilliseconds,9:F2} ms  {Instructions,11} instrs  {NanosecondsPerInstruction,7:F2} ns/instr  checksum {Checksum}";
    }
}

/// <summary>
///     Guest workloads shared by the benchmark test and external harnesses (e.g. a console app run on Mono).
/// </summary>
public static class InterpreterWorkloads
{
    private const string LoopWat = """
                                   (module
                                     (func (export "run") (param $n i32) (result i32)
                                       (local $i i32) (local $acc i32)
                                       (loop $l
                                         (local.set $acc
                                           (i32.add
                                             (i32.mul (local.get $acc) (i32.const 31))
                                             (i32.xor (local.get $i) (i32.shr_u (local.get $i) (i32.const 3)))))
                                         (local.set $i (i32.add (local.get $i) (i32.const 1)))
                                         (br_if $l (i32.lt_u (local.get $i) (local.get $n))))
                                       (local.get $acc)))
                                   """;

    // fills 4096 words, sums them and copies 16 KiB byte by byte on each round
    private const string MemoryWat = """
                                     (module
                                       (memory 1)
                                       (func (export "run") (param $n i32) (result i64)
                                         (local $i i32) (local $r i32) (local $sum i64)
                                         (loop $round
                                           (local.set $i (i32.const 0))
                                           (loop $fill
                                             (i32.store (i32.shl (local.get $i) (i32.const 2))
                                               (i32.add (local.get $i) (local.get $r)))
                                             (br_if $fill
                                               (i32.ne (local.tee $i (i32.add (local.get $i) (i32.const 1)))
                                                 (i32.const 4096))))
                                           (local.set $i (i32.const 0))
                                           (loop $sum
                                             (local.set $sum
                                               (i64.add (local.get $sum)
                                                 (i64.extend_i32_u (i32.load (i32.shl (local.get $i) (i32.const 2))))))
                                             (br_if $sum
                                               (i32.ne (local.tee $i (i32.add (local.get $i) (i32.const 1)))
                                                 (i32.const 4096))))
                                           (local.set $i (i32.const 0))
                                           (loop $copy
                                             (i32.store8 offset=16384 (local.get $i) (i32.load8_u (local.get $i)))
                                             (br_if $copy
                                               (i32.ne (local.tee $i (i32.add (local.get $i) (i32.const 1)))
                                                 (i32.const 16384))))
                                           (local.set $sum (i64.add (local.get $sum)
                                             (i64.load offset=16384 (i32.const 8))))
                                           (br_if $round
                                             (i32.lt_u (local.tee $r (i32.add (local.get $r) (i32.const 1)))
                                               (local.get $n))))
                                         (local.get $sum)))
                                     """;

    private const string FibWat = """
                                  (module
                                    (func $fib (export "run") (param $n i32) (result i32)
                                      (if (result i32) (i32.lt_u (local.get $n) (i32.const 2))
                                        (then (local.get $n))
                                        (else
                                          (i32.add
                                            (call $fib (i32.sub (local.get $n) (i32.const 1)))
                                            (call $fib (i32.sub (local.get $n) (i32.const 2))))))))
                                  """;

    // iterates z = z^2 + c on a grid
    private const string FloatWat = """
                                    (module
                                      (func (export "run") (param $n i32) (result i32)
                                        (local $px i32) (local $py i32) (local $k i32) (local $count i32)
                                        (local $cx f64) (local $cy f64) (local $x f64) (local $y f64) (local $t f64)
                                        (loop $row
                                          (local.set $px (i32.const 0))
                                          (loop $col
                                            (local.set $cx (f64.sub (f64.div (f64.convert_i32_u (local.get $px))
                                              (f64.const 32)) (f64.const 2)))
                                            (local.set $cy (f64.sub (f64.div (f64.convert_i32_u (local.get $py))
                                              (f64.const 32)) (f64.const 1)))
                                            (local.set $x (f64.const 0))
                                            (local.set $y (f64.const 0))
                                            (local.set $k (i32.const 0))
                                            (block $escape
                                              (loop $iter
                                                (br_if $escape (f64.gt
                                                  (f64.add (f64.mul (local.get $x) (local.get $x))
                                                    (f64.mul (local.get $y) (local.get $y)))
                                                  (f64.const 4)))
                                                (local.set $t (f64.add (f64.sub (f64.mul (local.get $x) (local.get $x))
                                                  (f64.mul (local.get $y) (local.get $y))) (local.get $cx)))
                                                (local.set $y (f64.add (f64.mul (f64.mul (f64.const 2) (local.get $x))
                                                  (local.get $y)) (local.get $cy)))
                                                (local.set $x (local.get $t))
                                                (br_if $iter (i32.lt_u (local.tee $k (i32.add (local.get $k) (i32.const 1)))
                                                  (i32.const 64)))))
                                            (local.set $count (i32.add (local.get $count) (local.get $k)))
                                            (br_if $col (i32.lt_u (local.tee $px (i32.add (local.get $px) (i32.const 1)))
                                              (i32.const 96))))
                                          (br_if $row (i32.lt_u (local.tee $py (i32.add (local.get $py) (i32.const 1)))
                                            (local.get $n))))
                                        (local.get $count)))
                                    """;

    /// <summary>
    ///     Creates the workloads.
    /// </summary>
    /// <param name="rustGuestPath">Path of the component built from Guests/interpreter-bench, or null to skip.</param>
    /// <param name="scale">Multiplier of the amount of work.</param>
    public static IReadOnlyList<InterpreterWorkload> Create(string? rustGuestPath, double scale = 1)
    {
        uint Scaled(uint n)
        {
            return Math.Max(1, (uint)(n * scale));
        }

        var workloads = new List<InterpreterWorkload>
        {
            CreateCore("loop", LoopWat, Scaled(1_000_000), false),
            CreateCore("memory", MemoryWat, Scaled(20), true),
            // the amount of work grows exponentially
            CreateCore("fib", FibWat, (uint)Math.Round(25 + Math.Log(scale, 1.618)), false),
            CreateCore("float", FloatWat, Scaled(32), false)
        };

        if (rustGuestPath != null)
        {
            var component = Component.Create(File.ReadAllBytes(rustGuestPath));
            var instance = component.Instantiate(new Dictionary<string, ISortedExportable>());

            var stringsN = Scaled(2_000);
            workloads.Add(new InterpreterWorkload("strings",
                context => new IInterpreterBenchExports.Wrapper(instance, context).RunStrings(stringsN)
                    .GetAwaiter().GetResult()));

            var listsN = Scaled(20_000);
            workloads.Add(new InterpreterWorkload("lists",
                context => new IInterpreterBenchExports.Wrapper(instance, context).RunLists(listsN)
                    .GetAwaiter().GetResult()));
        }

        return workloads;
    }

    private static InterpreterWorkload CreateCore(string name, string wat, uint n, bool resultIsI64)
    {
        var module = Module.Create(Utils.GetModuleBinary(wat));
        var instance = new Instance(module, new Imports());
        if (!instance.TryGetExport("run", out IInvocableFunction run)) throw new InvalidOperationException();

        return new InterpreterWorkload(name, context =>
        {
            Span<StackValueItem> args = stackalloc StackValueItem[1];
            args[0] = new StackValueItem(n);
            context.Invoke(run, args);
            Span<StackValueItem> results = stackalloc StackValueItem[1];
            context.TakeResults(results);
            return resultIsI64 ? results[0].ExpectValueI64() : results[0].ExpectValueI32();
        });
    }

    /// <summary>
    ///     Runs the workload repeatedly and measures the time. The number of the executed instructions is measured with
    ///     the fuel.
    /// </summary>
    public static InterpreterWorkloadResult Measure(InterpreterWorkload workload, int iterations)
    {
        using var context = new ExecutionContext();

        // warm up and count the instructions
        const ulong initialFuel = 1UL << 62;
        context.Fuel = initialFuel;
        var checksum = workload.Run(context);
        var instructions = initialFuel - context.Fuel!.Value;
        context.Fuel = null;

        var times = new double[iterations];
        for (var i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = workload.Run(context);
            stopwatch.Stop();
            if (result != checksum)
                throw new InvalidOperationException($"{workload.Name}: checksum mismatch ({result} != {checksum})");
            times[i] = stopwatch.Elapsed.TotalMilliseconds;
        }

        Array.Sort(times);
        return new InterpreterWorkloadResult(workload.Name, checksum, instructions, times[0],
            times[iterations / 2]);
    }
}
