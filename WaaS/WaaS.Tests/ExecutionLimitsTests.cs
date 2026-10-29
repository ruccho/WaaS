using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;
using ValueType = WaaS.ValueType;

namespace WaaS.Tests;

[ComponentInterface(@"test:limits/host")]
public partial interface ILimitsHost
{
    [ComponentApi(@"wait")]
    ValueTask Wait();
}

public class ExecutionLimitsTests
{
    private const string InfiniteLoop = """
                                        (module
                                          (func (export "run") (loop $l br $l))
                                          (func (export "add") (param i32 i32) (result i32)
                                            local.get 0 local.get 1 i32.add)
                                        )
                                        """;

    private static IInvocableFunction GetFunction(Instance instance, string name)
    {
        Assert.That(instance.TryGetExport(name, out IInvocableFunction? function), Is.True);
        return function!;
    }

    private static T? FindException<T>(Exception? ex) where T : Exception
    {
        for (; ex != null; ex = ex.InnerException)
            if (ex is T found)
                return found;
        return null;
    }

    private static uint InvokeAdd(ExecutionContext context, IInvocableFunction add, uint a, uint b)
    {
        Span<StackValueItem> args = stackalloc StackValueItem[2];
        args[0] = new StackValueItem(a);
        args[1] = new StackValueItem(b);
        context.Invoke(add, args);
        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);
        return results[0].ExpectValueI32();
    }

    private static Imports CreateAsyncImports(AsyncExternalFunctionDelegate.InvokeDelegate invoke)
    {
        return new Imports
        {
            {
                "host", new ModuleExports
                {
                    {
                        "yield",
                        new AsyncExternalFunctionDelegate(invoke, null,
                            new FunctionType(Array.Empty<ValueType>(), Array.Empty<ValueType>()))
                    }
                }
            }
        };
    }

    [Test]
    public void FuelIsUnlimitedByDefault()
    {
        var instance = Utils.GetInstance(InfiniteLoop, new Imports());
        using var context = new ExecutionContext();

        Assert.That(context.Fuel, Is.Null);
        Assert.That(InvokeAdd(context, GetFunction(instance, "add"), 1, 2), Is.EqualTo(3));
        Assert.That(context.Fuel, Is.Null);
    }

    [Test]
    public void FuelExhaustionStopsInfiniteLoop()
    {
        var instance = Utils.GetInstance(InfiniteLoop, new Imports());
        using var context = new ExecutionContext();
        context.Fuel = 10_000;

        var ex = Assert.Catch(() => context.Invoke(GetFunction(instance, "run"), Span<StackValueItem>.Empty));
        Assert.That(FindException<FuelExhaustedException>(ex), Is.Not.Null, ex!.ToString());
        Assert.That(FindException<TrapException>(ex), Is.Not.Null);
        Assert.That(context.Fuel, Is.EqualTo(0));

        // the context is still usable after refilling
        context.Fuel = 1_000;
        Assert.That(InvokeAdd(context, GetFunction(instance, "add"), 1, 2), Is.EqualTo(3));
        Assert.That(context.Fuel, Is.LessThan(1_000));

        // and still traps without fuel
        context.Fuel = 0;
        Assert.That(() => InvokeAdd(context, GetFunction(instance, "add"), 1, 2), Throws.Exception);

        context.Fuel = null;
        Assert.That(InvokeAdd(context, GetFunction(instance, "add"), 1, 2), Is.EqualTo(3));
    }

    [Test]
    public void FuelCountsExecutedInstructions()
    {
        var instance = Utils.GetInstance(InfiniteLoop, new Imports());
        var add = GetFunction(instance, "add");
        using var context = new ExecutionContext();

        context.Fuel = 100;
        InvokeAdd(context, add, 1, 2);
        var consumed = 100 - context.Fuel!.Value;
        Assert.That(consumed, Is.GreaterThanOrEqualTo(3)); // local.get, local.get, i32.add (and end)

        // exactly the consumed amount is enough
        context.Fuel = consumed;
        Assert.That(InvokeAdd(context, add, 1, 2), Is.EqualTo(3));
        Assert.That(context.Fuel, Is.EqualTo(0));

        // one less is not
        context.Fuel = consumed - 1;
        Assert.That(() => InvokeAdd(context, add, 1, 2), Throws.Exception);
    }

    [Test]
    public async ValueTask FuelPerResumeLimitsInstructionsBetweenSuspensions()
    {
        var imports = CreateAsyncImports(static (_, _, _) => default);
        var instance = Utils.GetInstance("""
                                         (module
                                           (import "host" "yield" (func $yield))
                                           (func (export "run") (param $n i32)
                                             (loop $l
                                               call $yield
                                               local.get $n i32.const 1 i32.sub local.tee $n
                                               br_if $l))
                                         )
                                         """, imports);
        var run = GetFunction(instance, "run");

        var args = new[] { new StackValueItem(1000) };

        // more than 1000 instructions in total, but few between two suspensions
        using (var context = new ExecutionContext { FuelPerResume = 50 })
        {
            await context.InvokeAsync(run, args);
        }

        // the same amount without refill runs out
        using (var context = new ExecutionContext { Fuel = 50 })
        {
            Exception? caught = null;
            try
            {
                await context.InvokeAsync(run, args);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.That(FindException<FuelExhaustedException>(caught), Is.Not.Null);
        }

        // a busy loop without suspension runs out even with FuelPerResume
        var loop = Utils.GetInstance(InfiniteLoop, new Imports());
        using (var context = new ExecutionContext { FuelPerResume = 50 })
        {
            Exception? caught = null;
            try
            {
                await context.InvokeAsync(GetFunction(loop, "run"), ReadOnlySpan<StackValueItem>.Empty);
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.That(FindException<FuelExhaustedException>(caught), Is.Not.Null);
        }
    }

    [Test]
    public void CancellationStopsRunningLoop()
    {
        var instance = Utils.GetInstance(InfiniteLoop, new Imports());
        using var cts = new CancellationTokenSource();
        using var context = new ExecutionContext { CancellationToken = cts.Token };

        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
        Assert.That(() => context.Invoke(GetFunction(instance, "run"), Span<StackValueItem>.Empty),
            Throws.InstanceOf<OperationCanceledException>());

        // the context is still usable with another token
        context.CancellationToken = default;
        Assert.That(InvokeAdd(context, GetFunction(instance, "add"), 1, 2), Is.EqualTo(3));
    }

    [Test]
    public void CancellationRequestedBeforeInvocation()
    {
        var instance = Utils.GetInstance(InfiniteLoop, new Imports());
        using var context = new ExecutionContext { CancellationToken = new CancellationToken(true) };

        Assert.That(() => InvokeAdd(context, GetFunction(instance, "add"), 1, 2),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async ValueTask CancellationWhileSuspended()
    {
        var pending = new TaskCompletionSource();
        var imports = CreateAsyncImports((_, _, _) => new ValueTask(pending.Task));
        var instance = Utils.GetInstance("""
                                         (module
                                           (import "host" "yield" (func $yield))
                                           (func (export "run") call $yield)
                                           (func (export "add") (param i32 i32) (result i32)
                                             local.get 0 local.get 1 i32.add)
                                         )
                                         """, imports);

        using var cts = new CancellationTokenSource();
        using var context = new ExecutionContext { CancellationToken = cts.Token };

        var task = context.InvokeAsync(GetFunction(instance, "run"), ReadOnlySpan<StackValueItem>.Empty);
        Assert.That(task.IsCompleted, Is.False);

        cts.Cancel();

        Exception? caught = null;
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        Assert.That(caught, Is.InstanceOf<OperationCanceledException>());

        // the abandoned host function completes later without affecting the context
        pending.SetResult();

        context.CancellationToken = default;
        Assert.That(InvokeAdd(context, GetFunction(instance, "add"), 1, 2), Is.EqualTo(3));
    }

    [Test]
    public async ValueTask ComponentCancellationWhileSuspended()
    {
        var host = new LimitsHost();
        var component = Utils.GetComponent("""
                                           (component
                                             (import "test:limits/host" (instance $host (export "wait" (func))))
                                             (alias export $host "wait" (func $wait))
                                             (core func $wait_lowered (canon lower (func $wait)))
                                             (core module $M
                                               (import "host" "wait" (func $wait))
                                               (func (export "run") (loop $l call $wait br $l)))
                                             (core instance $hi (export "wait" (func $wait_lowered)))
                                             (core instance $m (instantiate $M (with "host" (instance $hi))))
                                             (func $run (canon lift (core func $m "run")))
                                             (export "run" (func $run))
                                           )
                                           """);
        var instance = component.Instantiate(new Dictionary<string, ISortedExportable>
        {
            { "test:limits/host", ILimitsHost.CreateWaaSInstance(host) }
        });
        Assert.That(instance.TryGetExport("run", out IFunction? run), Is.True);

        using var cts = new CancellationTokenSource();
        using var context = new ExecutionContext { CancellationToken = cts.Token };
        using var binder = run!.GetBinder(context);

        var task = binder.InvokeAsync(context);

        // let the guest call the host function a few times
        for (var i = 0; i < 3; i++)
        {
            Assert.That(task.IsCompleted, Is.False);
            host.Resume();
        }

        Assert.That(task.IsCompleted, Is.False);
        cts.Cancel();

        Exception? caught = null;
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        Assert.That(caught, Is.InstanceOf<OperationCanceledException>());
        Assert.That(host.Calls, Is.EqualTo(4));

        // the abandoned host function can complete later without affecting the host
        Assert.That(() => host.Resume(), Throws.Nothing);
    }

    [Test]
    public void MemoryGrowFailsAtHostLimit()
    {
        const string wat = """
                           (module
                             (memory 1)
                             (func (export "grow") (param i32) (result i32) local.get 0 memory.grow)
                           )
                           """;

        uint Grow(ExecutionContext context, IInvocableFunction grow, uint pages)
        {
            Span<StackValueItem> args = stackalloc StackValueItem[1];
            args[0] = new StackValueItem(pages);
            context.Invoke(grow, args);
            Span<StackValueItem> results = stackalloc StackValueItem[1];
            context.TakeResults(results);
            return results[0].ExpectValueI32();
        }

        {
            var instance = Utils.GetInstance(wat, new Imports());
            var grow = GetFunction(instance, "grow");
            using var context = new ExecutionContext { MaxMemoryPages = 3 };

            Assert.That(Grow(context, grow, 1), Is.EqualTo(1));
            Assert.That(Grow(context, grow, 1), Is.EqualTo(2));
            Assert.That(Grow(context, grow, 1), Is.EqualTo(uint.MaxValue));
            Assert.That(Grow(context, grow, 0), Is.EqualTo(3));
            Assert.That(instance.MemoryInstance.Memories.Span[0].NumPages, Is.EqualTo(3));
        }

        {
            // without the limit
            var instance = Utils.GetInstance(wat, new Imports());
            using var context = new ExecutionContext();
            Assert.That(Grow(context, GetFunction(instance, "grow"), 7), Is.EqualTo(1));
            Assert.That(instance.MemoryInstance.Memories.Span[0].NumPages, Is.EqualTo(8));
        }
    }

    [Test]
    public void MemoryTryGrowWithLimit()
    {
        using var memory = new Memory(new Limits(1));
        Assert.That(memory.TryGrow(1, 2), Is.True);
        Assert.That(memory.TryGrow(1, 2), Is.False);
        Assert.That(memory.TryGrow(1), Is.True);
        Assert.That(memory.NumPages, Is.EqualTo(3));
    }

    private class LimitsHost : ILimitsHost
    {
        private TaskCompletionSource? pending;
        public int Calls { get; private set; }

        public ValueTask Wait()
        {
            Calls++;
            pending = new TaskCompletionSource(TaskCreationOptions.None);
            return new ValueTask(pending.Task);
        }

        public void Resume()
        {
            var p = pending!;
            pending = null;
            p.SetResult();
        }
    }
}
