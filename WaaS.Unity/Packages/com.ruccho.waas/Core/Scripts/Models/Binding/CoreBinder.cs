using System;

namespace WaaS.Runtime.Bindings
{
    public class CoreBinder : Binder
    {
        [ThreadStatic] private static SharedBox<BinderContext> pooledBinderContext;

        static unsafe CoreBinder()
        {
            MarshallerRegistry<uint>.Instance = new MarshallerRegistry<uint>(
                ValueType.I32,
                &MarshalI32,
                &UnmarshalI32);

            MarshallerRegistry<int>.Instance = new MarshallerRegistry<int>(
                ValueType.I32,
                (delegate*<int, out StackValueItem, void>)
                (delegate*<uint, out StackValueItem, void>)&MarshalI32,
                (delegate*<in StackValueItem, out int, void>)
                (delegate*<in StackValueItem, out uint, void>)&UnmarshalI32);

            MarshallerRegistry<ulong>.Instance = new MarshallerRegistry<ulong>(
                ValueType.I64,
                &MarshalI64,
                &UnmarshalI64);

            MarshallerRegistry<long>.Instance = new MarshallerRegistry<long>(
                ValueType.I64,
                (delegate*<long, out StackValueItem, void>)
                (delegate*<ulong, out StackValueItem, void>)&MarshalI64,
                (delegate*<in StackValueItem, out long, void>)
                (delegate*<in StackValueItem, out ulong, void>)&UnmarshalI64);

            MarshallerRegistry<float>.Instance = new MarshallerRegistry<float>(
                ValueType.F32,
                &MarshalF32,
                &UnmarshalF32);

            MarshallerRegistry<double>.Instance = new MarshallerRegistry<double>(
                ValueType.F64,
                &MarshalF64,
                &UnmarshalF64);
        }

        private static void MarshalI32(uint source, out StackValueItem stackValue)
        {
            stackValue = new StackValueItem(source);
        }

        private static void MarshalI64(ulong source, out StackValueItem stackValue)
        {
            stackValue = new StackValueItem(source);
        }

        private static void MarshalF32(float source, out StackValueItem stackValue)
        {
            stackValue = new StackValueItem(source);
        }

        private static void MarshalF64(double source, out StackValueItem stackValue)
        {
            stackValue = new StackValueItem(source);
        }

        private static void UnmarshalI32(in StackValueItem stackValue, out uint result)
        {
            result = stackValue.ExpectValueI32();
        }

        private static void UnmarshalI64(in StackValueItem stackValue, out ulong result)
        {
            result = stackValue.ExpectValueI64();
        }

        private static void UnmarshalF32(in StackValueItem stackValue, out float result)
        {
            result = stackValue.ExpectValueF32();
        }

        private static void UnmarshalF64(in StackValueItem stackValue, out double result)
        {
            result = stackValue.ExpectValueF64();
        }

        public override IMarshalContext GetMarshalContext()
        {
            pooledBinderContext ??= new SharedBox<BinderContext>(new BinderContext());
            var instance = pooledBinderContext.LockAndGetInstance();
            return instance;
        }

        public override IUnmarshalContext GetUnmarshalContext()
        {
            pooledBinderContext ??= new SharedBox<BinderContext>(new BinderContext());
            var instance = pooledBinderContext.LockAndGetInstance();
            return instance;
        }

        private interface ISharedBoxItem<T> where T : ISharedBoxItem<T>
        {
            public SharedBox<T> Host { set; }
        }

        private class SharedBox<T> where T : ISharedBoxItem<T>
        {
            private T instance;
            private bool locked;

            public SharedBox(T instance)
            {
                this.instance = instance;
            }

            public T LockAndGetInstance()
            {
                if (locked) throw new InvalidOperationException();
                locked = true;
                instance.Host = this;
                return instance;
            }

            public void Unlock()
            {
                locked = false;
            }
        }

        private class MarshallerRegistry<T>
        {
            public readonly unsafe delegate*<T, out StackValueItem, void> marshaller;
            public readonly unsafe delegate*<in StackValueItem, out T, void> unmarshaller;

            public readonly ValueType? valueType;

            public unsafe MarshallerRegistry(ValueType? valueType, delegate*<T, out StackValueItem, void> marshaller,
                delegate*<in StackValueItem, out T, void> unmarshaller)
            {
                this.valueType = valueType;
                this.marshaller = marshaller;
                this.unmarshaller = unmarshaller;
            }

            public static MarshallerRegistry<T> Instance { get; set; }
        }

        private class BinderContext : IMarshalContext, IUnmarshalContext, ISharedBoxItem<BinderContext>
        {
            private StateKind state = StateKind.Entry;

            public int AllocateLength { get; private set; }

            public void Dispose()
            {
                Host.Unlock();
                Host = null;

                AllocateLength = 0;
                state = StateKind.Entry;
            }

            public SharedBox<BinderContext> Host { private get; set; }

            private enum StateKind
            {
                Entry,
                FirstIteration,
                Allocation,
                SecondIteration,
                End
            }

            #region Unmarshal

            public unsafe void IterateValue<T>(out T value, ref UnmarshalQueue<StackValueItem> stack)
            {
                if (state is StateKind.FirstIteration)
                {
                    AllocateLength++;
                    value = default;
                }
                else if (state is StateKind.SecondIteration)
                {
                    MarshallerRegistry<T>.Instance.unmarshaller(stack.Dequeue(), out value);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            public unsafe void IterateValueBoxed(Type type, out object value, ref UnmarshalQueue<StackValueItem> stack)
            {
                if (state is StateKind.FirstIteration)
                {
                    AllocateLength++;
                    value = default;
                }
                else if (state is StateKind.SecondIteration)
                {
                    if (type == typeof(int) || type == typeof(uint))
                    {
                        MarshallerRegistry<uint>.Instance.unmarshaller(stack.Dequeue(), out var valueTyped);
                        value = valueTyped;
                    }
                    else if (type == typeof(long) || type == typeof(ulong))
                    {
                        MarshallerRegistry<ulong>.Instance.unmarshaller(stack.Dequeue(), out var valueTyped);
                        value = valueTyped;
                    }
                    else if (type == typeof(float))
                    {
                        MarshallerRegistry<float>.Instance.unmarshaller(stack.Dequeue(), out var valueTyped);
                        value = valueTyped;
                    }
                    else if (type == typeof(double))
                    {
                        MarshallerRegistry<double>.Instance.unmarshaller(stack.Dequeue(), out var valueTyped);
                        value = valueTyped;
                    }
                    else
                    {
                        throw new ArgumentException(nameof(type));
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            #endregion

            #region Marshal

            public unsafe void IterateValue<T>(T value, ref MarshalStack<StackValueItem> stack)
            {
                if (state is StateKind.FirstIteration)
                {
                    AllocateLength++;
                }
                else if (state is StateKind.SecondIteration)
                {
                    MarshallerRegistry<T>.Instance.marshaller(value, out var stackValue);
                    stack.Push(stackValue);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            public unsafe void IterateValueBoxed(object value, ref MarshalStack<StackValueItem> stack)
            {
                if (state is StateKind.FirstIteration)
                {
                    AllocateLength++;
                }
                else if (state is StateKind.SecondIteration)
                {
                    var type = value.GetType();
                    StackValueItem stackValue;
                    if (type == typeof(int))
                        MarshallerRegistry<int>.Instance.marshaller((int)value, out stackValue);
                    else if (type == typeof(uint))
                        MarshallerRegistry<uint>.Instance.marshaller((uint)value, out stackValue);
                    else if (type == typeof(long))
                        MarshallerRegistry<long>.Instance.marshaller((long)value, out stackValue);
                    else if (type == typeof(ulong))
                        MarshallerRegistry<ulong>.Instance.marshaller((ulong)value, out stackValue);
                    else if (type == typeof(float))
                        MarshallerRegistry<float>.Instance.marshaller((float)value, out stackValue);
                    else if (type == typeof(double))
                        MarshallerRegistry<double>.Instance.marshaller((double)value, out stackValue);
                    else
                        throw new ArgumentException(nameof(type));

                    stack.Push(stackValue);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            #endregion

            #region Common

            public MarshallerActionKind MoveNext()
            {
                switch (state++)
                {
                    case StateKind.Entry:
                    {
                        return MarshallerActionKind.Iterate;
                    }
                    case StateKind.FirstIteration:
                    {
                        return MarshallerActionKind.Allocate;
                    }
                    case StateKind.Allocation:
                    {
                        return MarshallerActionKind.Iterate;
                    }
                    case StateKind.SecondIteration:
                    {
                        return MarshallerActionKind.End;
                    }
                    case StateKind.End:
                    {
                        throw new InvalidOperationException();
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public void IterateValueType<T>(T value, ref MarshalStack<ValueType> types)
            {
                if (state is StateKind.FirstIteration)
                    AllocateLength++;
                else if (state is StateKind.SecondIteration)
                    types.Push(MarshallerRegistry<T>.Instance.valueType.Value);
                else
                    throw new InvalidOperationException();
            }

            public void IterateValueTypeBoxed(Type type, ref MarshalStack<ValueType> types)
            {
                if (state is StateKind.FirstIteration)
                {
                    AllocateLength++;
                }
                else if (state is StateKind.SecondIteration)
                {
                    StackValueItem stackValue;
                    if (type == typeof(int))
                        types.Push(MarshallerRegistry<int>.Instance.valueType.Value);
                    else if (type == typeof(uint))
                        types.Push(MarshallerRegistry<uint>.Instance.valueType.Value);
                    else if (type == typeof(long))
                        types.Push(MarshallerRegistry<long>.Instance.valueType.Value);
                    else if (type == typeof(ulong))
                        types.Push(MarshallerRegistry<ulong>.Instance.valueType.Value);
                    else if (type == typeof(float))
                        types.Push(MarshallerRegistry<float>.Instance.valueType.Value);
                    else if (type == typeof(double))
                        types.Push(MarshallerRegistry<double>.Instance.valueType.Value);
                    else
                        throw new ArgumentException(nameof(type));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            #endregion
        }
    }
}