using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

public class SaturatingTruncationTests
{
    private static ulong Run(string instruction, string paramType, StackValueItem argument)
    {
        var instance = Utils.GetInstance($$"""
                                           (module
                                             (func (export "run") (param {{paramType}}) (result i64)
                                               local.get 0
                                               {{instruction}}
                                               {{(instruction.StartsWith("i32") ? "i64.extend_i32_u" : "")}}))
                                           """, new Imports());
        Assert.That(instance.TryGetExport("run", out IInvocableFunction? run), Is.True);

        using var context = new ExecutionContext();
        Span<StackValueItem> args = [argument];
        context.Invoke(run!, args);

        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);
        return results[0].ExpectValueI64();
    }

    [Test]
    public void NaNIsConvertedToZero()
    {
        Assert.That(Run("i32.trunc_sat_f32_s", "f32", new StackValueItem(float.NaN)), Is.EqualTo(0));
        Assert.That(Run("i64.trunc_sat_f64_u", "f64", new StackValueItem(double.NaN)), Is.EqualTo(0));
    }

    [Test]
    public void OutOfRangeValuesAreClamped()
    {
        Assert.That(Run("i32.trunc_sat_f32_s", "f32", new StackValueItem(3e9f)), Is.EqualTo((ulong)int.MaxValue));
        Assert.That(Run("i32.trunc_sat_f64_s", "f64", new StackValueItem(-3e9)),
            Is.EqualTo((ulong)unchecked((uint)int.MinValue)));
        Assert.That(Run("i32.trunc_sat_f64_u", "f64", new StackValueItem(-1.0)), Is.EqualTo(0));
        Assert.That(Run("i32.trunc_sat_f32_u", "f32", new StackValueItem(float.PositiveInfinity)),
            Is.EqualTo((ulong)uint.MaxValue));
        Assert.That(Run("i64.trunc_sat_f64_s", "f64", new StackValueItem(1e19)), Is.EqualTo((ulong)long.MaxValue));
        Assert.That(Run("i64.trunc_sat_f32_s", "f32", new StackValueItem(float.NegativeInfinity)),
            Is.EqualTo(unchecked((ulong)long.MinValue)));
        Assert.That(Run("i64.trunc_sat_f32_u", "f32", new StackValueItem(1e20f)), Is.EqualTo(ulong.MaxValue));
    }

    [Test]
    public void InRangeValuesAreTruncated()
    {
        Assert.That(Run("i32.trunc_sat_f64_s", "f64", new StackValueItem(-2.9)), Is.EqualTo((ulong)unchecked((uint)-2)));
        Assert.That(Run("i64.trunc_sat_f64_u", "f64", new StackValueItem(12345.9)), Is.EqualTo(12345));
    }
}
