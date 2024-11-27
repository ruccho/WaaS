using System;
using System.Runtime.InteropServices;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a value on the stack.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct StackValueItem : IEquatable<StackValueItem>
    {
        [FieldOffset(0)] internal readonly ValueType valueType;

        [FieldOffset(8)] internal Label label;

        [FieldOffset(8)] internal uint valueI32;

        [FieldOffset(8)] internal ulong valueI64;

        [FieldOffset(8)] internal float valueF32;

        [FieldOffset(8)] internal double valueF64;

        public readonly bool IsLabel => (byte)valueType == 0xFF;

        public override string ToString()
        {
            return valueType switch
            {
                ValueType.I32 => valueI32.ToString(),
                ValueType.I64 => valueI64.ToString(),
                ValueType.F32 => valueF32.ToString(),
                ValueType.F64 => valueF64.ToString(),
                _ => "(unknown)"
            };
        }

        public readonly bool IsType(ValueType type)
        {
            return valueType == type;
        }

        public StackValueItem(Label label)
        {
            this = default;
            valueType = (ValueType)0xFF;
            this.label = label;
        }

        public StackValueItem(ValueType type)
        {
            this = default;
            valueType = type;
        }

        public StackValueItem(int value) : this(unchecked((uint)value))
        {
        }

        public StackValueItem(uint value)
        {
            this = default;
            valueType = ValueType.I32;
            valueI32 = value;
        }

        public StackValueItem(long value) : this(unchecked((ulong)value))
        {
        }

        public StackValueItem(ulong value)
        {
            this = default;
            valueType = ValueType.I64;
            valueI64 = value;
        }


        public StackValueItem(float value)
        {
            this = default;
            valueType = ValueType.F32;
            valueF32 = value;
        }


        public StackValueItem(double value)
        {
            this = default;
            valueType = ValueType.F64;
            valueF64 = value;
        }

        public readonly bool TryGetLabel(out Label label)
        {
            if (IsLabel)
            {
                label = this.label;
                return true;
            }

            label = default;
            return false;
        }

        public readonly bool TryGetValue(out uint value)
        {
            if (valueType != ValueType.I32)
            {
                value = default;
                return false;
            }

            value = valueI32;
            return true;
        }

        public readonly bool TryGetValue(out ulong value)
        {
            if (valueType != ValueType.I64)
            {
                value = default;
                return false;
            }

            value = valueI64;
            return true;
        }

        public readonly bool TryGetValue(out float value)
        {
            if (valueType != ValueType.F32)
            {
                value = default;
                return false;
            }

            value = valueF32;
            return true;
        }

        public readonly bool TryGetValue(out double value)
        {
            if (valueType != ValueType.F64)
            {
                value = default;
                return false;
            }

            value = valueF64;
            return true;
        }

        public readonly StackValueItem ExpectValue()
        {
            return ExpectValue(out _);
        }

        public readonly StackValueItem ExpectValue(out ValueType type)
        {
            if (IsLabel) throw new InvalidCodeException();
            type = valueType;
            return this;
        }

        public readonly StackValueItem ExpectValue(ValueType type)
        {
            if (type != valueType)
                throw new InvalidCodeException($"StackValueType expected: {type}, actual: {valueType}");
            return this;
        }

        public readonly uint ExpectValueI32()
        {
            if (valueType != ValueType.I32)
                throw new InvalidCodeException($"StackValueType expected: i32, actual: {valueType}");
            return valueI32;
        }

        public readonly ulong ExpectValueI64()
        {
            if (valueType != ValueType.I64)
                throw new InvalidCodeException($"StackValueType expected: i64, actual: {valueType}");
            return valueI64;
        }

        public readonly float ExpectValueF32()
        {
            if (valueType != ValueType.F32)
                throw new InvalidCodeException($"StackValueType expected: f32, actual: {valueType}");
            return valueF32;
        }

        public readonly double ExpectValueF64()
        {
            if (valueType != ValueType.F64)
                throw new InvalidCodeException($"StackValueType expected: f64, actual: {valueType}");
            return valueF64;
        }

        public readonly Label ExpectLabel()
        {
            if (!IsLabel) throw new InvalidCodeException();
            return label;
        }

        public bool Equals(StackValueItem other)
        {
            if (valueType != other.valueType) return false;
            switch (valueType)
            {
                case ValueType.I32:
                    return valueI32 == other.valueI32;
                case ValueType.I64:
                    return valueI64 == other.valueI64;
                case ValueType.F32:
                    return valueI32 == other.valueI32;
                case ValueType.F64:
                    return valueI64 == other.valueI64;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool Equals(object obj)
        {
            return obj is StackValueItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            var code = new HashCode();
            code.Add(valueType);
            switch (valueType)
            {
                case ValueType.I32:
                    code.Add(valueI32);
                    break;
                case ValueType.I64:
                    code.Add(valueI64);
                    break;
                case ValueType.F32:
                    code.Add(valueF32);
                    break;
                case ValueType.F64:
                    code.Add(valueF64);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return code.ToHashCode();
        }
    }
}