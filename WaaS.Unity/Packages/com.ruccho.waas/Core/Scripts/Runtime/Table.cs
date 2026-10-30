using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a table in the WebAssembly module.
    /// </summary>
    public abstract class Table : IExternal
    {
        protected Table(Limits limits)
        {
            Limits = limits;
        }

        /// <summary>
        ///     The limits declared when the table was created. <see cref="Size" /> is the current number of the elements.
        /// </summary>
        public Limits Limits { get; }

        public abstract uint Size { get; }

        /// <summary>
        ///     <see cref="ValueType.FuncRef" /> or <see cref="ValueType.ExternRef" />.
        /// </summary>
        public abstract ValueType ElementType { get; }

        internal Limits CurrentLimits => new(Size, Limits.Max);

        internal abstract object GetObject(uint index);
        internal abstract void SetObject(uint index, object value);

        /// <summary>
        ///     Grows the table by <paramref name="delta" /> elements initialized with <paramref name="value" />.
        /// </summary>
        /// <returns>The old size, or -1 if the table cannot grow.</returns>
        internal abstract int Grow(uint delta, object value);

        internal abstract void Fill(uint offset, uint count, object value);

        /// <summary>
        ///     Copies the elements between tables of the same element type. The ranges may overlap.
        /// </summary>
        internal abstract void CopyTo(Table dest, uint destOffset, uint srcOffset, uint count);

        internal void ThrowIfOutOfBounds(uint offset, uint count)
        {
            if ((ulong)offset + count > Size) throw new TrapException("out of bounds table access");
        }
    }

    /// <summary>
    ///     Represents a table in the WebAssembly module.
    /// </summary>
    /// <typeparam name="T">
    ///     <see cref="IInvocableFunction" /> for tables of <c>funcref</c>, and <see cref="object" /> for tables of
    ///     <c>externref</c>.
    /// </typeparam>
    public class Table<T> : Table where T : class
    {
        private T[] buffer;

        public Table(Limits limits) : base(limits)
        {
            if (typeof(T) != typeof(IInvocableFunction) && typeof(T) != typeof(object))
                throw new NotSupportedException(
                    $"The element type of tables must be {nameof(IInvocableFunction)} or object.");
            buffer = new T[limits.Min];
        }

        public Span<T> Elements => buffer;

        public override uint Size => (uint)buffer.Length;

        public override ValueType ElementType =>
            typeof(T) == typeof(IInvocableFunction) ? ValueType.FuncRef : ValueType.ExternRef;

        internal override object GetObject(uint index)
        {
            ThrowIfOutOfBounds(index, 1);
            return buffer[index];
        }

        internal override void SetObject(uint index, object value)
        {
            ThrowIfOutOfBounds(index, 1);
            buffer[index] = (T)value;
        }

        internal override int Grow(uint delta, object value)
        {
            var oldSize = (uint)buffer.Length;
            var newSize = (ulong)oldSize + delta;
            // the size is also limited by the size of arrays
            if (newSize > (Limits.Max ?? uint.MaxValue) || newSize > 0x1000_0000) return -1;
            if (delta == 0) return (int)oldSize;

            Array.Resize(ref buffer, (int)newSize);
            Array.Fill(buffer, (T)value, (int)oldSize, (int)delta);
            return (int)oldSize;
        }

        internal override void Fill(uint offset, uint count, object value)
        {
            ThrowIfOutOfBounds(offset, count);
            Array.Fill(buffer, (T)value, (int)offset, (int)count);
        }

        internal override void CopyTo(Table dest, uint destOffset, uint srcOffset, uint count)
        {
            ThrowIfOutOfBounds(srcOffset, count);
            dest.ThrowIfOutOfBounds(destOffset, count);
            if (dest is not Table<T> destTable) throw new InvalidCodeException();
            Array.Copy(buffer, (int)srcOffset, destTable.buffer, (int)destOffset, (int)count);
        }
    }
}
