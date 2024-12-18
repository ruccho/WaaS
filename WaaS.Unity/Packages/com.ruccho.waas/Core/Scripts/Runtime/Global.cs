﻿using System;
using System.Runtime.CompilerServices;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents global that can be accessed from a WebAssembly module.
    /// </summary>
    public class Global : IExternal
    {
        protected StackValueItem stackValue;

        internal Global(StackValueItem value)
        {
            stackValue = value;
        }

        public virtual Mutability Mutability => Mutability.Const;

        public ValueType ValueType => stackValue.valueType;

        public StackValueItem GetStackValue()
        {
            return stackValue;
        }
    }

    /// <summary>
    ///     Represents mutable global that can be accessed from a WebAssembly module.
    /// </summary>
    public class GlobalMutable : Global
    {
        internal GlobalMutable(StackValueItem value) : base(value)
        {
        }

        public sealed override Mutability Mutability => Mutability.Var;

        public void SetStackValue(StackValueItem value)
        {
            if (value.valueType != stackValue.valueType) throw new InvalidOperationException();
            stackValue = value;
        }
    }

    /// <summary>
    ///     Represents global that can be accessed from a WebAssembly module.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public sealed class Global<TValue> : Global where TValue : unmanaged
    {
        public Global(TValue value) : base(default)
        {
            var valueType = ValueTypeRegistry.GetFor<TValue>();
            if (!valueType.HasValue)
                throw new ArgumentException($"type {typeof(TValue)} cannot be used for global type.");

            stackValue = new StackValueItem(valueType.Value!);
            Unsafe.As<uint, TValue>(ref Unsafe.AsRef(in stackValue.valueI32)) = value;
        }

        public ref readonly TValue Value => ref Unsafe.As<uint, TValue>(ref Unsafe.AsRef(in stackValue.valueI32));
    }

    /// <summary>
    ///     Represents mutable global that can be accessed from a WebAssembly module.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public sealed class GlobalMutable<TValue> : GlobalMutable where TValue : unmanaged
    {
        public GlobalMutable(TValue value) : base(default)
        {
            var valueType = ValueTypeRegistry.GetFor<TValue>();
            if (!valueType.HasValue)
                throw new ArgumentException($"type {typeof(TValue)} cannot be used for global type.");

            stackValue = new StackValueItem(valueType.Value);
            Value = value;
        }

        public ref TValue Value => ref Unsafe.As<uint, TValue>(ref stackValue.valueI32);
    }
}