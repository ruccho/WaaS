using Lua;
using UnityEngine;

namespace WaaS.Benchmarks
{
    /// <summary>
    ///     Implementation of the host functions for Lua-CSharp. Set to the global variable benchmark_host of benchmark.lua.
    /// </summary>
    [LuaObject]
    public sealed partial class LuaCSharpBenchmarkHost
    {
        [LuaMember]
        public void Nop()
        {
            HostFunctions.NopCount++;
        }

        [LuaMember]
        public int Add(int a, int b)
        {
            return HostFunctions.Add(a, b);
        }

        [LuaMember]
        public int TakeString(string value)
        {
            return value.Length;
        }

        [LuaMember]
        public string GetString()
        {
            return HostFunctions.Text;
        }

        // Lua-CSharp has no value type for host structs, so a vector crosses the boundary as an object
        [LuaMember]
        public LuaCSharpVector3 Transform(LuaCSharpVector3 value)
        {
            return new LuaCSharpVector3(HostFunctions.Transform(value.Value));
        }

        [LuaMember]
        public LuaCSharpCounter GetCounter()
        {
            return new LuaCSharpCounter();
        }

        [LuaMember]
        public static LuaCSharpVector3 CreateVector3(float x, float y, float z)
        {
            return new LuaCSharpVector3(new Vector3(x, y, z));
        }
    }

    [LuaObject]
    public sealed partial class LuaCSharpCounter
    {
        private readonly BenchmarkCounter counter = new();

        [LuaMember]
        public int Add(int delta)
        {
            return counter.Add(delta);
        }
    }

    [LuaObject]
    public sealed partial class LuaCSharpVector3
    {
        public LuaCSharpVector3(Vector3 value)
        {
            Value = value;
        }

        public Vector3 Value { get; }

        [LuaMember("x")] public float X => Value.x;
        [LuaMember("y")] public float Y => Value.y;
        [LuaMember("z")] public float Z => Value.z;
    }
}
