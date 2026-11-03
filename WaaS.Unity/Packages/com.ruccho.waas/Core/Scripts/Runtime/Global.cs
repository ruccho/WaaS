using System;
using System.Runtime.CompilerServices;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents global that can be accessed from a WebAssembly module.
    /// </summary>
    public class Global : IExternal
    {
        // an array of one element, whose address can be fixed for the interpreter on pointers
        private readonly StackValueItem[] storage = new StackValueItem[1];

        protected internal ref StackValueItem stackValue => ref storage[0];

#if WAAS_NATIVE_INTERPRETER
        private System.Runtime.InteropServices.GCHandle storageHandle;

        internal unsafe StackValueItem* GetPointer()
        {
            if (!storageHandle.IsAllocated)
                storageHandle = System.Runtime.InteropServices.GCHandle.Alloc(storage,
                    System.Runtime.InteropServices.GCHandleType.Pinned);
            return (StackValueItem*)storageHandle.AddrOfPinnedObject();
        }

        ~Global()
        {
            if (storageHandle.IsAllocated) storageHandle.Free();
        }
#endif

        // the object of a reference value, which the global holds strongly
        private protected object referenceKeepAlive;

        internal Global(StackValueItem value)
        {
            stackValue = value;
            KeepReferenceAlive();
        }

        /// <summary>
        ///     Creates a global of a reference type (<c>funcref</c> or <c>externref</c>).
        /// </summary>
        public static Global CreateReference(ValueType type, object value, Mutability mutability = Mutability.Const)
        {
            var item = StackValueItem.FromReference(type, value);
            return mutability == Mutability.Var ? new GlobalMutable(item) : new Global(item);
        }

        private protected void KeepReferenceAlive()
        {
            referenceKeepAlive = stackValue.valueType.IsReference() ? stackValue.ExpectReference() : null;
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
            if (value.valueType.IsReference()) KeepReferenceAlive();
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