using WaaS.Models;
using WaaS.Runtime;
using WaaS.Runtime.Bindings;

namespace WaaS.Tests.Wast;

public static class SpecTest
{
    public static Imports CreateImports()
    {
        var imports = new Imports();
        var binder = new CoreBinder();

        imports["spectest"] = new ModuleImports
        {
            { "print", binder.ToExternalFunction(() => { Console.WriteLine("<spectest> print"); }) },
            {
                "print_i32",
                binder.ToExternalFunction((uint a) => { Console.WriteLine($"<spectest> print_i32: {a}"); })
            },
            {
                "print_i64",
                binder.ToExternalFunction((ulong a) => { Console.WriteLine($"<spectest> print_i64: {a}"); })
            },
            {
                "print_f32",
                binder.ToExternalFunction((float a) => { Console.WriteLine($"<spectest> print_f32: {a}"); })
            },
            {
                "print_f64",
                binder.ToExternalFunction(
                    (double a) => { Console.WriteLine($"<spectest> print_f64: {a}"); })
            },
            {
                "print_i32_f32",
                binder.ToExternalFunction((uint a, float b) =>
                {
                    Console.WriteLine($"<spectest> print_i32_f32: {a}, {b}");
                })
            },
            {
                "print_f64_f64",
                binder.ToExternalFunction((double a, double b) =>
                {
                    Console.WriteLine($"<spectest> print_f64_f64: {a}, {b}");
                })
            },
            { "global_i32", new Global<uint>(666) },
            { "global_i64", new Global<ulong>(0) },
            { "global_f32", new Global<float>(0) },
            { "global_f64", new Global<double>(0) },
            { "table", new Table<IInvocableFunction>(new Limits(10, 20)) },
            { "memory", new Memory(new Limits(1, 2)) }
        };

        return imports;
    }
}