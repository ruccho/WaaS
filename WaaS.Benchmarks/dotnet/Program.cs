using System.Diagnostics;
using System.Globalization;
using Waas.Benchmark;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

// Measures WaaS on CoreCLR, with the same workloads as the Unity harness.
// Usage: WaaS.Benchmarks.CoreClr <rust component> <moonbit component> <output>

const int warmupRuns = 3;
const int samples = 15;
const int loadRuns = 30;
const uint callCount = 100_000;

var records = new List<string>();

foreach (var (guest, path) in new[] { ("rust", args[0]), ("moonbit", args[1]) })
{
    var bytes = File.ReadAllBytes(path);

    var loads = new double[loadRuns];
    for (var i = 0; i < loadRuns; i++)
    {
        var stopwatch = Stopwatch.StartNew();
        var loaded = Component.Create(bytes);
        Instantiate(loaded);
        loads[i] = stopwatch.Elapsed.TotalMilliseconds;
    }

    Array.Sort(loads);
    records.Add(
        $$"""{"kind":"load","engine":"waas-coreclr","guest":"{{guest}}","phase":"warm","stage":"total","ms":{{Number(loads[loadRuns / 2])}}}""");

    using var context = new ExecutionContext();
    var exports = new IBenchmark.Wrapper(Instantiate(Component.Create(bytes)), context);

    (string name, uint n, Func<uint, ValueTask<double>> run)[] workloads =
    {
        ("compute_fib", 30, exports.ComputeFib),
        ("compute_sieve", 1_000_000, exports.ComputeSieve),
        ("compute_mandelbrot", 200, exports.ComputeMandelbrot),
        ("compute_strings", 20_000, exports.ComputeStrings),
        ("compute_sort", 200_000, exports.ComputeSort),
        ("compute_map", 500_000, exports.ComputeMap),
        ("ffi_nop", callCount, exports.FfiNop),
        ("ffi_add", callCount, exports.FfiAdd),
        ("ffi_string_in", callCount, exports.FfiStringIn),
        ("ffi_string_out", callCount, exports.FfiStringOut),
        ("ffi_struct", callCount, exports.FfiStruct),
        ("ffi_resource", callCount, exports.FfiResource)
    };

    foreach (var (name, n, run) in workloads)
        Measure(guest, name.StartsWith("compute") ? "compute" : "ffi", name, n, () => run(n).Result);

    Measure(guest, "callback", "callback_add", callCount, () =>
    {
        var value = 0;
        for (var i = 0; i < callCount; i++) value = exports.CallbackAdd(value, 1).Result;
        return value;
    });

    Measure(guest, "callback", "callback_string", callCount, () =>
    {
        var total = 0.0;
        for (var i = 0; i < callCount; i++) total += exports.CallbackString("hello, world").Result;
        return total;
    });
}

File.WriteAllText(args[2], $$"""{"records":[{{string.Join(",", records)}}]}""");
return;

void Measure(string guest, string group, string name, uint n, Func<double> workload)
{
    var checksum = 0.0;
    for (var i = 0; i < warmupRuns; i++) checksum = workload();

    var times = new double[samples];
    for (var i = 0; i < samples; i++)
    {
        GC.Collect();
        var stopwatch = Stopwatch.StartNew();
        checksum = workload();
        times[i] = stopwatch.Elapsed.TotalMilliseconds;
    }

    records.Add(
        $$"""{"kind":"exec","engine":"waas-coreclr","guest":"{{guest}}","group":"{{group}}","name":"{{name}}","n":{{n}},"checksum":{{Number(checksum)}},"samples":[{{string.Join(",", times.Select(Number))}}]}""");
}

static IInstance Instantiate(IComponent component)
{
    return component.Instantiate(new Dictionary<string, ISortedExportable>
    {
        { "waas:benchmark/host", IHost.CreateWaaSInstance(new Host()) }
    });
}

static string Number(double value)
{
    return value.ToString("R", CultureInfo.InvariantCulture);
}

internal sealed class Host : IHost
{
    private const string Text = "hello, world";

    public IHost.ICounterResourceImpl Counter { get; } = new CounterType();

    public ValueTask Nop()
    {
        return default;
    }

    public ValueTask<int> Add(int a, int b)
    {
        return new ValueTask<int>(a + b);
    }

    public ValueTask<int> TakeString(string value)
    {
        return new ValueTask<int>(value.Length);
    }

    public ValueTask<string> GetString()
    {
        return new ValueTask<string>(Text);
    }

    public ValueTask<IHost.Vec3> Transform(IHost.Vec3 value)
    {
        return new ValueTask<IHost.Vec3>(new IHost.Vec3 { X = value.X + 1, Y = value.Y + 2, Z = value.Z + 3 });
    }

    public ValueTask<Owned<IHost.ICounterResourceImpl>> GetCounter()
    {
        var owned = ((CounterType)Counter).Wrap(new[] { 0 });
        return new ValueTask<Owned<IHost.ICounterResourceImpl>>((Owned<IHost.ICounterResourceImpl>)owned);
    }

    private sealed class CounterType : HostResourceTypeBase<int[]>, IHost.ICounterResourceImpl
    {
        public IResourceType Type => this;

        public ValueTask<int> Add(Borrowed<IHost.ICounterResourceImpl> self, int delta)
        {
            var counter = Unwrap(self);
            counter[0] += delta;
            return new ValueTask<int>(counter[0]);
        }
    }
}
