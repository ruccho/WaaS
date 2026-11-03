using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using WaaS.ComponentModel.Binding;
using Waas.Benchmark;
using XLua;

namespace WaaS.Benchmarks
{
    /// <summary>
    ///     Behavior of the functions exposed to the scripts. Both engines call the same implementation so that the
    ///     difference between them is the cost of the bindings.
    /// </summary>
    internal static class HostFunctions
    {
        public const string Text = "hello, world";
        public static int NopCount;

        public static int Add(int a, int b)
        {
            return a + b;
        }

        public static Vector3 Transform(Vector3 value)
        {
            return new Vector3(value.x + 1, value.y + 2, value.z + 3);
        }
    }

    public sealed class BenchmarkCounter
    {
        private int value;

        public int Add(int delta)
        {
            value += delta;
            return value;
        }
    }

    /// <summary>
    ///     Implementation of the host interface for WaaS.
    /// </summary>
    internal sealed class WaasBenchmarkHost : IHost
    {
        public IHost.ICounterResourceImpl Counter { get; } = new CounterType();

        public ValueTask Nop()
        {
            HostFunctions.NopCount++;
            return default;
        }

        public ValueTask<int> Add(int a, int b)
        {
            return new ValueTask<int>(HostFunctions.Add(a, b));
        }

        public ValueTask<int> TakeString(string value)
        {
            return new ValueTask<int>(value.Length);
        }

        public ValueTask<string> GetString()
        {
            return new ValueTask<string>(HostFunctions.Text);
        }

        public ValueTask<IHost.Vec3> Transform(IHost.Vec3 value)
        {
            var result = HostFunctions.Transform(new Vector3(value.X, value.Y, value.Z));
            return new ValueTask<IHost.Vec3>(new IHost.Vec3 { X = result.x, Y = result.y, Z = result.z });
        }

        public ValueTask<Owned<IHost.ICounterResourceImpl>> GetCounter()
        {
            var owned = ((CounterType)Counter).Wrap(new BenchmarkCounter());
            return new ValueTask<Owned<IHost.ICounterResourceImpl>>((Owned<IHost.ICounterResourceImpl>)owned);
        }

        private sealed class CounterType : HostResourceTypeBase<BenchmarkCounter>, IHost.ICounterResourceImpl
        {
            public WaaS.ComponentModel.Runtime.IResourceType Type => this;

            public ValueTask<int> Add(Borrowed<IHost.ICounterResourceImpl> self, int delta)
            {
                return new ValueTask<int>(Unwrap(self).Add(delta));
            }

            protected override void OnDrop(BenchmarkCounter value)
            {
            }
        }
    }

    /// <summary>
    ///     Implementation of the host functions for xLua. Accessed from benchmark.lua.
    /// </summary>
    public sealed class LuaBenchmarkHost
    {
        public static readonly LuaBenchmarkHost Instance = new();

        public void Nop()
        {
            HostFunctions.NopCount++;
        }

        public int Add(int a, int b)
        {
            return HostFunctions.Add(a, b);
        }

        public int TakeString(string value)
        {
            return value.Length;
        }

        public string GetString()
        {
            return HostFunctions.Text;
        }

        public Vector3 Transform(Vector3 value)
        {
            return HostFunctions.Transform(value);
        }

        public BenchmarkCounter GetCounter()
        {
            return new BenchmarkCounter();
        }
    }

    /// <summary>
    ///     Configuration of the xLua code generation. Without the generated wrappers, xLua falls back to reflection,
    ///     which is not how a product would ship.
    /// </summary>
    public static class XLuaBenchmarkConfig
    {
        [LuaCallCSharp] public static readonly List<Type> LuaCallCSharp = new()
        {
            typeof(LuaBenchmarkHost),
            typeof(BenchmarkCounter),
            typeof(Vector3)
        };

        [CSharpCallLua] public static readonly List<Type> CSharpCallLua = new()
        {
            typeof(Func<uint, double>),
            typeof(Func<int, int, int>),
            typeof(Func<string, int>)
        };

        // Vector3 is passed by value without boxing, because XLua/Src/GenAttributes.cs declares it with GCOptimize
    }
}
