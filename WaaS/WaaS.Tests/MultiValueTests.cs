using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

public class MultiValueTests
{
    private static uint[] Run(string wat, params uint[] arguments)
    {
        var instance = Utils.GetInstance(wat, new Imports());
        Assert.That(instance.TryGetExport("run", out IInvocableFunction? run), Is.True);

        using var context = new ExecutionContext();
        Span<StackValueItem> args = stackalloc StackValueItem[arguments.Length];
        for (var i = 0; i < arguments.Length; i++) args[i] = new StackValueItem(arguments[i]);
        context.Invoke(run!, args);

        Span<StackValueItem> results = stackalloc StackValueItem[run!.Type.ResultTypes.Length];
        context.TakeResults(results);

        var values = new uint[results.Length];
        for (var i = 0; i < results.Length; i++) values[i] = results[i].ExpectValueI32();
        return values;
    }

    [Test]
    public void FunctionReturnsMultipleValues()
    {
        var results = Run("""
                          (module
                            (func $swap (param i32 i32) (result i32 i32) local.get 1 local.get 0)
                            (func (export "run") (param i32 i32) (result i32 i32)
                              local.get 0 local.get 1 call $swap)
                          )
                          """, 1, 2);

        Assert.That(results, Is.EqualTo(new uint[] { 2, 1 }));
    }

    [Test]
    public void BlockTakesParametersAndReturnsResults()
    {
        var results = Run("""
                          (module
                            (func (export "run") (param i32 i32) (result i32 i32 i32)
                              i32.const 100
                              local.get 0 local.get 1
                              (block (param i32 i32) (result i32 i32)
                                i32.add
                                i32.const 7))
                          )
                          """, 3, 4);

        Assert.That(results, Is.EqualTo(new uint[] { 100, 7, 7 }));
    }

    [Test]
    public void BranchCarriesMultipleValues()
    {
        var results = Run("""
                          (module
                            (func (export "run") (param i32) (result i32 i32)
                              (block $b (result i32 i32)
                                i32.const 1 i32.const 2
                                local.get 0 br_if $b
                                drop drop
                                i32.const 3 i32.const 4))
                          )
                          """, 0);

        Assert.That(results, Is.EqualTo(new uint[] { 3, 4 }));
    }

    [Test]
    public void BranchTableCarriesMultipleValues()
    {
        const string wat = """
                           (module
                             (func (export "run") (param i32) (result i32 i32)
                               (block $outer (result i32 i32)
                                 (block $inner (result i32 i32)
                                   i32.const 10 i32.const 20
                                   local.get 0
                                   br_table $inner $outer)
                                 drop i32.const 0))
                           )
                           """;

        Assert.That(Run(wat, 0), Is.EqualTo(new uint[] { 10, 0 }));
        Assert.That(Run(wat, 1), Is.EqualTo(new uint[] { 10, 20 }));
    }

    [Test]
    public void LoopTakesParametersOnBranch()
    {
        // sum of 1..n, carried as the loop parameters (counter, accumulator)
        var results = Run("""
                          (module
                            (func (export "run") (param $n i32) (result i32) (local $c i32) (local $acc i32)
                              local.get $n i32.const 0
                              (loop $l (param i32 i32) (result i32)
                                local.set $acc local.set $c
                                local.get $c i32.const 1 i32.sub
                                local.get $acc local.get $c i32.add
                                local.get $c i32.const 1 i32.ne
                                br_if $l
                                local.set $acc drop local.get $acc))
                          )
                          """, 10);

        Assert.That(results, Is.EqualTo(new uint[] { 55 }));
    }

    [Test]
    public void IfTakesParameters()
    {
        const string wat = """
                           (module
                             (func (export "run") (param i32) (result i32)
                               i32.const 5 i32.const 3
                               local.get 0
                               (if (param i32 i32) (result i32)
                                 (then i32.add)
                                 (else i32.sub)))
                           )
                           """;

        Assert.That(Run(wat, 1), Is.EqualTo(new uint[] { 8 }));
        Assert.That(Run(wat, 0), Is.EqualTo(new uint[] { 2 }));
    }

    [Test]
    public void IfWithoutElsePassesParametersThrough()
    {
        const string wat = """
                           (module
                             (func (export "run") (param i32) (result i32)
                               i32.const 5
                               local.get 0
                               (if (param i32) (result i32)
                                 (then i32.const 1 i32.add)))
                           )
                           """;

        Assert.That(Run(wat, 1), Is.EqualTo(new uint[] { 6 }));
        Assert.That(Run(wat, 0), Is.EqualTo(new uint[] { 5 }));
    }

    [Test]
    public void BlockTypeIndexIsEncodedInMultipleBytes()
    {
        // type indices of 64 and above take two bytes as s33
        var types = string.Concat(Enumerable.Range(0, 70).Select(i => $"(type (func (param {string.Join(" ", Enumerable.Repeat("i64", i % 8))})))\n"));
        var results = Run($$"""
                            (module
                              {{types}}
                              (type $t (func (param i32) (result i32 i32)))
                              (func (export "run") (param i32) (result i32 i32)
                                local.get 0
                                (block (type $t)
                                  i32.const 1))
                            )
                            """, 9);

        Assert.That(results, Is.EqualTo(new uint[] { 9, 1 }));
    }

    [Test]
    public void IfWithoutElseRejectsDifferentResults()
    {
        Assert.Throws<InvalidCodeException>(() => Utils.GetInstance("""
                                                                    (module
                                                                      (func (param i32) (result i32)
                                                                        i32.const 5
                                                                        local.get 0
                                                                        (if (param i32) (result i32 i32)
                                                                          (then i32.const 1))))
                                                                    """, new Imports()));
    }
}
