using WaaS.Models;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

public class DebuggerTests
{
    private const string Wat = """
                               (module
                                 (memory 1)
                                 (func $fib (export "fib") (param i32) (result i32)
                                   (if (result i32) (i32.lt_u (local.get 0) (i32.const 2))
                                     (then (local.get 0))
                                     (else (i32.add
                                       (call $fib (i32.sub (local.get 0) (i32.const 1)))
                                       (call $fib (i32.sub (local.get 0) (i32.const 2)))))))
                                 (func (export "sum") (param i32) (result i32) (local $i i32) (local $sum i32)
                                   (loop $l
                                     (i32.store (i32.mul (local.get $i) (i32.const 4)) (local.get $i))
                                     (local.set $sum (i32.add (local.get $sum) (i32.load (i32.mul (local.get $i) (i32.const 4)))))
                                     (local.set $i (i32.add (local.get $i) (i32.const 1)))
                                     (br_if $l (i32.lt_u (local.get $i) (local.get 0))))
                                   (local.get $sum))
                               )
                               """;

    private static readonly Instance Instance = new(Module.Create(Utils.GetModuleBinary(Wat)), new Imports());

    private static IInvocableFunction Export(string name)
    {
        Assert.That(Instance.TryGetExport(name, out IInvocableFunction? function), Is.True);
        return function!;
    }

    private static async Task<uint> InvokeAsync(ExecutionContext context, string name, uint argument)
    {
        await context.InvokeAsync(Export(name), [new StackValueItem(argument)]);
        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);
        return results[0].ExpectValueI32();
    }

    [TestCase("fib", 10u, 55u)]
    [TestCase("sum", 100u, 4950u)]
    public async Task PausingAtEveryInstructionKeepsTheResult(string name, uint argument, uint expected)
    {
        var debugger = new RecordingDebugger { PauseAlways = true };
        using var context = new ExecutionContext { Debugger = debugger };

        Assert.That(await InvokeAsync(context, name, argument), Is.EqualTo(expected));
        Assert.That(debugger.Pauses, Is.EqualTo(debugger.Locations.Count));
        Assert.That(debugger.Pauses, Is.GreaterThan(100));
    }

    [Test]
    public async Task EveryInstructionIsObservedOnce()
    {
        // the number of the instructions is the same as the fuel consumed without a debugger
        using var reference = new ExecutionContext { Fuel = 1_000_000 };
        await InvokeAsync(reference, "fib", 10);
        var executed = 1_000_000 - (long)reference.Fuel!.Value;

        var debugger = new RecordingDebugger();
        using var context = new ExecutionContext { Debugger = debugger };
        await InvokeAsync(context, "fib", 10);

        Assert.That(debugger.Locations.Count, Is.EqualTo(executed));
    }

    [Test]
    public async Task BreakpointShowsTheFrames()
    {
        // pauses at the first call in $fib with the parameter 2, which is called from $fib with 3
        var fib = Instance.Module.InternalFunctions.Span[0];
        var call = fib.Body.Instructions.Span.ToArray().OfType<Call>().First();
        WasmDebugPause? paused = null;
        var debugger = new RecordingDebugger
        {
            Condition = location => paused == null && location.Instruction == call &&
                                    location.Depth == 2,
            OnPause = pause =>
            {
                paused = pause;
                // resumed from another thread
                Task.Run(pause.Resume);
            }
        };
        using var context = new ExecutionContext { Debugger = debugger };

        Assert.That(await InvokeAsync(context, "fib", 3), Is.EqualTo(2));
        Assert.That(paused, Is.Not.Null);

        var frames = paused!.Frames;
        Assert.That(frames.Select(f => f.Function.function), Is.EqualTo(new[] { fib, fib }));
        Assert.That(frames.Select(f => f.Locals.Span[0].ExpectValueI32()), Is.EqualTo(new uint[] { 2, 3 }));
        Assert.That(frames[0].Instruction, Is.SameAs(call));
        // the argument of the call, 2 - 1, is on the operand stack
        Assert.That(frames[0].OperandStack.ToArray().Select(v => v.ExpectValueI32()), Is.EqualTo(new uint[] { 1 }));
        Assert.That(frames[1].Instruction, Is.InstanceOf<Call>());
    }

    [Test]
    public void SynchronousInvocationDoesNotPause()
    {
        var debugger = new RecordingDebugger { PauseAlways = true };
        using var context = new ExecutionContext { Debugger = debugger };
        context.Invoke(Export("sum"), [new StackValueItem(10u)]);
        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);

        Assert.That(results[0].ExpectValueI32(), Is.EqualTo(45));
        Assert.That(debugger.Pauses, Is.Zero);
        Assert.That(debugger.Locations, Is.Not.Empty);
    }

    [Test]
    public async Task FuelIsConsumedWhileDebugging()
    {
        using var context = new ExecutionContext { Fuel = 50, Debugger = new RecordingDebugger() };
        Assert.That(context.Fuel, Is.EqualTo(50));
        Exception? exception = null;
        try
        {
            await InvokeAsync(context, "sum", 100);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // the exception is wrapped with the location in WebAssembly
        Assert.That(exception, Is.InstanceOf<FuelExhaustedException>().Or.InnerException.InstanceOf<FuelExhaustedException>());
        Assert.That(context.Fuel, Is.EqualTo(0));

        // detaching keeps the fuel of the host
        context.Fuel = 1_000_000;
        context.Debugger = null;
        Assert.That(context.Fuel, Is.EqualTo(1_000_000));
        Assert.That(await InvokeAsync(context, "sum", 100), Is.EqualTo(4950));
    }

    private sealed class RecordingDebugger : IWasmDebugger
    {
        public readonly List<WasmDebugLocation> Locations = new();
        public bool PauseAlways { get; init; }
        public Func<WasmDebugLocation, bool>? Condition { get; init; }
        public Action<WasmDebugPause>? OnPause { get; init; }
        public int Pauses { get; private set; }

        public bool ShouldPause(in WasmDebugLocation location)
        {
            Locations.Add(location);
            return PauseAlways || (Condition?.Invoke(location) ?? false);
        }

        public void OnPaused(WasmDebugPause pause)
        {
            Pauses++;
            if (OnPause != null) OnPause(pause);
            else pause.Resume();
        }
    }
}
