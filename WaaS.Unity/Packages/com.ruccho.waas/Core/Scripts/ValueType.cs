using WaaS.Runtime;

namespace WaaS
{
    public enum ValueType : byte
    {
        I32 = 0x7F,
        I64 = 0x7E,
        F32 = 0x7D,
        F64 = 0x7C
    }

    internal static class ValueTypeRegistry
    {
        static ValueTypeRegistry()
        {
            Registry<uint>.valueType = ValueType.I32;
            Registry<ulong>.valueType = ValueType.I64;
            Registry<float>.valueType = ValueType.F32;
            Registry<double>.valueType = ValueType.F64;
        }

        public static ValueType? GetFor<T>() where T : unmanaged
        {
            return Registry<T>.valueType;
        }

        private static class Registry<T> where T : unmanaged
        {
            public static ValueType? valueType;
        }
    }

    public interface IValueType<T> where T : unmanaged
    {
        T Pop(WasmStackFrame frame);
        void Push(WasmStackFrame frame, T value);
    }

    public struct ValueTypeI32 : IValueType<uint>
    {
        public uint Pop(WasmStackFrame frame)
        {
            return frame.Pop().ExpectValueI32();
        }

        public void Push(WasmStackFrame frame, uint value)
        {
            frame.Push(value);
        }
    }

    public struct ValueTypeI64 : IValueType<ulong>
    {
        public ulong Pop(WasmStackFrame frame)
        {
            return frame.Pop().ExpectValueI64();
        }

        public void Push(WasmStackFrame frame, ulong value)
        {
            frame.Push(value);
        }
    }

    public struct ValueTypeF32 : IValueType<float>
    {
        public float Pop(WasmStackFrame frame)
        {
            return frame.Pop().ExpectValueF32();
        }

        public void Push(WasmStackFrame frame, float value)
        {
            frame.Push(value);
        }
    }

    public struct ValueTypeF64 : IValueType<double>
    {
        public double Pop(WasmStackFrame frame)
        {
            return frame.Pop().ExpectValueF64();
        }

        public void Push(WasmStackFrame frame, double value)
        {
            frame.Push(value);
        }
    }

    public interface IBitWidth
    {
        int Value { get; }
    }

    public struct BitWidth8 : IBitWidth
    {
        public int Value => 8;
    }

    public struct BitWidth16 : IBitWidth
    {
        public int Value => 16;
    }
}