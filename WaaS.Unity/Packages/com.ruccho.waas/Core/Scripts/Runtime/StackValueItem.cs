using System;
using System.Runtime.InteropServices;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a value on the stack.
    /// </summary>
    /// <remarks>
    ///     The payload starts at offset 8. Scalar values use 8 bytes and v128 values use 16 bytes of it.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct StackValueItem : IEquatable<StackValueItem>
    {
        [FieldOffset(0)] internal ValueType valueType;

        [FieldOffset(8)] internal Label label;

        [FieldOffset(8)] internal uint valueI32;

        [FieldOffset(8)] internal ulong valueI64;

        [FieldOffset(8)] internal float valueF32;

        [FieldOffset(8)] internal double valueF64;

        [FieldOffset(8)] internal V128 valueV128;

        public readonly bool IsLabel => (byte)valueType == 0xFF;

        public override string ToString()
        {
            return valueType switch
            {
                ValueType.I32 => valueI32.ToString(),
                ValueType.I64 => valueI64.ToString(),
                ValueType.F32 => valueF32.ToString(),
                ValueType.F64 => valueF64.ToString(),
                ValueType.V128 => valueV128.ToString(),
                ValueType.FuncRef or ValueType.ExternRef => valueI32 == 0
                    ? $"{valueType}(null)"
                    : $"{valueType}({References.Get(valueI32)})",
                _ => "(unknown)"
            };
        }

        public readonly bool IsType(ValueType type)
        {
            return valueType == type;
        }

        internal StackValueItem(ValueType type)
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

        public StackValueItem(V128 value)
        {
            this = default;
            valueType = ValueType.V128;
            valueV128 = value;
        }

        /// <summary>
        ///     Creates a reference value (<c>funcref</c> or <c>externref</c>). <c>null</c> is the null reference.
        /// </summary>
        /// <remarks>
        ///     The value holds the object weakly. The object must be kept alive while the value is used outside
        ///     tables and globals, which hold the objects strongly.
        /// </remarks>
        public static StackValueItem FromReference(ValueType type, object value)
        {
            if (!type.IsReference()) throw new ArgumentException($"{type} is not a reference type.", nameof(type));
            if (type == ValueType.FuncRef && value is not (null or IInvocableFunction))
                throw new ArgumentException($"funcref must be {nameof(IInvocableFunction)}.", nameof(value));
            return new StackValueItem(type) { valueI32 = References.GetHandle(value) };
        }

        public readonly bool IsNullReference => valueType.IsReference() && valueI32 == 0;

        /// <summary>
        ///     Gets the object of a reference value. Returns <c>null</c> for the null reference.
        /// </summary>
        public readonly object ExpectReference()
        {
            if (!valueType.IsReference())
                throw new InvalidCodeException($"StackValueType expected: reference, actual: {valueType}");
            return References.Get(valueI32);
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

        public readonly bool TryGetValue(out V128 value)
        {
            if (valueType != ValueType.V128)
            {
                value = default;
                return false;
            }

            value = valueV128;
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

        public readonly V128 ExpectValueV128()
        {
            if (valueType != ValueType.V128)
                throw new InvalidCodeException($"StackValueType expected: v128, actual: {valueType}");
            return valueV128;
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
                case ValueType.V128:
                    return valueV128.Equals(other.valueV128);
                case ValueType.FuncRef:
                case ValueType.ExternRef:
                    return valueI32 == other.valueI32;
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
                case ValueType.V128:
                    code.Add(valueV128);
                    break;
                case ValueType.FuncRef:
                case ValueType.ExternRef:
                    code.Add(valueI32);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return code.ToHashCode();
        }
    }
}