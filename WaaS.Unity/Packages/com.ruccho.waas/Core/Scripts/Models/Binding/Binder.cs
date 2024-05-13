using System;

namespace WaaS.Runtime.Bindings
{
    public abstract class Binder
    {
        public abstract IMarshalContext GetMarshalContext();
        public abstract IUnmarshalContext GetUnmarshalContext();
    }

    public enum MarshallerActionKind
    {
        End,
        Iterate,
        Allocate
    }

    public interface IMarshalContext : IDisposable
    {
        int AllocateLength { get; }

        MarshallerActionKind MoveNext();

        void IterateValue<T>(T value, ref MarshalStack<StackValueItem> stack);
        void IterateValueBoxed(object value, ref MarshalStack<StackValueItem> stack);

        void IterateValueType<T>(T value, ref MarshalStack<ValueType> types);
        void IterateValueTypeBoxed(Type type, ref MarshalStack<ValueType> types);
    }

    public interface IUnmarshalContext : IDisposable
    {
        int AllocateLength { get; }

        MarshallerActionKind MoveNext();

        void IterateValue<T>(out T value, ref UnmarshalQueue<StackValueItem> stack);
        void IterateValueBoxed(Type type, out object value, ref UnmarshalQueue<StackValueItem> stack);

        void IterateValueType<T>(T value, ref MarshalStack<ValueType> types);
        void IterateValueTypeBoxed(Type type, ref MarshalStack<ValueType> types);
    }

    public ref struct MarshalStack<T>
    {
        private readonly Span<T> values;
        private int position;

        public bool End => position >= values.Length;

        public MarshalStack(Span<T> values) : this()
        {
            this.values = values;
        }

        public void Push(T value)
        {
            values[position++] = value;
        }
    }

    public ref struct UnmarshalQueue<T>
    {
        private readonly ReadOnlySpan<T> values;
        private int position;

        public bool End => position >= values.Length;

        public UnmarshalQueue(ReadOnlySpan<T> values) : this()
        {
            this.values = values;
        }

        public T Dequeue()
        {
            return values[position++];
        }
    }
}