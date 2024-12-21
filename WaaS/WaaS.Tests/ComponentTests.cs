using System.Runtime.InteropServices;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

public class ComponentTests
{
    [Test]
    public void TestSimple1()
    {
        Utils.GetInstance("""
                          (component)
                          """, new Dictionary<string, ISortedExportable>());

        Assert.Pass();
    }

    [Test]
    public void TestSimple2()
    {
        Utils.GetInstance("""
                          (component
                            (core module)
                          )
                          """, new Dictionary<string, ISortedExportable>());
    }

    [Test]
    public void TestSimple3()
    {
        Utils.GetInstance("""
                          (component
                            (core module)
                            (core module)
                            (core module)
                          )
                          """, new Dictionary<string, ISortedExportable>());
    }

    [Test]
    public void TestSimple4()
    {
        Utils.GetInstance("""
                          (component
                            (core module
                              (func (export "a") (result i32) i32.const 0)
                              (func (export "b") (result i64) i64.const 0)
                            )
                            (core module
                              (func (export "c") (result f32) f32.const 0)
                              (func (export "d") (result f64) f64.const 0)
                            )
                          )
                          """, new Dictionary<string, ISortedExportable>());
    }

    [Test]
    public void TestSimple5()
    {
        Utils.GetInstance("""
                          (component
                            (import "a" (component))
                          )
                          """, new Dictionary<string, ISortedExportable>()
        {
            {
                "a", Utils.GetComponent("""
                                        (component)
                                        """)
            }
        });
    }

    [Test]
    public void TestSimple6()
    {
        var instance = Utils.GetInstance(
            """
            (component
              (core module $A (func (export "add") (param i32 i32) (result i32) (i32.add (local.get 0) (local.get 1))))
              (core instance $a (instantiate $A))
              (alias core export $a "add" (core func $f))
              (type $t (func (param "lhs" s32) (param "rhs" s32) (result s32)))
              (func $l (type $t) (canon lift (core func $f)))
              (export "add" (func $l))
            )
            """, new Dictionary<string, ISortedExportable>());

        // エクスポートされたcomponent funcの取得
        if (!instance.TryGetExport("add", out IFunction? function)) throw new InvalidOperationException();

        using var context = new ExecutionContext();

        using var binder = function.GetBinder(context);

        var pusher = binder.ArgumentPusher;
        pusher.Push(123);
        pusher.Push(456);
        binder.Invoke(context);

        Assert.That(binder.TakeResult<int>(), Is.EqualTo(123 + 456));

    }
}