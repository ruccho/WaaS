using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace WaaS.Benchmarks
{
    /// <summary>
    ///     Entry point of the benchmark player. It runs only with <c>-benchmark-mode</c>:
    ///     <list type="bullet">
    ///         <item><c>cold-waas</c>, <c>cold-lua</c>, <c>cold-lua-bytecode</c>: one load in a fresh process</item>
    ///         <item><c>warm</c>: repeated loads and all execution workloads</item>
    ///     </list>
    ///     Results are written as JSON to the path given by <c>-benchmark-output</c>.
    /// </summary>
    public sealed class BenchmarkRunner : MonoBehaviour
    {
        private const int WarmLoadRuns = 30;
        private const int WarmupRuns = 3;
        private const int Samples = 15;
        private const uint CallCount = 100_000;

        private static readonly (string name, uint n)[] ComputeWorkloads =
        {
            ("compute_fib", 30),
            ("compute_sieve", 1_000_000),
            ("compute_mandelbrot", 200),
            ("compute_strings", 20_000),
            ("compute_sort", 200_000),
            ("compute_map", 500_000)
        };

        private static readonly string[] FfiWorkloads =
        {
            "ffi_nop", "ffi_add", "ffi_string_in", "ffi_string_out", "ffi_struct", "ffi_resource"
        };

        private static byte[] componentBytes;
        private static byte[] luaSource;
        private static byte[] luaBytecode;
        private static readonly List<string> Records = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (GetArgument("-benchmark-mode") == null) return;
            var gameObject = new GameObject(nameof(BenchmarkRunner));
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<BenchmarkRunner>();
        }

        private IEnumerator Start()
        {
            // let the startup of the player settle
            for (var i = 0; i < 30; i++) yield return null;

            var exitCode = 0;
            try
            {
                Run(GetArgument("-benchmark-mode"), GetArgument("-benchmark-output"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                exitCode = 1;
            }

            Application.Quit(exitCode);
        }

        private static void Run(string mode, string outputPath)
        {
            if (outputPath == null) throw new ArgumentException("-benchmark-output is required.");

            // file IO is excluded from every measurement
            var directory = Application.streamingAssetsPath;
            componentBytes = File.ReadAllBytes(Path.Combine(directory, "benchmark.wasm"));
            luaSource = File.ReadAllBytes(Path.Combine(directory, "benchmark.lua"));
            luaBytecode = File.ReadAllBytes(Path.Combine(directory, "benchmark.luac"));

            switch (mode)
            {
                case "cold-waas":
                    RecordLoad("waas", "cold", LoadStages.Waas(componentBytes));
                    break;
                case "cold-lua":
                    RecordLoad("xlua", "cold", LoadStages.Lua(luaSource));
                    break;
                case "cold-lua-bytecode":
                    RecordLoad("xlua-bytecode", "cold", LoadStages.Lua(luaBytecode));
                    break;
                case "warm":
                    RunWarm();
                    break;
                default:
                    throw new ArgumentException($"Unknown mode: {mode}");
            }

            File.WriteAllText(outputPath, BuildReport(mode), Encoding.UTF8);
        }

        private static void RunWarm()
        {
            for (var i = 0; i < WarmLoadRuns + WarmupRuns; i++)
            {
                var record = i >= WarmupRuns;
                MeasureWarmLoad("waas", record, () => LoadStages.Waas(componentBytes));
                MeasureWarmLoad("xlua", record, () => LoadStages.Lua(luaSource));
                MeasureWarmLoad("xlua-bytecode", record, () => LoadStages.Lua(luaBytecode));
            }

            using var waas = new WaasEngine(componentBytes);
            using var lua = new LuaEngine(luaSource);

            foreach (var (name, n) in ComputeWorkloads) MeasureBoth(waas, lua, "compute", name, n);
            foreach (var name in FfiWorkloads) MeasureBoth(waas, lua, "ffi", name, CallCount);

            MeasureBoth(waas, lua, "callback", "callback_add", CallCount, engine =>
            {
                var add = engine.GetCallbackAdd();
                return n =>
                {
                    var value = 0;
                    for (var i = 0; i < n; i++) value = add(value, 1);
                    return value;
                };
            });
            MeasureBoth(waas, lua, "callback", "callback_string", CallCount, engine =>
            {
                var callback = engine.GetCallbackString();
                return n =>
                {
                    var total = 0.0;
                    for (var i = 0; i < n; i++) total += callback(HostFunctions.Text);
                    return total;
                };
            });
        }

        private static void MeasureWarmLoad(string engine, bool record,
            Func<IEnumerable<(string stage, double milliseconds)>> run)
        {
            GC.Collect();
            var stages = run();
            if (record) RecordLoad(engine, "warm", stages);
        }

        private static void MeasureBoth(IScriptEngine a, IScriptEngine b, string group, string name, uint n,
            Func<IScriptEngine, Func<uint, double>> factory = null)
        {
            factory ??= engine => engine.GetWorkload(name);
            var checksumA = Measure(a, group, name, n, factory(a));
            var checksumB = Measure(b, group, name, n, factory(b));

            // guards against workloads that have drifted apart, or that were optimized away
            if (checksumA != checksumB)
                throw new InvalidOperationException($"Checksum mismatch in {name}: {checksumA} != {checksumB}");
        }

        private static double Measure(IScriptEngine engine, string group, string name, uint n,
            Func<uint, double> workload)
        {
            var checksum = 0.0;
            for (var i = 0; i < WarmupRuns; i++) checksum = workload(n);

            var samples = new double[Samples];
            for (var i = 0; i < Samples; i++)
            {
                engine.Collect();
                GC.Collect();

                var stopwatch = Stopwatch.StartNew();
                checksum = workload(n);
                samples[i] = stopwatch.Elapsed.TotalMilliseconds;
            }

            Records.Add(
                $"{{\"kind\":\"exec\",\"engine\":\"{engine.Name}\",\"group\":\"{group}\",\"name\":\"{name}\",\"n\":{n},\"checksum\":{Number(checksum)},\"samples\":[{string.Join(",", samples.Select(Number))}]}}");
            return checksum;
        }

        private static void RecordLoad(string engine, string phase, IEnumerable<(string stage, double milliseconds)> stages)
        {
            foreach (var (stage, milliseconds) in stages)
                Records.Add(
                    $"{{\"kind\":\"load\",\"engine\":\"{engine}\",\"phase\":\"{phase}\",\"stage\":\"{stage}\",\"ms\":{Number(milliseconds)}}}");
        }

        private static string BuildReport(string mode)
        {
#if ENABLE_IL2CPP
            const string backend = "il2cpp";
#else
            const string backend = "mono";
#endif
            var builder = new StringBuilder();
            builder.Append('{');
            builder.Append($"\"mode\":\"{mode}\",\"backend\":\"{backend}\",");
            builder.Append($"\"unity\":\"{Application.unityVersion}\",\"development\":{(Debug.isDebugBuild ? "true" : "false")},");
            builder.Append($"\"cpu\":\"{SystemInfo.processorType.Trim()}\",\"os\":\"{SystemInfo.operatingSystem}\",");
            builder.Append($"\"sizes\":{{\"component\":{componentBytes.Length},\"lua\":{luaSource.Length},\"luac\":{luaBytecode.Length}}},");
            builder.Append($"\"records\":[{string.Join(",", Records)}]");
            builder.Append('}');
            return builder.ToString();
        }

        private static string Number(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
