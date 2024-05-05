using WaaS.Models;
using WaaS.Runtime;

namespace WaaS.Tests;

public static class SpecTest
{
    public static Imports CreateImports()
    {
        var imports = new Imports();
        imports["spectest"] = new ModuleImports
        {
            { "print", new ExternalFunctionDelegate(() => { Console.WriteLine("<spectest> print"); }) },
            {
                "print_i32",
                new ExternalFunctionDelegate((uint a) => { Console.WriteLine($"<spectest> print_i32: {a}"); })
            },
            {
                "print_i64",
                new ExternalFunctionDelegate((ulong a) => { Console.WriteLine($"<spectest> print_i64: {a}"); })
            },
            {
                "print_f32",
                new ExternalFunctionDelegate((float a) => { Console.WriteLine($"<spectest> print_f32: {a}"); })
            },
            {
                "print_f64",
                new ExternalFunctionDelegate((double a) => { Console.WriteLine($"<spectest> print_f64: {a}"); })
            },
            {
                "print_i32_f32",
                new ExternalFunctionDelegate((uint a, float b) =>
                {
                    Console.WriteLine($"<spectest> print_i32_f32: {a}, {b}");
                })
            },
            {
                "print_f64_f64",
                new ExternalFunctionDelegate((double a, double b) =>
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