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
        T Pop(in TransientWasmStackFrame frame);
        void Push(in TransientWasmStackFrame frame, T value);
    }

    public struct ValueTypeI32 : IValueType<uint>
    {
        public uint Pop(in TransientWasmStackFrame frame)
        {
            return frame.Pop().ExpectValueI32();
        }

        public void Push(in TransientWasmStackFrame frame, uint value)
        {
            frame.Push(value);
        }
    }

    public struct ValueTypeI64 : IValueType<ulong>
    {
        public ulong Pop(in TransientWasmStackFrame frame)
        {
            return frame.Pop().ExpectValueI64();
        }

        public void Push(in TransientWasmStackFrame frame, ulong value)
        {
            frame.Push(value);
        }
    }

    public struct ValueTypeF32 : IValueType<float>
    {
        public float Pop(in TransientWasmStackFrame frame)
        {
            return frame.Pop().ExpectValueF32();
        }

        public void Push(in TransientWasmStackFrame frame, float value)
        {
            frame.Push(value);
        }
    }

    public struct ValueTypeF64 : IValueType<double>
    {
        public double Pop(in TransientWasmStackFrame frame)
        {
            return frame.Pop().ExpectValueF64();
        }

        public void Push(in TransientWasmStackFrame frame, double value)
        {
            frame.Push(value);
        }
    }
}