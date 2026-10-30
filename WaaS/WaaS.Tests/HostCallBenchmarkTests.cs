using System.Diagnostics;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

// C# definitions corresponding to Guests/host-call-bench/wit/host-call-bench.wit

[ComponentInterface(@"waas:host-call-bench/host")]
public partial interface IHostCallBenchHost
{
    [ComponentApi(@"nop")]
    ValueTask Nop();

    [ComponentApi(@"take-string")]
    ValueTask TakeString(string value);

    [ComponentApi(@"add-one")]
    ValueTask<uint> AddOne(uint value);
}

[ComponentInterface(@"waas:host-call-bench/host-call-bench")]
public partial interface IHostCallBenchExports
{
    [ComponentApi(@"run-nop")]
    ValueTask RunNop(uint n);

    [ComponentApi(@"run-string")]
    ValueTask RunString(uint n, string value);

    [ComponentApi(@"run-add-one")]
    ValueTask<uint> RunAddOne(uint n);

    [ComponentApi(@"run-loop")]
    ValueTask<uint> RunLoop(uint n);
}

/// <summary>
///     Measures the cost of calls from a guest component into host functions.
///     Run the benchmark with <c>dotnet test -c Release --filter Name=MeasureHostCalls</c>.
///     Setting <c>DOTNET_TieredCompilation=0</c> makes the results stable.
/// </summary>
public class HostCallBenchmarkTests
{
    private static IComponent? component;

    private BenchHost host = null!;
    private IInstance instance = null!;

    [OneTimeSetUp]
    public void BuildGuest()
    {
        component ??= Component.Create(File.ReadAllBytes(GuestBuilder.Build("host-call-bench")));
    }

    [SetUp]
    public void Instantiate()
    {
        host = new BenchHost();
        instance = component!.Instantiate(new Dictionary<string, ISortedExportable>
        {
            { "waas:host-call-bench/host", IHostCallBenchHost.CreateWaaSInstance(host) }
        });
    }

    [Test]
    public async Task HostFunctionsAreCalled()
    {
        using var context = new ExecutionContext();
        var exports = new IHostCallBenchExports.Wrapper(instance, context);

        await exports.RunNop(10);
        Assert.That(host.NopCount, Is.EqualTo(10));

        await exports.RunString(3, "abc");
        Assert.That(host.StringLength, Is.EqualTo(9));

        Assert.That(await exports.RunAddOne(10), Is.EqualTo(10));
    }

    [Test]
    [Explicit("benchmark")]
    public async Task MeasureHostCalls()
    {
        const uint n = 100_000;
        using var context = new ExecutionContext();
        var exports = new IHostCallBenchExports.Wrapper(instance, context);

        await Measure("guest loop", n, () => exports.RunLoop(n));
        await Measure("nop()", n, () => exports.RunNop(n));
        await Measure("take-string(string)", n, () => exports.RunString(n, "hello, world"));
        await Measure("add-one(u32) -> u32", n, () => exports.RunAddOne(n));
    }

    private static async ValueTask Measure<T>(string name, uint n, Func<ValueTask<T>> run)
    {
        await Measure(name, n, async () => { await run(); });
    }

    private static async ValueTask Measure(string name, uint n, Func<ValueTask> run)
    {
        // warm up
        await run();

        const int rounds = 5;
        var best = double.MaxValue;
        long allocated = 0;
        for (var i = 0; i < rounds; i++)
        {
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            await run();
            stopwatch.Stop();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            best = Math.Min(best, stopwatch.Elapsed.TotalMilliseconds * 1000 / n);
        }

        TestContext.Out.WriteLine($"{name,-24} {best,8:F3} us/call {(double)allocated / n,10:F1} B/call");
    }

    private class BenchHost : IHostCallBenchHost
    {
        public int NopCount { get; private set; }
        public int StringLength { get; private set; }

        public ValueTask Nop()
        {
            NopCount++;
            return default;
        }

        public ValueTask TakeString(string value)
        {
            StringLength += value.Length;
            return default;
        }

        public ValueTask<uint> AddOne(uint value)
        {
            return new ValueTask<uint>(value + 1);
        }
    }
}
