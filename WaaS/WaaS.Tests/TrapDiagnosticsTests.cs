using WaaS.Models;
using WaaS.Runtime;
using WaaS.Runtime.Bindings;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

public class TrapDiagnosticsTests
{
    private const string NestedCallsWat = """
                                          (module $diagnostics
                                            (memory 1)
                                            (func $inner (param $kind i32) (result i32)
                                              (block $b
                                                local.get $kind
                                                br_if $b
                                                unreachable)
                                              local.get $kind
                                              i32.const 1
                                              i32.eq
                                              if
                                                i32.const 1
                                                i32.const 0
                                                i32.div_s
                                                drop
                                              end
                                              i32.const 0x10000
                                              i32.load)
                                            (func $middle (param i32) (result i32)
                                              local.get 0
                                              call $inner)
                                            (func $outer (export "run") (param i32) (result i32)
                                              nop
                                              local.get 0
                                              call $middle)
                                          )
                                          """;

    private static (Module module, byte[] binary, IInvocableFunction run) Load(string wat, Imports? imports = null)
    {
        var binary = Utils.GetModuleBinary(wat);
        var module = Module.Create(binary);
        var instance = new Instance(module, imports ?? new Imports());
        Assert.That(instance.TryGetExport("run", out IInvocableFunction? run), Is.True);
        return (module, binary, run!);
    }

    private static Exception Run(IInvocableFunction run, uint argument, uint? maxStackFrames = null)
    {
        using var context = new ExecutionContext(maxStackFrames);
        var arguments = new[] { new StackValueItem(argument) };
        return Assert.Catch(() => context.Invoke(run, arguments))!;
    }

    [Test]
    public void UnreachableTrapHasStackTrace()
    {
        var (_, binary, run) = Load(NestedCallsWat);
        var exception = Run(run, 0);

        Assert.That(exception, Is.InstanceOf<WasmException>());
        Assert.That(exception.InnerException, Is.InstanceOf<TrapException>());

        var trace = WasmStackTrace.FromException(exception);
        Assert.That(trace, Is.Not.Null);
        Assert.That(trace, Is.SameAs(((WasmException)exception).WasmStackTrace));

        var frames = trace!.Frames;
        Assert.That(frames.Select(f => f.FunctionName), Is.EqualTo(new[] { "inner", "middle", "outer" }));
        Assert.That(frames.Select(f => f.FunctionIndex), Is.EqualTo(new uint?[] { 0, 1, 2 }));
        Assert.That(frames.All(f => !f.IsHost && f.ModuleName == "diagnostics"), Is.True);

        // the innermost frame points to the trapping instruction, the others point to the calls
        Assert.That(binary[frames[0].Offset!.Value], Is.EqualTo(0x00 /* unreachable */));
        Assert.That(binary[frames[1].Offset!.Value], Is.EqualTo(0x10 /* call */));
        Assert.That(binary[frames[2].Offset!.Value], Is.EqualTo(0x10 /* call */));
        Assert.That(frames[1].InstructionIndex, Is.EqualTo(1));
        Assert.That(frames[2].InstructionIndex, Is.EqualTo(2));

        var message = exception.Message;
        TestContext.Out.WriteLine(exception);
        Assert.That(message, Does.Contain("wasm backtrace:"));
        Assert.That(message, Does.Contain($"0x{frames[0].Offset:x} - diagnostics!inner"));
        Assert.That(message.IndexOf("!inner", StringComparison.Ordinal),
            Is.LessThan(message.IndexOf("!middle", StringComparison.Ordinal)));
        Assert.That(message.IndexOf("!middle", StringComparison.Ordinal),
            Is.LessThan(message.IndexOf("!outer", StringComparison.Ordinal)));
    }

    [Test]
    public void IntegerDivisionTrapHasStackTrace()
    {
        var (_, binary, run) = Load(NestedCallsWat);
        var exception = Run(run, 1);

        var frames = WasmStackTrace.FromException(exception)!.Frames;
        Assert.That(frames.Select(f => f.FunctionName), Is.EqualTo(new[] { "inner", "middle", "outer" }));
        Assert.That(binary[frames[0].Offset!.Value], Is.EqualTo(0x6D /* i32.div_s */));
    }

    [Test]
    public void OutOfBoundsTrapHasStackTrace()
    {
        var (_, binary, run) = Load(NestedCallsWat);
        var exception = Run(run, 2);

        var frames = WasmStackTrace.FromException(exception)!.Frames;
        Assert.That(frames.Select(f => f.FunctionName), Is.EqualTo(new[] { "inner", "middle", "outer" }));
        Assert.That(binary[frames[0].Offset!.Value], Is.EqualTo(0x28 /* i32.load */));
    }

    [Test]
    public void StackOverflowHasStackTrace()
    {
        var (_, _, run) = Load("""
                               (module
                                 (func $recurse (export "run") (param i32) (result i32)
                                   local.get 0
                                   call $recurse)
                               )
                               """);
        var exception = Run(run, 0, 16);

        var frames = WasmStackTrace.FromException(exception)!.Frames;
        Assert.That(frames, Has.Count.EqualTo(16));
        Assert.That(frames.All(f => f.FunctionName == "recurse"), Is.True);
        // the innermost frame failed to push the next frame
        Assert.That(frames[0].InstructionIndex, Is.EqualTo(1));
    }

    [Test]
    public void HostExceptionHasStackTrace()
    {
        var imports = new Imports
        {
            {
                "env", new ModuleExports
                {
                    {
                        "fail",
                        CoreBinder.Instance.ToExternalFunction((int value) =>
                            value >= 0 ? throw new InvalidOperationException("host failure") : value)
                    }
                }
            }
        };

        var (_, _, run) = Load("""
                               (module
                                 (import "env" "fail" (func $fail (param i32) (result i32)))
                                 (func $guest (param i32) (result i32)
                                   local.get 0
                                   call $fail)
                                 (func (export "run") (param i32) (result i32)
                                   local.get 0
                                   call $guest)
                               )
                               """, imports);
        var exception = Run(run, 0);

        // the exception from the host is not wrapped
        Assert.That(exception, Is.TypeOf<InvalidOperationException>());
        Assert.That(exception.Message, Is.EqualTo("host failure"));

        var frames = WasmStackTrace.FromException(exception)!.Frames;
        Assert.That(frames, Has.Count.EqualTo(3));
        Assert.That(frames[0].IsHost, Is.True);
        Assert.That(frames[1].FunctionName, Is.EqualTo("guest"));
        // the imported function occupies the index 0
        Assert.That(frames[1].FunctionIndex, Is.EqualTo(1));
        Assert.That(frames[2].FunctionName, Is.Null);
        Assert.That(frames[2].FunctionIndex, Is.EqualTo(2));
        Assert.That(frames[2].ToString(), Does.Contain("<wasm function 2>"));
    }

    [Test]
    public void ModuleWithoutNameSectionHasStackTrace()
    {
        var binary = Utils.GetModuleBinary("""
                                           (module
                                             (func (export "run") (param i32) (result i32)
                                               unreachable)
                                           )
                                           """);
        // no name section is emitted without identifiers
        var module = Module.Create(binary);
        Assert.That(module.Names.FunctionNames, Is.Empty);

        var instance = new Instance(module, new Imports());
        Assert.That(instance.TryGetExport("run", out IInvocableFunction? run), Is.True);
        var exception = Run(run!, 0);

        var frames = WasmStackTrace.FromException(exception)!.Frames;
        Assert.That(frames, Has.Count.EqualTo(1));
        Assert.That(frames[0].FunctionName, Is.Null);
        Assert.That(frames[0].ModuleName, Is.Null);
        Assert.That(binary[frames[0].Offset!.Value], Is.EqualTo(0x00 /* unreachable */));
    }

    [Test]
    public void ContextIsReusableAfterTrap()
    {
        var (_, _, run) = Load(NestedCallsWat);

        using var context = new ExecutionContext();
        Assert.Catch(() => context.Invoke(run, new[] { new StackValueItem(0u) }));

        // a valid call after the trap succeeds
        var (_, _, ok) = Load("""
                              (module
                                (func (export "run") (param i32) (result i32)
                                  local.get 0)
                              )
                              """);
        context.Invoke(ok, new[] { new StackValueItem(7u) });
        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);
        Assert.That(results[0].ExpectValueI32(), Is.EqualTo(7));
    }
}
