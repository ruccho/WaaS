using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

/// <summary>
///     Tests the calls between a guest and the host that do not go through frames and formatters.
/// </summary>
public class DirectInvocationTests
{
    private static IComponent? component;

    [OneTimeSetUp]
    public void BuildGuest()
    {
        component ??= Component.Create(File.ReadAllBytes(GuestBuilder.Build("host-call-bench")));
    }

    private static IInstance Instantiate(IHostCallBenchHost host)
    {
        return component!.Instantiate(new Dictionary<string, ISortedExportable>
        {
            { "waas:host-call-bench/host", IHostCallBenchHost.CreateWaaSInstance(host) }
        });
    }

    [Test]
    public async Task HostFunctionsCompleteSynchronously()
    {
        var host = new CountingHost();
        using var context = new ExecutionContext();
        var exports = new IHostCallBenchExports.Wrapper(Instantiate(host), context);

        Assert.That(await exports.RunAddOne(5), Is.EqualTo(5));
        await exports.RunNop(3);
        Assert.That(host.NopCount, Is.EqualTo(3));
    }

    [Test]
    public async Task HostFunctionsSuspend()
    {
        using var context = new ExecutionContext();
        var exports = new IHostCallBenchExports.Wrapper(Instantiate(new YieldingHost()), context);

        Assert.That(await exports.RunAddOne(5), Is.EqualTo(5));
        await exports.RunNop(3);
    }

    [Test]
    public async Task ExceptionOfHostFunctionPropagates()
    {
        using var context = new ExecutionContext();
        var exports = new IHostCallBenchExports.Wrapper(Instantiate(new ThrowingHost()), context);

        await Assert.ThrowsAsync<InvalidTimeZoneException>(async () => await exports.RunAddOne(1));
    }

    [Test]
    public async Task ExceptionAfterSuspensionPropagates()
    {
        using var context = new ExecutionContext();
        var exports = new IHostCallBenchExports.Wrapper(Instantiate(new ThrowingAfterYieldHost()), context);

        await Assert.ThrowsAsync<InvalidTimeZoneException>(async () => await exports.RunAddOne(1));
    }

    [Test]
    public async Task ContextIsReusableAfterFailure()
    {
        using var context = new ExecutionContext();
        var failing = new IHostCallBenchExports.Wrapper(Instantiate(new ThrowingHost()), context);
        await Assert.ThrowsAsync<InvalidTimeZoneException>(async () => await failing.RunAddOne(1));

        var exports = new IHostCallBenchExports.Wrapper(Instantiate(new CountingHost()), context);
        Assert.That(await exports.RunAddOne(2), Is.EqualTo(2));
    }

    [Test]
    public void BindingsWithoutScalarsAreNotDirect()
    {
        var instance = IHostCallBenchHost.CreateWaaSInstance(new CountingHost());
        Assert.That(instance.TryGetExport("add-one", out IFunction? addOne), Is.True);
        Assert.That(instance.TryGetExport("take-string", out IFunction? takeString), Is.True);

        Assert.That(((WaaS.ComponentModel.Binding.ExternalFunction)addOne!).SupportsDirectInvoke, Is.True);
        Assert.That(((WaaS.ComponentModel.Binding.ExternalFunction)takeString!).SupportsDirectInvoke, Is.False);
    }

    private class CountingHost : IHostCallBenchHost
    {
        public int NopCount { get; private set; }

        public ValueTask Nop()
        {
            NopCount++;
            return default;
        }

        public ValueTask TakeString(string value)
        {
            return default;
        }

        public ValueTask<uint> AddOne(uint value)
        {
            return new ValueTask<uint>(value + 1);
        }
    }

    private class YieldingHost : IHostCallBenchHost
    {
        public async ValueTask Nop()
        {
            await Task.Yield();
        }

        public async ValueTask TakeString(string value)
        {
            await Task.Yield();
        }

        public async ValueTask<uint> AddOne(uint value)
        {
            await Task.Yield();
            return value + 1;
        }
    }

    private class ThrowingHost : IHostCallBenchHost
    {
        public ValueTask Nop()
        {
            throw new InvalidTimeZoneException();
        }

        public ValueTask TakeString(string value)
        {
            throw new InvalidTimeZoneException();
        }

        public ValueTask<uint> AddOne(uint value)
        {
            throw new InvalidTimeZoneException();
        }
    }

    private class ThrowingAfterYieldHost : IHostCallBenchHost
    {
        public ValueTask Nop()
        {
            return default;
        }

        public ValueTask TakeString(string value)
        {
            return default;
        }

        public async ValueTask<uint> AddOne(uint value)
        {
            await Task.Yield();
            throw new InvalidTimeZoneException();
        }
    }
}
