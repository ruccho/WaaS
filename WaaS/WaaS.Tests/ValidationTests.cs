using System.Text;
using WaaS.Models;
using WaaS.Runtime;
using WaaS.Runtime.Bindings;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

public class ValidationTests
{
    [Test]
    public void DeeplyNestedBlocksAreValidatedWithSmallStack()
    {
        // rustc emits deeply nested blocks for large match expressions
        const int depth = 20_000;
        var wat = new StringBuilder();
        wat.AppendLine("""(module (func (export "run") (result i32)""");
        for (var i = 0; i < depth; i++) wat.AppendLine(i % 2 == 0 ? "(block (result i32)" : "(loop (result i32)");
        wat.AppendLine("i32.const 42");
        // the innermost one is a loop, and the next one is a block
        wat.AppendLine("br 1");
        for (var i = 0; i < depth; i++) wat.Append(')');
        wat.AppendLine("))");
        var binary = Utils.GetModuleBinary(wat.ToString());

        uint? result = null;
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                var instance = new Instance(Module.Create(binary), new Imports());
                Assert.That(instance.TryGetExport("run", out IInvocableFunction? run), Is.True);
                using var context = new ExecutionContext();
                context.Invoke(run!, ReadOnlySpan<StackValueItem>.Empty);
                Span<StackValueItem> results = stackalloc StackValueItem[1];
                context.TakeResults(results);
                result = results[0].ExpectValueI32();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }, 256 * 1024);
        thread.Start();
        thread.Join();

        Assert.That(exception, Is.Null, exception?.ToString());
        Assert.That(result, Is.EqualTo(42));
    }
}
