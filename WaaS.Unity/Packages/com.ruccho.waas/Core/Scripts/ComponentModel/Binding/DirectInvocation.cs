#nullable enable
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Conversions from core values used by the generated bindings. Numbers lifted from a guest have the same
    ///     canonicalization as <see cref="WaaS.ComponentModel.Runtime.ValueLifter" />.
    /// </summary>
    public static class DirectInvocation
    {
        public static float LiftF32(in StackValueItem value)
        {
            var result = value.ExpectValueF32();
            if (float.IsNaN(result)) Unsafe.As<float, uint>(ref result) = 0x7fc00000;
            return result;
        }

        public static double LiftF64(in StackValueItem value)
        {
            var result = value.ExpectValueF64();
            if (double.IsNaN(result)) Unsafe.As<double, ulong>(ref result) = 0x7ff8000000000000;
            return result;
        }

        /// <summary>
        ///     Calls realloc of a guest and returns the pointer.
        /// </summary>
        internal static uint Realloc(ExecutionContext context, ICanonOptions options, uint alignment, uint size)
        {
            var realloc = options.ReallocFunction ?? throw new TrapException("The realloc option is not specified.");

            Span<StackValueItem> args = stackalloc StackValueItem[5];
            args[0] = new StackValueItem(0u);
            args[1] = new StackValueItem(0u);
            args[2] = new StackValueItem(alignment);
            args[3] = new StackValueItem(size);
            context.InterruptFrame(realloc, args[..4], args[4..]);
            var ptr = args[4].ExpectValueI32();
            if ((ptr & (alignment - 1)) != 0) throw new TrapException("Realloc returned unaligned pointer");
            return ptr;
        }

        /// <summary>
        ///     Stores a string encoded in UTF-8 into the memory of a guest.
        /// </summary>
        internal static void LowerString(ExecutionContext context, ICanonOptions options, string value, out uint ptr,
            out uint length)
        {
            var encoding = Encoding.UTF8;
            length = (uint)encoding.GetByteCount(value);
            ptr = Realloc(context, options, 1, length);
            encoding.GetBytes(value, options.MemoryToRealloc!.Span.Slice(checked((int)ptr), (int)length));
        }
    }

    /// <summary>
    ///     What a host function called directly needs to access the guest. It is used by the generated bindings.
    /// </summary>
    public readonly struct DirectInvocationContext
    {
        private readonly LoweredFunction function;
        private readonly ExecutionContext context;

        internal DirectInvocationContext(LoweredFunction function, ExecutionContext context)
        {
            this.function = function;
            this.context = context;
        }

        private Span<byte> Memory(uint ptr, int length)
        {
            return function.MemoryToRealloc!.Span.Slice(checked((int)ptr), length);
        }

        public string LiftString(in StackValueItem ptr, in StackValueItem length)
        {
            var bytes = function.MemoryToRealloc!.Span.Slice(checked((int)ptr.ExpectValueI32()),
                checked((int)length.ExpectValueI32()));
            return Encoding.UTF8.GetString(bytes);
        }

        public Borrowed<T> LiftBorrowed<T>(in StackValueItem index, IResourceType type) where T : IResourceImpl
        {
            var handle = HandleTable.Of(function.Instance).Get(index.ExpectValueI32(), type);

            // a lifted borrow must not drop the resource when the handle is released
            return (Borrowed<T>)Ownership.GetHandle(type, handle.Rep, false).Borrow();
        }

        /// <summary>
        ///     Ends a borrow lifted by <see cref="LiftBorrowed{T}" /> when the host function has returned.
        /// </summary>
        public void ReleaseBorrowed<T>(Borrowed<T> borrowed) where T : IResourceImpl
        {
            borrowed.handle.Owned.Dispose();
        }

        public void CheckResultPointer(uint ptr, byte alignmentRank)
        {
            if (Utils.ElementSizeAlignTo(ptr, alignmentRank) != ptr)
                throw new TrapException("Result pointer is not aligned");
        }

        public void Store(uint ptr, uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(Memory(ptr, sizeof(uint)), value);
        }

        public void Store(uint ptr, ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(Memory(ptr, sizeof(ulong)), value);
        }

        public void Store(uint ptr, float value)
        {
            Store(ptr, Unsafe.As<float, uint>(ref value));
        }

        public void Store(uint ptr, double value)
        {
            Store(ptr, Unsafe.As<double, ulong>(ref value));
        }

        /// <summary>
        ///     Stores a string as a result. The pointer and the length are written at <paramref name="resultPointer" />.
        /// </summary>
        public void StoreString(uint resultPointer, string value)
        {
            CheckResultPointer(resultPointer, 2);
            DirectInvocation.LowerString(context, function, value, out var ptr, out var length);
            Store(resultPointer, ptr);
            Store(resultPointer + sizeof(uint), length);
        }
    }
}
