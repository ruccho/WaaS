using System.Diagnostics;
using System.Runtime.CompilerServices;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

// C# definitions corresponding to Guests/abi-test/wit/abi-test.wit

[ComponentInterface(@"waas:abi-test/host")]
public partial interface IAbiHost
{
    [ComponentResource(@"counter")]
    public partial interface ICounterResourceImpl : IResourceImpl
    {
        [ComponentApi(@"[method]counter.get")]
        ValueTask<int> Get(Borrowed<ICounterResourceImpl> self);

        [ComponentApi(@"[method]counter.increment")]
        ValueTask Increment(Borrowed<ICounterResourceImpl> self);
    }

    [ComponentRecord]
    public readonly partial struct Point
    {
        [ComponentField] public int X { get; init; }
        [ComponentField] public float YCoord { get; init; }
    }

    [ComponentVariant]
    public readonly partial struct Shape
    {
        [ComponentCase] public float? Circle { get; init; }
        [ComponentCase] public Point? Rect { get; init; }
        [ComponentCase] public None? Empty { get; init; }
    }

    public enum Color
    {
        Red,
        GreenBlue
    }

    [Flags]
    public enum Permissions : byte
    {
        Read = 1 << 0,
        Write = 1 << 1
    }

    [ComponentApi(@"make-counter")]
    ValueTask<Owned<ICounterResourceImpl>> MakeCounter(int initial);

    [ComponentApi(@"read-counter")]
    ValueTask<int> ReadCounter(Borrowed<ICounterResourceImpl> counter);

    [ComponentApi(@"echo")]
    ValueTask<string> Echo(string value);

    [ComponentApi(@"sum")]
    ValueTask<long> Sum(ReadOnlyMemory<int> values);

    [ComponentApi(@"increment-optional")]
    ValueTask<int?> IncrementOptional(int? value);

    [ComponentApi(@"checked-negate")]
    ValueTask<Result<int, string>> CheckedNegate(int value);

    [ComponentApi(@"shape-area")]
    ValueTask<float> ShapeArea(Shape shape);

    [ComponentApi(@"describe")]
    ValueTask<string> Describe(Color color, Permissions permissions);

    [ComponentApi(@"wait-ticks")]
    ValueTask<uint> WaitTicks(uint ticks);
}

[ComponentInterface(@"waas:abi-test/guest-types")]
public partial interface IAbiGuestTypes
{
    [ComponentResource(@"accumulator")]
    public partial interface IAccumulatorResourceImpl : IResourceImpl
    {
        [ComponentApi(@"[constructor]accumulator")]
        ValueTask<Owned<IAccumulatorResourceImpl>> New(int start);

        [ComponentApi(@"[method]accumulator.add")]
        ValueTask Add(Borrowed<IAccumulatorResourceImpl> self, int value);

        [ComponentApi(@"[method]accumulator.total")]
        ValueTask<int> Total(Borrowed<IAccumulatorResourceImpl> self);
    }

    [ComponentApi(@"destroyed-count")]
    ValueTask<uint> DestroyedCount();

    [ComponentApi(@"consume")]
    ValueTask<int> Consume(Owned<IAccumulatorResourceImpl> accumulator);
}

[ComponentInterface(@"waas:abi-test/abi-test")]
public partial interface IAbiTestExports
{
    [ComponentApi(@"test-resource")]
    ValueTask<int> TestResource();

    [ComponentApi(@"test-borrow-param")]
    ValueTask<int> TestBorrowParam(Borrowed<IAbiHost.ICounterResourceImpl> counter);

    [ComponentApi(@"test-string")]
    ValueTask<string> TestString(string value);

    [ComponentApi(@"test-list")]
    ValueTask<long> TestList();

    [ComponentApi(@"test-option")]
    ValueTask<int> TestOption();

    [ComponentApi(@"test-result")]
    ValueTask<string> TestResult();

    [ComponentApi(@"test-record")]
    ValueTask<IAbiHost.Point> TestRecord(IAbiHost.Point value);

    [ComponentApi(@"test-variant")]
    ValueTask<float> TestVariant();

    [ComponentApi(@"test-enum-flags")]
    ValueTask<string> TestEnumFlags();

    [ComponentApi(@"test-async")]
    ValueTask<uint> TestAsync();

    [ComponentApi(@"test-forged-handle")]
    ValueTask<int> TestForgedHandle(uint handle);
}

/// <summary>
///     Tests of the Canonical ABI using a guest component built with wit-bindgen.
///     Requires cargo with the wasm32-unknown-unknown target and wasm-tools.
/// </summary>
public class ComponentAbiTests
{
    private static IComponent? component;

    private AbiHost host = null!;
    private IInstance instance = null!;

    [OneTimeSetUp]
    public void BuildGuest()
    {
        component ??= Component.Create(File.ReadAllBytes(GuestBuilder.Build("abi-test")));
    }

    [SetUp]
    public void Instantiate()
    {
        host = new AbiHost();
        var hostInstance = IAbiHost.CreateWaaSInstance(host);

        // the world imports the types it uses with `use` at the top level as well
        hostInstance.TryGetExport("counter", out IType? counter);
        hostInstance.TryGetExport("point", out IType? point);

        instance = component!.Instantiate(new Dictionary<string, ISortedExportable>
        {
            { "waas:abi-test/host", hostInstance },
            { "counter", counter! },
            { "point", point! }
        });
    }

    private async ValueTask<T> Call<T>(Func<IAbiTestExports, ValueTask<T>> call)
    {
        using var context = new ExecutionContext();
        return await call(new IAbiTestExports.Wrapper(instance, context));
    }

    [Test]
    public async Task OwnedResourceIsDestroyedWhenGuestDropsIt()
    {
        // 10 + 2 increments: read through borrow (12) + get (12)
        Assert.That(await Call(e => e.TestResource()), Is.EqualTo(24));
        Assert.That(host.CounterType.Dropped, Is.EqualTo(new[] { 12 }));
    }

    [Test]
    public async Task BorrowedResourceSurvivesGuestDrop()
    {
        var owned = host.CounterType.Wrap(new Counter { Value = 1 });

        var result = await Call(e => e.TestBorrowParam((Borrowed<IAbiHost.ICounterResourceImpl>)owned.Borrow()));
        Assert.That(result, Is.EqualTo(2));
        Assert.That(host.CounterType.Dropped, Is.Empty);

        // the resource is still alive and can be lent again
        result = await Call(e => e.TestBorrowParam((Borrowed<IAbiHost.ICounterResourceImpl>)owned.Borrow()));
        Assert.That(result, Is.EqualTo(3));

        owned.Dispose();
        Assert.That(host.CounterType.Dropped, Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public async Task GuestDefinedResourceIsDestroyedByOwner()
    {
        using var context = new ExecutionContext();
        Assert.That(instance.TryGetExport("waas:abi-test/guest-types", out IInstance? guestTypesInstance), Is.True);
        var guestTypes = new IAbiGuestTypes.Wrapper(guestTypesInstance!, context);
        var accumulatorType = guestTypes.Accumulator;

        // the host owns an accumulator created by the guest
        var accumulator = await accumulatorType.New(5);
        await accumulatorType.Add((Borrowed<IAbiGuestTypes.IAccumulatorResourceImpl>)accumulator.handle.Borrow(), 3);
        Assert.That(await accumulatorType.Total((Borrowed<IAbiGuestTypes.IAccumulatorResourceImpl>)accumulator.handle.Borrow()),
            Is.EqualTo(8));
        Assert.That(await guestTypes.DestroyedCount(), Is.EqualTo(0));

        // dropping it on the host calls the destructor in the guest
        accumulator.handle.Dispose();
        Assert.That(await guestTypes.DestroyedCount(), Is.EqualTo(1));

        // passing the ownership back to the guest
        var another = await accumulatorType.New(10);
        Assert.That(await guestTypes.Consume(another), Is.EqualTo(10));
        Assert.That(await guestTypes.DestroyedCount(), Is.EqualTo(2));
    }

    [Test]
    public void ComponentCannotAccessResourcesNotPassedToIt()
    {
        // a host resource that exists but was never passed to the component
        var owned = host.CounterType.Wrap(new Counter { Value = 1 });
        var rep = owned.GetValue(false);

        foreach (var handle in new[] { rep, 12345u })
        {
            var exception = Assert.CatchAsync(async () => await Call(e => e.TestForgedHandle(handle)));
            Assert.That(exception, Is.InstanceOf<TrapException>().Or.InnerException.InstanceOf<TrapException>(),
                exception!.ToString());
        }

        owned.Dispose();
    }

    [Test]
    public async Task StringsRoundTrip()
    {
        Assert.That(await Call(e => e.TestString("こんにちは, world")), Is.EqualTo("こんにちは, world|12"));
        Assert.That(await Call(e => e.TestString("")), Is.EqualTo("|0"));
    }

    [Test]
    public async Task ListIsLowered()
    {
        Assert.That(await Call(e => e.TestList()), Is.EqualTo(5050));
    }

    [Test]
    public async Task OptionIsLiftedAndLowered()
    {
        Assert.That(await Call(e => e.TestOption()), Is.EqualTo(42 + 1000));
    }

    [Test]
    public async Task ResultIsLiftedAndLowered()
    {
        Assert.That(await Call(e => e.TestResult()), Is.EqualTo("-5/overflow"));
    }

    [Test]
    public async Task RecordRoundTrip()
    {
        var result = await Call(e => e.TestRecord(new IAbiHost.Point { X = 21, YCoord = 1.25f }));
        Assert.That(result.X, Is.EqualTo(42));
        Assert.That(result.YCoord, Is.EqualTo(1.75f));
    }

    [Test]
    public async Task VariantIsLifted()
    {
        Assert.That(await Call(e => e.TestVariant()), Is.EqualTo(3f + 6f + 0f));
    }

    [Test]
    public async Task EnumAndFlagsAreLifted()
    {
        Assert.That(await Call(e => e.TestEnumFlags()), Is.EqualTo("Red:Read;GreenBlue:Read, Write"));
    }

    [Test]
    public async Task AsyncHostFunctionSuspendsGuest()
    {
        using var context = new ExecutionContext();
        var task = new IAbiTestExports.Wrapper(instance, context).TestAsync();

        // the guest is suspended until the host completes the waits
        var ticks = 0;
        while (!task.IsCompleted)
        {
            Assert.That(ticks, Is.LessThan(100));
            host.Tick();
            ticks++;
        }

        Assert.That(await task, Is.EqualTo(3u * 2 + 4u * 2));
        Assert.That(ticks, Is.EqualTo(3 + 4));
        Assert.That(host.CompletedWaits, Is.EqualTo(2));
    }

    internal class Counter
    {
        public int Value;
    }

    internal class CounterType : HostResourceTypeBase<Counter>, IAbiHost.ICounterResourceImpl
    {
        public List<int> Dropped { get; } = new();
        public IResourceType Type => this;

        public ValueTask<int> Get(Borrowed<IAbiHost.ICounterResourceImpl> self)
        {
            return new ValueTask<int>(Unwrap(self).Value);
        }

        public ValueTask Increment(Borrowed<IAbiHost.ICounterResourceImpl> self)
        {
            Unwrap(self).Value++;
            return default;
        }

        protected override void OnDrop(Counter value)
        {
            Dropped.Add(value.Value);
        }
    }

    internal class AbiHost : IAbiHost
    {
        public CounterType CounterType { get; } = new();
        public int CompletedWaits { get; private set; }

        public IAbiHost.ICounterResourceImpl Counter => CounterType;

        public ValueTask<Owned<IAbiHost.ICounterResourceImpl>> MakeCounter(int initial)
        {
            return new ValueTask<Owned<IAbiHost.ICounterResourceImpl>>(
                (Owned<IAbiHost.ICounterResourceImpl>)CounterType.Wrap(new Counter { Value = initial }));
        }

        public ValueTask<int> ReadCounter(Borrowed<IAbiHost.ICounterResourceImpl> counter)
        {
            return CounterType.Get(counter);
        }

        public ValueTask<string> Echo(string value)
        {
            return new ValueTask<string>(value);
        }

        public ValueTask<long> Sum(ReadOnlyMemory<int> values)
        {
            long sum = 0;
            foreach (var value in values.Span) sum += value;
            return new ValueTask<long>(sum);
        }

        public ValueTask<int?> IncrementOptional(int? value)
        {
            return new ValueTask<int?>(value + 1);
        }

        public ValueTask<Result<int, string>> CheckedNegate(int value)
        {
            return new ValueTask<Result<int, string>>(value == int.MinValue
                ? Result<int, string>.CreateError("overflow")
                : Result<int, string>.CreateOk(-value));
        }

        public ValueTask<float> ShapeArea(IAbiHost.Shape shape)
        {
            return new ValueTask<float>(shape.Case switch
            {
                IAbiHost.Shape.VariantCase.Circle => 3f * shape.Circle!.Value * shape.Circle.Value,
                IAbiHost.Shape.VariantCase.Rect => shape.Rect!.Value.X * shape.Rect.Value.YCoord,
                _ => 0f
            });
        }

        public ValueTask<string> Describe(IAbiHost.Color color, IAbiHost.Permissions permissions)
        {
            return new ValueTask<string>($"{color}:{permissions}");
        }

        private TaskCompletionSource? tick;

        /// <summary>
        ///     Advances the waits on the current thread.
        ///     WaaS requires async host functions to complete on the thread that invoked them.
        /// </summary>
        public void Tick()
        {
            var current = tick;
            tick = null;
            current?.SetResult();
        }

        public async ValueTask<uint> WaitTicks(uint ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                tick = new TaskCompletionSource(TaskCreationOptions.None);
                await tick.Task;
            }

            CompletedWaits++;
            return ticks * 2;
        }
    }
}

internal static class GuestBuilder
{
    private static readonly Dictionary<string, string> Built = new();

    /// <summary>
    ///     Builds a guest crate under Guests/ and returns the path of the component.
    /// </summary>
    /// <param name="name">name of the crate</param>
    /// <param name="witOverrides">
    ///     WIT files that replace the files in the wit directory of the crate (file name -> source).
    ///     When specified, the crate is copied to a temporary directory and built there.
    /// </param>
    public static string Build(string name, IReadOnlyDictionary<string, string>? witOverrides = null)
    {
        lock (Built)
        {
            var key = witOverrides == null ? name : $"{name}+overrides";
            if (Built.TryGetValue(key, out var cached)) return cached;

            var crateDir = Path.Combine(GetSourceDirectory(), "Guests", name);
            var targetDir = Path.Combine(Path.GetTempPath(), "WaaS.Tests", "guest-target");

            if (witOverrides != null)
            {
                var copiedDir = Path.Combine(Path.GetTempPath(), "WaaS.Tests", "guest-overrides", name);
                if (Directory.Exists(copiedDir)) Directory.Delete(copiedDir, true);
                CopyDirectory(crateDir, copiedDir);
                foreach (var (fileName, source) in witOverrides)
                    File.WriteAllText(Path.Combine(copiedDir, "wit", fileName), source);
                crateDir = copiedDir;
                targetDir += "-overrides";
            }
            Run("cargo",
                $"build --release --target wasm32-unknown-unknown --target-dir \"{targetDir}\"", crateDir);

            var modulePath = Path.Combine(targetDir, "wasm32-unknown-unknown", "release",
                $"{name.Replace('-', '_')}.wasm");
            var componentPath = Path.Combine(targetDir, $"{name}.component.wasm");
            Run("wasm-tools", $"component new \"{modulePath}\" -o \"{componentPath}\"", crateDir);

            Built[key] = componentPath;
            return componentPath;
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        foreach (var directory in Directory.GetDirectories(source))
        {
            var name = Path.GetFileName(directory);
            if (name is "target") continue;
            CopyDirectory(directory, Path.Combine(destination, name));
        }
    }

    private static string GetSourceDirectory([CallerFilePath] string path = "")
    {
        return Path.GetDirectoryName(path)!;
    }

    private static void Run(string fileName, string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new Exception($"{fileName} {arguments} failed:\n{stdout.Result}\n{stderr.Result}");
    }
}
