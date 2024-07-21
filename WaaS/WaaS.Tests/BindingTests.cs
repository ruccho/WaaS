using System.Diagnostics;
using System.Text;
using WaaS.Models;
using WaaS.Runtime;
using WaaS.Runtime.Bindings;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

public class BindingTests
{

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestExportCore()
    {
        var function = Utils.GetInstance("""
                                   (module
                                        (func $add (param $lhs i32) (param $rhs i32) (result i32)
                                           local.get $lhs
                                           local.get $rhs
                                           i32.add)
                                       (export "add" (func $add))
                                   )
                                   """, new Imports()).ExportInstance.Items["add"] as IInvocableFunction;

        var binder = new CoreBinder();
        var result = binder.Invoke<int>(new ExecutionContext(), function, 1, 2);

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void TestImportCore()
    {
        var binder = new CoreBinder();

        var externalFunc = binder.ToExternalFunction((int a, int b) => a + b);
        var imports = new Imports
        {
            {
                "waas", new ModuleExports
                {
                    { "add", externalFunc }
                }
            }
        };

        var function = Utils.GetInstance(
/*    */"""
        (module
            (import "waas" "add" (func $add (param i32) (param i32) (result i32)))
            (func $main (param i32) (param i32) (result i32)
                local.get 0
                local.get 1
                call $add)
            (export "main" (func $main))
        )
        """, imports).ExportInstance.Items["main"] as IInvocableFunction;

        var result = binder.Invoke<int>(new ExecutionContext(), function, 1, 2);

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public async ValueTask TestImportAsyncCore()
    {
        var binder = new CoreBinder();

        var externalFunc = binder.ToAsyncExternalFunction(async (int a, int b) => a + b);
        var imports = new Imports
        {
            {
                "waas", new ModuleExports
                {
                    { "add", externalFunc }
                }
            }
        };

        var function = Utils.GetInstance(
/*    */"""
        (module
            (import "waas" "add" (func $add (param i32) (param i32) (result i32)))
            (func $main (param i32) (param i32) (result i32)
                local.get 0
                local.get 1
                call $add)
            (export "main" (func $main))
        )
        """, imports).ExportInstance.Items["main"] as IInvocableFunction;

        var result = await binder.InvokeAsync<int>(new ExecutionContext(), function, 1, 2);

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public async ValueTask TestImportAsyncCoreNoReturn()
    {
        var binder = new CoreBinder();

        var externalFunc = binder.ToAsyncExternalFunction(async (int a, int b) => { Console.WriteLine(a + b); });
        var imports = new Imports
        {
            {
                "waas", new ModuleExports
                {
                    { "add", externalFunc }
                }
            }
        };

        var function = Utils.GetInstance(
/*    */"""
        (module
            (import "waas" "add" (func $add (param i32) (param i32)))
            (func $main (param i32) (param i32)
                local.get 0
                local.get 1
                call $add)
            (export "main" (func $main))
        )
        """, imports).ExportInstance.Items["main"] as IInvocableFunction;

        await binder.InvokeAsync(new ExecutionContext(), function, 1, 2);
    }

    [Test]
    public async ValueTask TestImportAsyncCoreDelayed()
    {
        var binder = new CoreBinder();

        var externalFunc = binder.ToAsyncExternalFunction(async (int a, int b) =>
        {
            await Task.Delay(1000);
            return a + b;
        });
        var imports = new Imports
        {
            {
                "waas", new ModuleExports
                {
                    { "add", externalFunc }
                }
            }
        };

        var function = Utils.GetInstance(
/*    */"""
        (module
            (import "waas" "add" (func $add (param i32) (param i32) (result i32)))
            (func $main (param i32) (param i32) (result i32)
                local.get 0
                local.get 1
                call $add)
            (export "main" (func $main))
        )
        """, imports).ExportInstance.Items["main"] as IInvocableFunction;

        var result = await binder.InvokeAsync<int>(new ExecutionContext(), function, 1, 2);

        Assert.That(result, Is.EqualTo(3));
    }
}