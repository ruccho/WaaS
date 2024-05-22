using System.Diagnostics;
using System.Text;
using WaaS.Models;
using WaaS.Runtime;
using WaaS.Runtime.Bindings;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

public class BindingTests
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestExportCore()
    {
        var function = GetInstance("""
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
                "waas", new ModuleImports
                {
                    { "add", externalFunc }
                }
            }
        };

        var function = GetInstance(
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
                "waas", new ModuleImports
                {
                    { "add", externalFunc }
                }
            }
        };

        var function = GetInstance(
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
                "waas", new ModuleImports
                {
                    { "add", externalFunc }
                }
            }
        };

        var function = GetInstance(
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
                "waas", new ModuleImports
                {
                    { "add", externalFunc }
                }
            }
        };

        var function = GetInstance(
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

    private static Instance GetInstance(string wat, Imports imports)
    {
        var dir = Path.Combine(Path.GetTempPath(), "WaaS.Tests");
        var watPath = Path.Combine(dir, "temp.wat");
        var wasmPath = Path.Combine(dir, "temp.wasm");

        File.WriteAllText(watPath, wat, Utf8NoBom);

        var psi = new ProcessStartInfo("wat2wasm", watPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = dir
        };

        using var wat2wasm = new Process();

        wat2wasm.StartInfo = psi;

        wat2wasm.OutputDataReceived += (sender, args) => { Console.WriteLine(args.Data); };
        wat2wasm.ErrorDataReceived += (sender, args) => { Console.WriteLine(args.Data); };

        wat2wasm.Start();
        wat2wasm.BeginErrorReadLine();
        wat2wasm.BeginOutputReadLine();

        wat2wasm.WaitForExit();

        var module = Module.Create(File.ReadAllBytes(wasmPath));
        return new Instance(module, imports);
    }
}