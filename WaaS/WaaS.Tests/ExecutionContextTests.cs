using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

public class ExecutionContextTests
{
    [Test]
    public void ManyCallsDoNotExhaustNativeStack()
    {
        // Each returning call passes its results to the caller frame.
        // This must not consume native stack per call.
        var instance = Utils.GetInstance("""
                                         (module
                                           (func $id (param i32) (result i32) local.get 0)
                                           (func (export "run") (param $n i32) (result i32) (local $acc i32)
                                             (block $b (loop $l
                                               local.get $n i32.eqz br_if $b
                                               local.get $acc local.get $n call $id i32.add local.set $acc
                                               local.get $n i32.const 1 i32.sub local.set $n
                                               br $l))
                                             local.get $acc)
                                         )
                                         """, new Imports());

        Assert.That(instance.TryGetExport("run", out IInvocableFunction? run), Is.True);

        using var context = new ExecutionContext();
        Span<StackValueItem> args = stackalloc StackValueItem[1];
        args[0] = new StackValueItem(1_000_000);
        context.Invoke(run!, args);

        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);
        Assert.That(results[0].ExpectValueI32(), Is.EqualTo(unchecked((uint)(1_000_000L * 1_000_001L / 2))));
    }
}
