using WaaS.Models;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

/// <summary>
///     Regression tests for the problems found by fuzzing (WaaS.Fuzz).
/// </summary>
public class FuzzingFindingsTests
{
    private const string Wat = """
                               (module
                                 (memory 1)
                                 (func (export "div_s") (param i32 i32) (result i32) (i32.div_s (local.get 0) (local.get 1)))
                                 (func (export "div_u") (param i32 i32) (result i32) (i32.div_u (local.get 0) (local.get 1)))
                                 (func (export "rem_s") (param i32 i32) (result i32) (i32.rem_s (local.get 0) (local.get 1)))
                                 (func (export "div_s64") (param i64 i64) (result i64) (i64.div_s (local.get 0) (local.get 1)))
                                 (func (export "rem_u64") (param i64 i64) (result i64) (i64.rem_u (local.get 0) (local.get 1)))
                                 (func (export "trunc_f32_s") (param f32) (result i32) (i32.trunc_f32_s (local.get 0)))
                                 (func (export "trunc_f64_u") (param f64) (result i32) (i32.trunc_f64_u (local.get 0)))
                                 (func (export "trunc_f64_s64") (param f64) (result i64) (i64.trunc_f64_s (local.get 0)))
                                 (func (export "grow") (param i32) (result i32) (memory.grow (local.get 0)))
                                 (func (export "copy") (param i32 i32 i32) (memory.copy (local.get 0) (local.get 1) (local.get 2)))
                                 (func (export "fill") (param i32 i32) (memory.fill (local.get 0) (i32.const 0) (local.get 1)))
                               )
                               """;

    private static readonly Instance Instance = new(Module.Create(Utils.GetModuleBinary(Wat)), new Imports());

    private static StackValueItem Invoke(string name, params StackValueItem[] args)
    {
        Assert.That(Instance.TryGetExport(name, out IInvocableFunction? function), Is.True);
        using var context = new ExecutionContext();
        context.Invoke(function!, args);
        if (context.ResultLength == 0) return default;
        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);
        return results[0];
    }

    private static void AssertTrap(string message, string name, params StackValueItem[] args)
    {
        var exception = Assert.Throws<WasmException>(() => Invoke(name, args));
        Assert.That(exception!.InnerException, Is.InstanceOf<TrapException>());
        Assert.That(exception.InnerException!.Message, Is.EqualTo(message));
    }

    [Test]
    public void IntegerDivisionTraps()
    {
        AssertTrap("integer divide by zero", "div_s", new StackValueItem(1), new StackValueItem(0));
        AssertTrap("integer divide by zero", "div_u", new StackValueItem(1), new StackValueItem(0));
        AssertTrap("integer divide by zero", "rem_s", new StackValueItem(1), new StackValueItem(0));
        AssertTrap("integer divide by zero", "rem_u64", new StackValueItem(1L), new StackValueItem(0L));
        AssertTrap("integer overflow", "div_s", new StackValueItem(int.MinValue), new StackValueItem(-1));
        AssertTrap("integer overflow", "div_s64", new StackValueItem(long.MinValue), new StackValueItem(-1L));
    }

    [Test]
    public void DivisionOfMinimumValueByOne()
    {
        // the result is the minimum value, which is not an overflow
        Assert.That(Invoke("div_s", new StackValueItem(int.MinValue), new StackValueItem(1)).ExpectValueI32(),
            Is.EqualTo(unchecked((uint)int.MinValue)));
        Assert.That(Invoke("div_s64", new StackValueItem(long.MinValue), new StackValueItem(1L)).ExpectValueI64(),
            Is.EqualTo(unchecked((ulong)long.MinValue)));
    }

    [Test]
    public void TruncationTraps()
    {
        AssertTrap("invalid conversion to integer", "trunc_f32_s", new StackValueItem(float.NaN));
        AssertTrap("integer overflow", "trunc_f32_s", new StackValueItem(2147483648f));
        AssertTrap("integer overflow", "trunc_f64_u", new StackValueItem(-1.0));
        AssertTrap("integer overflow", "trunc_f64_s64", new StackValueItem(9223372036854775808.0));

        Assert.That(Invoke("trunc_f64_u", new StackValueItem(-0.9)).ExpectValueI32(), Is.EqualTo(0));
        Assert.That(Invoke("trunc_f64_s64", new StackValueItem(-9223372036854775808.0)).ExpectValueI64(),
            Is.EqualTo(unchecked((ulong)long.MinValue)));
    }

    [Test]
    public void GrowingMemoryBeyondTheLimitFails()
    {
        Assert.That(Invoke("grow", new StackValueItem(uint.MaxValue)).ExpectValueI32(), Is.EqualTo(uint.MaxValue));
        Assert.That(Invoke("grow", new StackValueItem(0x10000u)).ExpectValueI32(), Is.EqualTo(uint.MaxValue));
    }

    [Test]
    public void BulkMemoryOutOfBoundsTraps()
    {
        AssertTrap("out of bounds memory access", "copy", new StackValueItem(0), new StackValueItem(uint.MaxValue),
            new StackValueItem(2));
        AssertTrap("out of bounds memory access", "fill", new StackValueItem(uint.MaxValue), new StackValueItem(2));
    }

    [Test]
    public void MemoryLargerThanArraysCannotBeCreated()
    {
        var module = Module.Create(Utils.GetModuleBinary("(module (memory 40000))"));
        Assert.Throws<NotSupportedException>(() => _ = new Instance(module, new Imports()));
    }

    [Test]
    public void MalformedModulesThrowInvalidModuleException()
    {
        var binary = Utils.GetModuleBinary("""(module (func (export "f") (result i32) i32.const 1))""");

        // an unknown opcode, an unknown section id and a duplicated export
        foreach (var malformed in new[]
                 {
                     Replace(binary, [0x41, 0x01, 0x0B], [0xE4, 0x01, 0x0B]),
                     [..binary, 0x20, 0x00],
                     Utils.GetModuleBinary("""(module (func) (export "f" (func 0)) (export "f" (func 0)))""")
                 })
            Assert.Throws<InvalidModuleException>(() => Module.Create(malformed));
    }

    private static byte[] Replace(byte[] source, byte[] pattern, byte[] replacement)
    {
        var index = source.AsSpan().IndexOf(pattern);
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        var result = (byte[])source.Clone();
        replacement.CopyTo(result, index);
        return result;
    }
}
