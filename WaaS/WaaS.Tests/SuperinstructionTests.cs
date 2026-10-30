using WaaS.Models;
using WaaS.Runtime;
using WaaS.Runtime.Bindings;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

/// <summary>
///     Tests the sequences of instructions executed at once by the interpreter.
/// </summary>
public class SuperinstructionTests
{
    // each sequence of the superinstructions appears at least once
    private static readonly string[] Body =
    {
        "loop $l",
        // local.get, local.get, i32.const
        "local.get $i", "local.get $acc", "i32.const 3", "i32.add", "i32.add", "local.set $a",
        // i32.store, i32.const, i32.const
        "i32.const 0", "local.get $a", "i32.store", "i32.const 4", "i32.const 7", "i32.store offset=4",
        // i32.const, i32.load, local.tee / i32.const, i32.and
        "i32.const 0", "i32.load", "local.tee $b", "i32.const 255", "i32.and",
        // local.get, i32.add, local.tee / local.tee, i32.const
        "local.get $b", "i32.add", "local.tee $a", "local.tee $b", "i32.const 3", "i32.add", "local.set $c",
        // i32.shl, i32.add
        "local.get $acc", "local.get $i", "i32.const 2", "i32.shl", "i32.add", "local.set $acc",
        // i32.shr_u, local.tee
        "local.get $a", "i32.const 1", "i32.shr_u", "local.tee $b", "drop",
        // i32.load, local.tee
        "local.get $i", "i32.const 3", "i32.and", "i32.const 2", "i32.shl", "i32.load", "local.tee $a", "drop",
        // local.get, local.get, local.get
        "local.get $a", "local.get $b", "local.get $i", "i32.add", "i32.add", "local.get $acc", "i32.add",
        "local.set $acc",
        // i32.store, local.get, local.get
        "i32.const 12", "local.get $acc", "i32.store", "local.get $a", "local.get $b", "i32.xor", "local.set $a",
        // i32.ne, br_if / i32.gt_u, br_if / i32.lt_u, br_if
        "block $x", "local.get $a", "local.get $b", "i32.ne", "br_if $x", "local.get $acc", "i32.const 1", "i32.add",
        "local.set $acc", "end",
        "block $y", "local.get $a", "local.get $b", "i32.gt_u", "br_if $y", "local.get $acc", "i32.const 5",
        "i32.add", "local.set $acc", "end",
        "block $z", "local.get $a", "local.get $b", "i32.lt_u", "br_if $z", "local.get $acc", "i32.const 9",
        "i32.add", "local.set $acc", "end",
        // the loop variants
        "local.get $b", "i32.const 3", "i32.add", "local.set $c",
        "loop $m", "local.get $acc", "i32.const 1", "i32.add", "local.set $acc", "local.get $b", "i32.const 1",
        "i32.add", "local.tee $b", "local.get $c", "i32.ne", "br_if $m", "end",
        "loop $k", "local.get $acc", "i32.const 1", "i32.xor", "local.set $acc", "local.get $c", "i32.const 1",
        "i32.sub", "local.tee $c", "local.get $b", "i32.const 2", "i32.sub", "i32.gt_u", "br_if $k", "end",
        "local.get $i", "i32.const 1", "i32.add", "local.tee $i", "local.get $n", "i32.lt_u", "br_if $l",
        "end",
        "local.get $acc", "i32.const 0", "i32.load", "i32.add", "i32.const 8", "i32.load", "i32.add",
        "i32.const 12", "i32.load", "i32.add"
    };

    private static uint Run(string separator)
    {
        var wat = $$"""
                    (module
                      (memory 1)
                      (func (export "run") (param $n i32) (result i32)
                        (local $i i32) (local $a i32) (local $b i32) (local $c i32) (local $acc i32)
                        {{string.Join(separator, Body)}}))
                    """;
        var instance = Utils.GetInstance(wat, new Imports());
        Assert.That(instance.TryGetExport("run", out IInvocableFunction? run), Is.True);

        using var context = new ExecutionContext();
        context.Invoke(run!, new[] { new StackValueItem(20u) });
        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);
        return results[0].ExpectValueI32();
    }

    [Test]
    public void SuperinstructionsExecuteAsInstructions()
    {
        // the nops between the instructions prevent the superinstructions
        Assert.That(Run("\n"), Is.EqualTo(Run("\nnop\n")));
    }

    [TestCase("i32.const 0x10000 i32.load local.tee 1", (byte)0x28)]
    [TestCase("local.get 0 i32.load local.tee 1", (byte)0x28)]
    [TestCase("local.get 0 i32.const 1 i32.store i32.const 0 i32.const 0 i32.store local.get 1", (byte)0x36)]
    [TestCase("local.get 0 i32.const 1 i32.store local.get 0 local.get 1 i32.store local.get 1", (byte)0x36)]
    public void TrapInSuperinstructionPointsToMemoryAccess(string body, byte opcode)
    {
        var binary = Utils.GetModuleBinary($$"""
                                             (module
                                               (memory 1)
                                               (func (export "run") (param i32) (result i32) (local i32)
                                                 {{body}}))
                                             """);
        var instance = new Instance(Module.Create(binary), new Imports());
        Assert.That(instance.TryGetExport("run", out IInvocableFunction? run), Is.True);

        using var context = new ExecutionContext();
        var exception = Assert.Catch(() => context.Invoke(run!, new[] { new StackValueItem(0x10000u) }));
        var frames = WasmStackTrace.FromException(exception)!.Frames;
        Assert.That(binary[frames[0].Offset!.Value], Is.EqualTo(opcode));
    }
}
