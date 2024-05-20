using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WaaS.Models;

namespace WaaS.Runtime.Bindings
{
    public static class BindingExtensions
    {
        public static ValueTask<TResult> InvokeAsync<TResult>(this Binder binder, ExecutionContext context,
            IInvocableFunction function, params object[] parameters)
        {
            ValueTask task;
            {
                var marshalContext = binder.GetMarshalContext();
                var disposed = false;
                try
                {
                    var allocated = false;

                    Start:
                    Span<StackValueItem> parameterValues =
                        stackalloc StackValueItem[allocated ? marshalContext.AllocateLength : 0];
                    var marshalStack = new MarshalStack<StackValueItem>(parameterValues);

                    do
                    {
                        switch (marshalContext.MoveNext())
                        {
                            case MarshallerActionKind.End:
                            {
                                if (!marshalStack.End) throw new InvalidOperationException();
                                goto End;
                            }
                            case MarshallerActionKind.Iterate:
                            {
                                foreach (var parameter in parameters)
                                    marshalContext.IterateValueBoxed(parameter, ref marshalStack);

                                break;
                            }
                            case MarshallerActionKind.Allocate:
                            {
                                if (allocated) throw new InvalidOperationException();
                                allocated = true;
                                goto Start;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    } while (true);

                    End:

                    disposed = true;
                    marshalContext.Dispose();

                    task = context.InvokeAsync(function, parameterValues);
                }
                finally
                {
                    if (!disposed) marshalContext.Dispose();
                }
            }

            return InvokeAsyncCore(binder, context, task);

            static async ValueTask<TResult> InvokeAsyncCore(Binder binder, ExecutionContext context, ValueTask task)
            {
                await task;
                return GetResult(binder, context);
            }

            static TResult GetResult(Binder binder, ExecutionContext context)
            {
                Span<StackValueItem> resultValues = stackalloc StackValueItem[context.ResultLength];
                context.TakeResults(resultValues);

                var unmarshalQueue = new UnmarshalQueue<StackValueItem>(resultValues);

                TResult result = default;
                {
                    using var unmarshalContext = binder.GetUnmarshalContext();

                    do
                    {
                        switch (unmarshalContext.MoveNext())
                        {
                            case MarshallerActionKind.End:
                            {
                                if (!unmarshalQueue.End) throw new InvalidOperationException();
                                goto End;
                            }
                            case MarshallerActionKind.Iterate:
                            {
                                unmarshalContext.IterateValue(out result, ref unmarshalQueue);
                                break;
                            }
                            case MarshallerActionKind.Allocate:
                            {
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    } while (true);

                    End: ;
                }

                return result;
            }
        }

        public static unsafe TResult Invoke<TResult>(this Binder binder, ExecutionContext context,
            IInvocableFunction function, params object[] parameters)
        {
            {
                var marshalContext = binder.GetMarshalContext();
                var disposed = false;
                try
                {
                    var allocated = false;

                    Start:
                    Span<StackValueItem> parameterValues =
                        stackalloc StackValueItem[allocated ? marshalContext.AllocateLength : 0];
                    var marshalStack = new MarshalStack<StackValueItem>(parameterValues);

                    do
                    {
                        switch (marshalContext.MoveNext())
                        {
                            case MarshallerActionKind.End:
                            {
                                if (!marshalStack.End) throw new InvalidOperationException();
                                goto End;
                            }
                            case MarshallerActionKind.Iterate:
                            {
                                foreach (var parameter in parameters)
                                    marshalContext.IterateValueBoxed(parameter, ref marshalStack);

                                break;
                            }
                            case MarshallerActionKind.Allocate:
                            {
                                if (allocated) throw new InvalidOperationException();
                                allocated = true;
                                goto Start;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    } while (true);

                    End:

                    disposed = true;
                    marshalContext.Dispose();

                    context.Invoke(function, parameterValues);
                }
                finally
                {
                    if (!disposed) marshalContext.Dispose();
                }
            }


            Span<StackValueItem> resultValues = stackalloc StackValueItem[context.ResultLength];
            context.TakeResults(resultValues);

            var unmarshalQueue = new UnmarshalQueue<StackValueItem>(resultValues);

            TResult result = default;
            {
                using var unmarshalContext = binder.GetUnmarshalContext();

                do
                {
                    switch (unmarshalContext.MoveNext())
                    {
                        case MarshallerActionKind.End:
                        {
                            if (!unmarshalQueue.End) throw new InvalidOperationException();
                            goto End;
                        }
                        case MarshallerActionKind.Iterate:
                        {
                            unmarshalContext.IterateValue(out result, ref unmarshalQueue);

                            break;
                        }
                        case MarshallerActionKind.Allocate:
                        {
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                } while (true);

                End: ;
            }

            return result;
        }

        public static unsafe ExternalFunction ToExternalFunction(this Binder binder, Delegate @delegate)
        {
            var method = @delegate.Method;

            ValueType[] resultTypes = default;
            if (method.ReturnType != typeof(void))
            {
                using var marshalContext = binder.GetMarshalContext();

                var allocated = false;
                do
                {
                    MarshalStack<ValueType> marshalStack = default;
                    switch (marshalContext.MoveNext())
                    {
                        case MarshallerActionKind.End:
                        {
                            if (!marshalStack.End) throw new InvalidOperationException();
                            goto End;
                        }
                        case MarshallerActionKind.Iterate:
                        {
                            marshalStack = new MarshalStack<ValueType>(resultTypes);
                            marshalContext.IterateValueTypeBoxed(method.ReturnType, ref marshalStack);

                            break;
                        }
                        case MarshallerActionKind.Allocate:
                        {
                            if (allocated) throw new InvalidOperationException();
                            allocated = true;
                            resultTypes = new ValueType[marshalContext.AllocateLength];
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                } while (true);

                End: ;
            }
            else
            {
                resultTypes = Array.Empty<ValueType>();
            }

            ValueType[] parameterTypes = default;
            var parameters = method.GetParameters();
            {
                using var unmarshalContext = binder.GetUnmarshalContext();

                var allocated = false;
                do
                {
                    MarshalStack<ValueType> marshalStack = default;
                    switch (unmarshalContext.MoveNext())
                    {
                        case MarshallerActionKind.End:
                        {
                            if (!marshalStack.End) throw new InvalidOperationException();
                            goto End;
                        }
                        case MarshallerActionKind.Iterate:
                        {
                            marshalStack = new MarshalStack<ValueType>(parameterTypes);
                            foreach (var parameter in parameters)
                                unmarshalContext.IterateValueTypeBoxed(parameter.ParameterType, ref marshalStack);

                            break;
                        }
                        case MarshallerActionKind.Allocate:
                        {
                            if (allocated) throw new InvalidOperationException();
                            allocated = true;
                            parameterTypes = new ValueType[unmarshalContext.AllocateLength];
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                } while (true);

                End: ;
            }

            var type = new FunctionType(parameterTypes, resultTypes);

            return new ExternalFunctionPointer(&Invoke, new Tuple<Binder, Delegate>(binder, @delegate), type);


            static void Invoke(object state, ReadOnlySpan<StackValueItem> parameters, Span<StackValueItem> result)
            {
                if (state is not Tuple<Binder, Delegate> tuple) throw new InvalidOperationException();
                var (binder, @delegate) = tuple;

                var method = @delegate.Method;

                var cliParameterInfo = method.GetParameters();
                var cliParameters = new object[cliParameterInfo.Length]; // NOTE: should we pool?
                {
                    using var unmarshalContext = binder.GetUnmarshalContext();

                    do
                    {
                        UnmarshalQueue<StackValueItem> unmarshalQueue = default;
                        switch (unmarshalContext.MoveNext())
                        {
                            case MarshallerActionKind.End:
                            {
                                if (!unmarshalQueue.End) throw new InvalidOperationException();
                                goto End;
                            }
                            case MarshallerActionKind.Iterate:
                            {
                                unmarshalQueue = new UnmarshalQueue<StackValueItem>(parameters);

                                for (var i = 0; i < cliParameterInfo.Length; i++)
                                {
                                    var parameter = cliParameterInfo[i];
                                    unmarshalContext.IterateValueBoxed(parameter.ParameterType, out cliParameters[i],
                                        ref unmarshalQueue);
                                }

                                break;
                            }
                            case MarshallerActionKind.Allocate:
                            {
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    } while (true);

                    End: ;
                }

                var cliResult = @delegate.DynamicInvoke(cliParameters);

                if (method.ReturnType != typeof(void))
                {
                    using var marshalContext = binder.GetMarshalContext();

                    do
                    {
                        MarshalStack<StackValueItem> marshalStack = default;
                        switch (marshalContext.MoveNext())
                        {
                            case MarshallerActionKind.End:
                            {
                                if (!marshalStack.End) throw new InvalidOperationException();
                                goto End;
                            }
                            case MarshallerActionKind.Iterate:
                            {
                                marshalStack = new MarshalStack<StackValueItem>(result);
                                marshalContext.IterateValueBoxed(cliResult, ref marshalStack);
                                break;
                            }
                            case MarshallerActionKind.Allocate:
                            {
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    } while (true);

                    End: ;
                }
                else if (result.Length != 0)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public static AsyncExternalFunctionPointer ToAsyncExternalFunction(this Binder binder, Delegate @delegate)
        {
            var method = @delegate.Method;
            var returnType = method.ReturnType;

            // awaitable check
            if (returnType == typeof(void))
                throw new ArgumentException("delegate must return awaitable object", nameof(@delegate));

            var getAwaiterMethod = returnType.GetMethod(
                "GetAwaiter",
                BindingFlags.Public | BindingFlags.Instance,
                Type.DefaultBinder,
                CallingConventions.HasThis,
                Array.Empty<Type>(),
                Array.Empty<ParameterModifier>());

            if (getAwaiterMethod == null)
                throw new ArgumentException("delegate must return awaitable object", nameof(@delegate));

            var awaiterType = getAwaiterMethod.ReturnType;

            var getResultMethod = awaiterType.GetMethod(
                "GetResult",
                BindingFlags.Public | BindingFlags.Instance,
                Type.DefaultBinder,
                CallingConventions.HasThis,
                Array.Empty<Type>(),
                Array.Empty<ParameterModifier>());

            var resultType = getResultMethod.ReturnType;

            ValueType[] resultTypes = default;
            if (method.ReturnType != typeof(void))
            {
                using var marshalContext = binder.GetMarshalContext();

                var allocated = false;
                do
                {
                    MarshalStack<ValueType> marshalStack = default;
                    switch (marshalContext.MoveNext())
                    {
                        case MarshallerActionKind.End:
                        {
                            if (!marshalStack.End) throw new InvalidOperationException();
                            goto End;
                        }
                        case MarshallerActionKind.Iterate:
                        {
                            marshalStack = new MarshalStack<ValueType>(resultTypes);
                            marshalContext.IterateValueTypeBoxed(resultType, ref marshalStack);

                            break;
                        }
                        case MarshallerActionKind.Allocate:
                        {
                            if (allocated) throw new InvalidOperationException();
                            allocated = true;
                            resultTypes = new ValueType[marshalContext.AllocateLength];
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                } while (true);

                End: ;
            }
            else
            {
                resultTypes = Array.Empty<ValueType>();
            }

            ValueType[] parameterTypes = default;
            var parameters = method.GetParameters();
            {
                using var unmarshalContext = binder.GetUnmarshalContext();

                var allocated = false;
                do
                {
                    MarshalStack<ValueType> marshalStack = default;
                    switch (unmarshalContext.MoveNext())
                    {
                        case MarshallerActionKind.End:
                        {
                            if (!marshalStack.End) throw new InvalidOperationException();
                            goto End;
                        }
                        case MarshallerActionKind.Iterate:
                        {
                            marshalStack = new MarshalStack<ValueType>(parameterTypes);
                            foreach (var parameter in parameters)
                                unmarshalContext.IterateValueTypeBoxed(parameter.ParameterType, ref marshalStack);

                            break;
                        }
                        case MarshallerActionKind.Allocate:
                        {
                            if (allocated) throw new InvalidOperationException();
                            allocated = true;
                            parameterTypes = new ValueType[unmarshalContext.AllocateLength];
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                } while (true);

                End: ;
            }

            var type = new FunctionType(parameterTypes, resultTypes);

            unsafe
            {
                return new AsyncExternalFunctionPointer(&InvokeAsync1, new Tuple<Binder, Delegate>(binder, @delegate),
                    type);
            }

            static ValueTask InvokeAsync1(object state, ReadOnlySpan<StackValueItem> parameters,
                Memory<StackValueItem> result)
            {
                if (state is not Tuple<Binder, Delegate> tuple) throw new InvalidOperationException();
                var (binder, @delegate) = tuple;

                var method = @delegate.Method;

                var cliParameterInfo = method.GetParameters();
                var cliParameters = new object[cliParameterInfo.Length]; // NOTE: should we pool?
                {
                    using var unmarshalContext = binder.GetUnmarshalContext();

                    do
                    {
                        UnmarshalQueue<StackValueItem> unmarshalQueue = default;
                        switch (unmarshalContext.MoveNext())
                        {
                            case MarshallerActionKind.End:
                            {
                                if (!unmarshalQueue.End) throw new InvalidOperationException();
                                goto End;
                            }
                            case MarshallerActionKind.Iterate:
                            {
                                unmarshalQueue = new UnmarshalQueue<StackValueItem>(parameters);

                                for (var i = 0; i < cliParameterInfo.Length; i++)
                                {
                                    var parameter = cliParameterInfo[i];
                                    unmarshalContext.IterateValueBoxed(parameter.ParameterType, out cliParameters[i],
                                        ref unmarshalQueue);
                                }

                                break;
                            }
                            case MarshallerActionKind.Allocate:
                            {
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    } while (true);

                    End: ;
                }

                var resultTask = @delegate.DynamicInvoke(cliParameters);
                return InvokeAsync2(resultTask, method, binder, result);
            }

            static async ValueTask InvokeAsync2(object resultTask, MethodInfo method, Binder binder,
                Memory<StackValueItem> result)
            {
                InvokeAsync3(method, binder, result, await new BoxedAwaitableWrapper(resultTask));
            }

            static void InvokeAsync3(MethodInfo method, Binder binder, Memory<StackValueItem> result, object cliResult)
            {
                if (method.ReturnType != typeof(void))
                {
                    using var marshalContext = binder.GetMarshalContext();

                    do
                    {
                        MarshalStack<StackValueItem> marshalStack = default;
                        switch (marshalContext.MoveNext())
                        {
                            case MarshallerActionKind.End:
                            {
                                if (!marshalStack.End) throw new InvalidOperationException();
                                goto End;
                            }
                            case MarshallerActionKind.Iterate:
                            {
                                marshalStack = new MarshalStack<StackValueItem>(result.Span);
                                marshalContext.IterateValueBoxed(cliResult, ref marshalStack);
                                break;
                            }
                            case MarshallerActionKind.Allocate:
                            {
                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    } while (true);

                    End: ;
                }
                else if (result.Length != 0)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private readonly struct BoxedAwaitableWrapper
        {
            private readonly object awaitable;

            public BoxedAwaitableWrapper(object awaitable)
            {
                this.awaitable = awaitable;
            }

            public Awaiter GetAwaiter()
            {
                var type = awaitable.GetType();
                var method = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .First(m => m.Name == "GetAwaiter" && m.GetParameters().Length == 0);

                var awaiter = method.Invoke(awaitable, Array.Empty<object>());

                return new Awaiter(awaiter);
            }

            public readonly struct Awaiter : INotifyCompletion
            {
                private readonly object awaiter;

                public Awaiter(object awaiter)
                {
                    this.awaiter = awaiter;
                }

                public bool IsCompleted => (bool)awaiter.GetType()
                    .GetProperty("IsCompleted", BindingFlags.Public | BindingFlags.Instance)!.GetValue(awaiter);

                public void OnCompleted(Action continuation)
                {
                    awaiter.GetType().GetMethod("OnCompleted", new[] { typeof(Action) })!
                        .Invoke(awaiter, new object[] { continuation });
                }

                public object GetResult()
                {
                    return awaiter.GetType().GetMethod("GetResult", Array.Empty<Type>())!
                        .Invoke(awaiter, Array.Empty<object>());
                }
            }
        }
    }
}