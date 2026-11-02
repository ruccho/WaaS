using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Waas.Benchmark;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using XLua;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Benchmarks
{
    /// <summary>
    ///     A loaded script. Workload names are those of guest/wit/benchmark.wit, with hyphens replaced by underscores.
    /// </summary>
    internal interface IScriptEngine : IDisposable
    {
        string Name { get; }
        Func<uint, double> GetWorkload(string name);
        Func<int, int, int> GetCallbackAdd();
        Func<string, int> GetCallbackString();

        /// <summary>
        ///     Reclaims garbage between samples, outside of the measured time.
        /// </summary>
        void Collect();
    }

    internal sealed class WaasEngine : IScriptEngine
    {
        private readonly ExecutionContext context = new();
        private readonly IBenchmark.Wrapper exports;

        public WaasEngine(byte[] componentBytes)
        {
            var instance = Instantiate(Component.Create(componentBytes));
            exports = new IBenchmark.Wrapper(instance, context);
        }

        public string Name => "waas";

        public Func<uint, double> GetWorkload(string name)
        {
            switch (name)
            {
                case "compute_fib": return n => Sync(exports.ComputeFib(n));
                case "compute_sieve": return n => Sync(exports.ComputeSieve(n));
                case "compute_mandelbrot": return n => Sync(exports.ComputeMandelbrot(n));
                case "compute_strings": return n => Sync(exports.ComputeStrings(n));
                case "compute_sort": return n => Sync(exports.ComputeSort(n));
                case "compute_map": return n => Sync(exports.ComputeMap(n));
                case "ffi_nop": return n => Sync(exports.FfiNop(n));
                case "ffi_add": return n => Sync(exports.FfiAdd(n));
                case "ffi_string_in": return n => Sync(exports.FfiStringIn(n));
                case "ffi_string_out": return n => Sync(exports.FfiStringOut(n));
                case "ffi_struct": return n => Sync(exports.FfiStruct(n));
                case "ffi_resource": return n => Sync(exports.FfiResource(n));
                default: throw new ArgumentOutOfRangeException(nameof(name), name, null);
            }
        }

        public Func<int, int, int> GetCallbackAdd()
        {
            return (a, b) => Sync(exports.CallbackAdd(a, b));
        }

        public Func<string, int> GetCallbackString()
        {
            return value => Sync(exports.CallbackString(value));
        }

        public void Collect()
        {
        }

        public void Dispose()
        {
            context.Dispose();
        }

        public static IInstance Instantiate(IComponent component)
        {
            return component.Instantiate(new Dictionary<string, ISortedExportable>
            {
                { "waas:benchmark/host", IHost.CreateWaaSInstance(new WaasBenchmarkHost()) }
            });
        }

        public static T Sync<T>(ValueTask<T> task)
        {
            // none of the host functions suspends, so the guest always runs to completion synchronously
            if (!task.IsCompletedSuccessfully) throw new InvalidOperationException("The call did not complete.");
            return task.Result;
        }
    }

    internal sealed class LuaEngine : IScriptEngine
    {
        private readonly LuaEnv env = new();
        private readonly LuaTable module;

        public LuaEngine(byte[] chunk)
        {
            using var function = env.LoadString<LuaFunction>(chunk, "benchmark");
            module = (LuaTable)function.Call()[0];
        }

        public string Name => "xlua";

        public Func<uint, double> GetWorkload(string name)
        {
            return module.Get<Func<uint, double>>(name);
        }

        public Func<int, int, int> GetCallbackAdd()
        {
            return module.Get<Func<int, int, int>>("callback_add");
        }

        public Func<string, int> GetCallbackString()
        {
            return module.Get<Func<string, int>>("callback_string");
        }

        public void Collect()
        {
            env.FullGc();
        }

        public void Dispose()
        {
            // the delegates obtained from the table stay referenced by the caller until the process exits,
            // so the environment is intentionally not disposed
        }
    }

    /// <summary>
    ///     Measures each stage from an unloaded script to its first call.
    /// </summary>
    internal static class LoadStages
    {
        public static IEnumerable<(string stage, double milliseconds)> Waas(byte[] bytes)
        {
            var stopwatch = Stopwatch.StartNew();
            var component = Component.Create(bytes);
            var load = Elapsed(stopwatch);

            var instance = WaasEngine.Instantiate(component);
            var instantiate = Elapsed(stopwatch);

            using var context = new ExecutionContext();
            var exports = new IBenchmark.Wrapper(instance, context);
            var result = WaasEngine.Sync(exports.CallbackAdd(1, 2));
            var firstCall = Elapsed(stopwatch);
            if (result != 3) throw new InvalidOperationException("Unexpected result.");

            return new[] { ("engine-init", 0.0), ("load", load), ("instantiate", instantiate), ("first-call", firstCall) };
        }

        public static IEnumerable<(string stage, double milliseconds)> Lua(byte[] chunk)
        {
            var stopwatch = Stopwatch.StartNew();
            var env = new LuaEnv();
            var engineInit = Elapsed(stopwatch);

            var function = env.LoadString<LuaFunction>(chunk, "benchmark");
            var load = Elapsed(stopwatch);

            var module = (LuaTable)function.Call()[0];
            var instantiate = Elapsed(stopwatch);

            var callback = module.Get<LuaFunction>("callback_add");
            var result = callback.Call(1, 2)[0];
            var firstCall = Elapsed(stopwatch);
            if (Convert.ToInt32(result) != 3) throw new InvalidOperationException("Unexpected result.");

            callback.Dispose();
            module.Dispose();
            function.Dispose();
            env.Dispose();

            return new[] { ("engine-init", engineInit), ("load", load), ("instantiate", instantiate), ("first-call", firstCall) };
        }

        private static double Elapsed(Stopwatch stopwatch)
        {
            var value = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();
            return value;
        }
    }
}
