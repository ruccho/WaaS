using System;
using WaaS.Models;

namespace WaaS.Runtime.Bindings
{
    public static class BinderExtensions
    {
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
            var instance = @delegate.Target;

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

            return new ExternalFunctionDelegate(&Invoke, new Tuple<Binder, Delegate>(binder, @delegate), type);


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
    }
}