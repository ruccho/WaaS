using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;

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
}