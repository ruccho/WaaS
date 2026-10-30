using WaaS.Models;
using WaaS.Runtime;
using WaaS.Runtime.Bindings;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

public class SimdTests
{
    private static V128 FromInt32(int lane0, int lane1, int lane2, int lane3)
    {
        var vector = default(V128);
        vector.SetLane(0, lane0);
        vector.SetLane(1, lane1);
        vector.SetLane(2, lane2);
        vector.SetLane(3, lane3);
        return vector;
    }

    private static StackValueItem[] Invoke(Instance instance, string name, params StackValueItem[] arguments)
    {
        Assert.That(instance.TryGetExport(name, out IInvocableFunction? function), Is.True);

        using var context = new ExecutionContext();
        context.Invoke(function!, arguments);

        var results = new StackValueItem[context.ResultLength];
        context.TakeResults(results);
        return results;
    }

    [Test]
    public void LanewiseArithmetic()
    {
        var instance = Utils.GetInstance("""
                                         (module
                                           (func (export "add") (param v128 v128) (result v128)
                                             local.get 0
                                             local.get 1
                                             i32x4.add)
                                           (func (export "add_sat") (result v128)
                                             v128.const i8x16 120 -120 0 0 0 0 0 0 0 0 0 0 0 0 0 0
                                             v128.const i8x16 10 -10 0 0 0 0 0 0 0 0 0 0 0 0 0 0
                                             i8x16.add_sat_s)
                                           (func (export "shl") (param i32) (result v128)
                                             v128.const i16x8 1 1 1 1 1 1 1 1
                                             local.get 0
                                             i16x8.shl))
                                         """, new Imports());

        var sum = Invoke(instance, "add",
            new StackValueItem(FromInt32(1, 2, int.MaxValue, -1)),
            new StackValueItem(FromInt32(10, 20, 1, 1)));
        Assert.That(sum[0].ExpectValueV128(), Is.EqualTo(FromInt32(11, 22, int.MinValue, 0)));

        var saturated = Invoke(instance, "add_sat")[0].ExpectValueV128();
        Assert.That(saturated.GetLane<sbyte>(0), Is.EqualTo(sbyte.MaxValue));
        Assert.That(saturated.GetLane<sbyte>(1), Is.EqualTo(sbyte.MinValue));

        // the shift count is taken modulo the lane width
        var shifted = Invoke(instance, "shl", new StackValueItem(17u))[0].ExpectValueV128();
        Assert.That(shifted.GetLane<ushort>(7), Is.EqualTo(2));
    }

    [Test]
    public void MemoryAccess()
    {
        var instance = Utils.GetInstance("""
                                         (module
                                           (memory 1)
                                           (func (export "roundtrip") (param i32) (result i64)
                                             local.get 0
                                             v128.const i64x2 0x0102030405060708 0x1112131415161718
                                             v128.store offset=1
                                             local.get 0
                                             v128.load offset=1
                                             i64x2.extract_lane 1)
                                           (func (export "load_lane") (param i32) (result i32)
                                             local.get 0
                                             i32.const 0xFFFF
                                             i32.store16
                                             local.get 0
                                             v128.const i32x4 0 0 0 0
                                             v128.load16_lane 3
                                             i16x8.extract_lane_s 3))
                                         """, new Imports());

        Assert.That(Invoke(instance, "roundtrip", new StackValueItem(100u))[0].ExpectValueI64(),
            Is.EqualTo(0x1112131415161718UL));
        Assert.That(Invoke(instance, "load_lane", new StackValueItem(8u))[0].ExpectValueI32(),
            Is.EqualTo(uint.MaxValue));

        // the access straddles the end of the memory
        Assert.Catch(() => Invoke(instance, "roundtrip", new StackValueItem(65536u - 16u)));
        // the effective address overflows 32 bits
        Assert.Catch(() => Invoke(instance, "roundtrip", new StackValueItem(uint.MaxValue)));
    }

    [Test]
    public void ValueTypeInBlocksGlobalsAndLocals()
    {
        var instance = Utils.GetInstance("""
                                         (module
                                           (global $g (mut v128) (v128.const i32x4 1 2 3 4))
                                           (global (export "c") v128 (v128.const f32x4 1.5 0 0 0))
                                           (func (export "run") (param i32) (result v128)
                                             (local v128)
                                             (block (result v128)
                                               global.get $g
                                               local.get 1 ;; zero
                                               local.get 0
                                               select)
                                             global.set $g
                                             global.get $g))
                                         """, new Imports());

        Assert.That(Invoke(instance, "run", new StackValueItem(1u))[0].ExpectValueV128(),
            Is.EqualTo(FromInt32(1, 2, 3, 4)));
        Assert.That(Invoke(instance, "run", new StackValueItem(0u))[0].ExpectValueV128(), Is.EqualTo(default(V128)));

        Assert.That(instance.TryGetExport("c", out WaaS.Runtime.Global global), Is.True);
        Assert.That(global.ValueType, Is.EqualTo(ValueType.V128));
        Assert.That(global.GetStackValue().ExpectValueV128().GetLane<float>(0), Is.EqualTo(1.5f));
    }

    [Test]
    public void ShuffleAndTests()
    {
        var instance = Utils.GetInstance("""
                                         (module
                                           (func (export "shuffle") (result v128)
                                             v128.const i8x16 0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15
                                             v128.const i8x16 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31
                                             i8x16.shuffle 31 0 30 1 29 2 28 3 27 4 26 5 25 6 24 7)
                                           (func (export "find") (param v128) (result i32)
                                             ;; bitmask of the 16-bit lanes equal to 0x22
                                             local.get 0
                                             i32.const 0x22
                                             i16x8.splat
                                             i16x8.eq
                                             i16x8.bitmask)
                                           (func (export "all_true") (param v128) (result i32)
                                             local.get 0
                                             i8x16.all_true))
                                         """, new Imports());

        var shuffled = Invoke(instance, "shuffle")[0].ExpectValueV128();
        for (var i = 0; i < 16; i++)
            Assert.That(shuffled.GetLane<byte>(i), Is.EqualTo(i % 2 == 0 ? 31 - i / 2 : i / 2));

        var text = default(V128);
        text.SetLane<ushort>(2, 0x22);
        text.SetLane<ushort>(5, 0x22);
        Assert.That(Invoke(instance, "find", new StackValueItem(text))[0].ExpectValueI32(), Is.EqualTo(0b100100u));

        Assert.That(Invoke(instance, "all_true", new StackValueItem(new V128(ulong.MaxValue, 0x0100000000000001)))[0]
            .ExpectValueI32(), Is.EqualTo(0u));
        Assert.That(Invoke(instance, "all_true", new StackValueItem(new V128(0x0101010101010101, ulong.MaxValue)))[0]
            .ExpectValueI32(), Is.EqualTo(1u));
    }

    [Test]
    public void FloatingPointSemantics()
    {
        var instance = Utils.GetInstance("""
                                         (module
                                           (func (export "nearest") (result v128)
                                             v128.const f32x4 0.5 1.5 -2.5 -0.25
                                             f32x4.nearest)
                                           (func (export "min") (result v128)
                                             v128.const f64x2 0 nan
                                             v128.const f64x2 -0 1
                                             f64x2.min))
                                         """, new Imports());

        var nearest = Invoke(instance, "nearest")[0].ExpectValueV128();
        Assert.That(nearest.GetLane<float>(0), Is.EqualTo(0f));
        Assert.That(nearest.GetLane<float>(1), Is.EqualTo(2f));
        Assert.That(nearest.GetLane<float>(2), Is.EqualTo(-2f));
        Assert.That(nearest.GetLane<uint>(3), Is.EqualTo(0x8000_0000u)); // -0

        var min = Invoke(instance, "min")[0].ExpectValueV128();
        Assert.That(min.GetLane<ulong>(0), Is.EqualTo(0x8000_0000_0000_0000UL)); // -0
        Assert.That(double.IsNaN(min.GetLane<double>(1)), Is.True);
    }

    [Test]
    public void HostFunctionWithV128()
    {
        var imports = new Imports();
        imports["env"] = new ModuleExports
        {
            {
                "reverse",
                CoreBinder.Instance.ToExternalFunction((V128 value) => new V128(value.Upper, value.Lower))
            }
        };

        var instance = Utils.GetInstance("""
                                         (module
                                           (import "env" "reverse" (func $reverse (param v128) (result v128)))
                                           (func (export "run") (param v128) (result v128)
                                             local.get 0
                                             call $reverse))
                                         """, imports);

        Assert.That(Invoke(instance, "run", new StackValueItem(new V128(1, 2)))[0].ExpectValueV128(),
            Is.EqualTo(new V128(2, 1)));
    }

    [Test]
    public void InvalidLaneIndexIsRejected()
    {
        // i32x4.extract_lane 4 (the lane index is patched since the text format rejects it)
        var binary = Utils.GetModuleBinary("""
                                           (module
                                             (func (param v128) (result i32)
                                               local.get 0
                                               i32x4.extract_lane 3))
                                           """);
        var index = Array.LastIndexOf(binary, (byte)0x1B);
        Assert.That(binary[index - 1], Is.EqualTo(0xFD));
        Assert.That(binary[index + 1], Is.EqualTo(3));
        binary[index + 1] = 4;

        Assert.Catch(() => _ = new Instance(Module.Create(binary), new Imports()));
    }
}
