using WaaS.Models;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

/// <summary>
///     Tests the instructions that the interpreter executes with dedicated operations.
/// </summary>
public class DedicatedOperationTests
{
    private static ulong Call(string body, string parameters, string result, params StackValueItem[] arguments)
    {
        var function = Utils.GetInstance(
            $$"""
              (module
                (func (export "run") {{parameters}} (result {{result}})
                  {{body}}))
              """, new Imports()).ExportInstance.Items["run"] as IInvocableFunction;

        using var context = new ExecutionContext();
        context.Invoke(function!, arguments);
        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);
        return result == "i32" ? results[0].ExpectValueI32() : results[0].ExpectValueI64();
    }

    private static StackValueItem I32(uint value)
    {
        return new StackValueItem(value);
    }

    private static StackValueItem I64(ulong value)
    {
        return new StackValueItem(value);
    }

    [TestCase(7u, 2u, 3u)]
    [TestCase(uint.MaxValue, 2u, 0x7FFFFFFFu)]
    [TestCase(1u, uint.MaxValue, 0u)]
    public void I32DivU(uint lhs, uint rhs, uint expected)
    {
        Assert.That(Call("local.get 0 local.get 1 i32.div_u", "(param i32 i32)", "i32", I32(lhs), I32(rhs)),
            Is.EqualTo(expected));
    }

    [TestCase(7u, 2u, 1u)]
    [TestCase(uint.MaxValue, 16u, 15u)]
    public void I32RemU(uint lhs, uint rhs, uint expected)
    {
        Assert.That(Call("local.get 0 local.get 1 i32.rem_u", "(param i32 i32)", "i32", I32(lhs), I32(rhs)),
            Is.EqualTo(expected));
    }

    [TestCase(7ul, 2ul, 3ul)]
    [TestCase(ulong.MaxValue, 2ul, 0x7FFFFFFFFFFFFFFFul)]
    public void I64DivU(ulong lhs, ulong rhs, ulong expected)
    {
        Assert.That(Call("local.get 0 local.get 1 i64.div_u", "(param i64 i64)", "i64", I64(lhs), I64(rhs)),
            Is.EqualTo(expected));
    }

    [TestCase(7ul, 2ul, 1ul)]
    [TestCase(ulong.MaxValue, 256ul, 255ul)]
    public void I64RemU(ulong lhs, ulong rhs, ulong expected)
    {
        Assert.That(Call("local.get 0 local.get 1 i64.rem_u", "(param i64 i64)", "i64", I64(lhs), I64(rhs)),
            Is.EqualTo(expected));
    }

    [TestCase("i32.div_u", "i32")]
    [TestCase("i32.rem_u", "i32")]
    [TestCase("i64.div_u", "i64")]
    [TestCase("i64.rem_u", "i64")]
    public void DivisionByZeroTraps(string instruction, string type)
    {
        var body = $"local.get 0 local.get 1 {instruction}";
        var parameters = $"(param {type} {type})";
        var arguments = type == "i32" ? new[] { I32(1), I32(0) } : new[] { I64(1), I64(0) };

        var exception = Assert.Catch(() => Call(body, parameters, type, arguments));
        Assert.That(exception, Is.InstanceOf<TrapException>().Or.InnerException.InstanceOf<TrapException>());
    }

    [TestCase(0x7Fu, 0x7Fu)]
    [TestCase(0x80u, 0xFFFFFF80u)]
    [TestCase(0x1FFu, 0xFFFFFFFFu)]
    public void I32Extend8S(uint value, uint expected)
    {
        Assert.That(Call("local.get 0 i32.extend8_s", "(param i32)", "i32", I32(value)), Is.EqualTo(expected));
    }

    [Test]
    public void I64Ctz()
    {
        Assert.That(Call("local.get 0 i64.ctz", "(param i64)", "i64", I64(0)), Is.EqualTo(64));
        for (var i = 0; i < 64; i++)
        {
            Assert.That(Call("local.get 0 i64.ctz", "(param i64)", "i64", I64(1ul << i)), Is.EqualTo(i));
            Assert.That(Call("local.get 0 i64.ctz", "(param i64)", "i64", I64(ulong.MaxValue << i)), Is.EqualTo(i));
        }
    }

    [TestCase(0u, 2u)]
    [TestCase(1u, 1u)]
    public void IfWithoutElseWhoseBodyBranches(uint condition, uint expected)
    {
        const string body = """
                            block $exit (result i32)
                              local.get 0
                              if
                                i32.const 1
                                br $exit
                              end
                              i32.const 2
                            end
                            """;

        Assert.That(Call(body, "(param i32)", "i32", I32(condition)), Is.EqualTo(expected));
    }

    // a value on the stack keeps the value of the local at the time it was pushed
    [TestCase(3u, 3u + 4u)]
    public void LocalIsWrittenWhileItsValueIsOnTheStack(uint value, uint expected)
    {
        const string body = """
                            local.get 0
                            i32.const 4
                            local.set 0
                            local.get 0
                            i32.add
                            """;

        Assert.That(Call(body, "(param i32)", "i32", I32(value)), Is.EqualTo(expected));
    }

    [TestCase("i32.const 10 local.get 0 i32.sub", 3u, 7u)]
    [TestCase("local.get 0 i32.const 10 i32.sub", 13u, 3u)]
    [TestCase("i32.const 1 local.get 0 i32.shl", 4u, 16u)]
    [TestCase("i32.const 256 local.get 0 i32.shr_u", 4u, 16u)]
    [TestCase("i32.const -256 local.get 0 i32.shr_s", 4u, 0xFFFFFFF0u)]
    [TestCase("i32.const 7 local.get 0 i32.add", 3u, 10u)]
    [TestCase("i32.const 7 local.get 0 i32.lt_u", 8u, 1u)]
    [TestCase("i32.const 7 local.get 0 i32.lt_u", 7u, 0u)]
    [TestCase("i32.const -1 local.get 0 i32.lt_s", 0u, 1u)]
    [TestCase("i32.const 7 local.get 0 i32.ge_u", 7u, 1u)]
    public void ConstantOperandsOfI32(string body, uint value, uint expected)
    {
        Assert.That(Call(body, "(param i32)", "i32", I32(value)), Is.EqualTo(expected));
    }

    [TestCase("i64.const 10 local.get 0 i64.sub", 3ul, 7ul)]
    [TestCase("i64.const 1 local.get 0 i64.shl", 40ul, 1ul << 40)]
    [TestCase("i64.const -16 local.get 0 i64.shr_s", 4ul, ulong.MaxValue)]
    [TestCase("i64.const 0x123456789 local.get 0 i64.add", 1ul, 0x12345678Aul)]
    public void ConstantOperandsOfI64(string body, ulong value, ulong expected)
    {
        Assert.That(Call(body, "(param i64)", "i64", I64(value)), Is.EqualTo(expected));
    }

    [TestCase("f64.const 10 local.get 0 f64.sub", 4.0, 6.0)]
    [TestCase("local.get 0 f64.const 10 f64.sub", 4.0, -6.0)]
    [TestCase("f64.const 1 local.get 0 f64.div", 4.0, 0.25)]
    [TestCase("local.get 0 f64.const 2 f64.div", 4.0, 2.0)]
    [TestCase("f64.const 2 local.get 0 f64.mul", 4.0, 8.0)]
    public void ConstantOperandsOfF64(string body, double value, double expected)
    {
        var function = Utils.GetInstance(
            $$"""
              (module
                (func (export "run") (param f64) (result f64)
                  {{body}}))
              """, new Imports()).ExportInstance.Items["run"] as IInvocableFunction;

        using var context = new ExecutionContext();
        Span<StackValueItem> arguments = stackalloc StackValueItem[1];
        arguments[0] = new StackValueItem(value);
        context.Invoke(function!, arguments);
        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);

        Assert.That(results[0].ExpectValueF64(), Is.EqualTo(expected));
    }

    [TestCase("f64.const 1 local.get 0 f64.lt", 2.0, 1u)]
    [TestCase("f64.const 1 local.get 0 f64.lt", 1.0, 0u)]
    [TestCase("f64.const 1 local.get 0 f64.lt", double.NaN, 0u)]
    [TestCase("f64.const 1 local.get 0 f64.ge", double.NaN, 0u)]
    [TestCase("f64.const 1 local.get 0 f64.ne", double.NaN, 1u)]
    [TestCase("local.get 0 f64.const 1 f64.le", double.NaN, 0u)]
    public void ComparisonsWithFloatConstants(string body, double value, uint expected)
    {
        Assert.That(Call(body, "(param f64)", "i32", new StackValueItem(value)), Is.EqualTo(expected));
    }

    // the entries for blocks, loops and ends execute the following instruction by themselves
    [Test]
    public void BlocksEnteredByBranchesAndCalls()
    {
        var function = Utils.GetInstance(
            """
            (module
              (func $identity (param i32) (result i32) local.get 0)
              (func (export "run") (param $n i32) (result i32)
                (local $i i32) (local $sum i32)
                block $exit
                  loop $l
                    local.get $i
                    local.get $n
                    i32.ge_u
                    br_if $exit
                    block $skip
                      local.get $i
                      i32.const 1
                      i32.and
                      br_if $skip
                      local.get $sum
                      local.get $i
                      call $identity
                      i32.add
                      local.set $sum
                    end
                    local.get $i
                    i32.const 1
                    i32.add
                    local.set $i
                    br $l
                  end
                end
                local.get $sum))
            """, new Imports()).ExportInstance.Items["run"] as IInvocableFunction;

        using var context = new ExecutionContext();
        Span<StackValueItem> arguments = stackalloc StackValueItem[1];
        arguments[0] = new StackValueItem(10u);
        context.Invoke(function!, arguments);
        Span<StackValueItem> results = stackalloc StackValueItem[1];
        context.TakeResults(results);

        Assert.That(results[0].ExpectValueI32(), Is.EqualTo(0u + 2 + 4 + 6 + 8));
    }
}
