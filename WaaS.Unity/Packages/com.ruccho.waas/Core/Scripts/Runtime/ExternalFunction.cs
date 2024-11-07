using System;
using System.Linq;
using WaaS.Models;

namespace WaaS.Runtime
{
    public abstract class ExternalFunction : IInvocableFunction
    {
        public abstract FunctionType Type { get; }

        public StackFrame CreateFrame(ExecutionContext context, ReadOnlySpan<StackValueItem> inputValues)
        {
            return new StackFrame(ExternalStackFrame.Get(context, this, inputValues));
        }

        public abstract void Invoke(ExecutionContext context, ReadOnlySpan<StackValueItem> parameters,
            Span<StackValueItem> results);
    }

    public unsafe class ExternalFunctionPointer : ExternalFunction
    {
        private readonly delegate*<object /*state*/, ReadOnlySpan<StackValueItem> /*parameters*/
            , Span<StackValueItem> /*results*/, void> invoke;

        private readonly object state;

        public ExternalFunctionPointer(
            delegate*<object, ReadOnlySpan<StackValueItem>, Span<StackValueItem>, void> invoke, object state,
            FunctionType type)
        {
            this.invoke = invoke;
            this.state = state;
            Type = type;
        }

        public override FunctionType Type { get; }

        public override void Invoke(ExecutionContext context, ReadOnlySpan<StackValueItem> parameters,
            Span<StackValueItem> results)
        {
            invoke(state, parameters, results);
        }
    }

    public class ExternalFunctionDelegate : ExternalFunction
    {
        public delegate void InvokeDelegate(object state, ReadOnlySpan<StackValueItem> parameters,
            Span<StackValueItem> results);

        private readonly InvokeDelegate invoke;

        private readonly object state;

        public ExternalFunctionDelegate(
            InvokeDelegate invoke, object state,
            FunctionType type)
        {
            this.invoke = invoke;
            this.state = state;
            Type = type;
        }

        public override FunctionType Type { get; }

        public override void Invoke(ExecutionContext context, ReadOnlySpan<StackValueItem> parameters,
            Span<StackValueItem> results)
        {
            invoke?.Invoke(state, parameters, results);
        }
    }

    public class ExternalFunctionCoreBoxedDelegate : ExternalFunction
    {
        public ExternalFunctionCoreBoxedDelegate(Delegate @delegate)
        {
            Delegate = @delegate;
            var method = @delegate.Method;

            ValueType ParameterToValueType(Type t)
            {
                if (t == typeof(uint)) return ValueType.I32;
                if (t == typeof(ulong)) return ValueType.I64;
                if (t == typeof(float)) return ValueType.F32;
                if (t == typeof(double)) return ValueType.F64;
                throw new InvalidOperationException($"Unsupported type {t}");
            }

            Type = new FunctionType(method.GetParameters().Select(p => ParameterToValueType(p.ParameterType)).ToArray(),
                method.ReturnType == typeof(void)
                    ? Array.Empty<ValueType>()
                    : new[] { ParameterToValueType(method.ReturnType) });
        }

        private Delegate Delegate { get; }

        public override FunctionType Type { get; }

        public override void Invoke(ExecutionContext context, ReadOnlySpan<StackValueItem> parameters,
            Span<StackValueItem> results)
        {
            var method = Delegate.Method;

            var parameterInfos = method.GetParameters();

            if (parameterInfos.Length != parameters.Length)
                throw new InvalidOperationException("Signature doesn't match");

            var hasReturn = method.ReturnType != typeof(void);
            if (!hasReturn)
            {
                if (results.Length != 0)
                    throw new InvalidOperationException("Signature doesn't match");
            }
            else
            {
                if (results.Length != 1)
                    throw new InvalidOperationException("Signature doesn't match");

                var returnType = method.ReturnType;
                if (returnType != typeof(uint) &&
                    returnType != typeof(ulong) &&
                    returnType != typeof(float) &&
                    returnType != typeof(double))
                    throw new InvalidOperationException(
                        $"The external function contains incompatible return type: {returnType}");
            }


            var parametersObj = new object[parameterInfos.Length];

            for (var i = 0; i < parameterInfos.Length; i++)
            {
                var info = parameterInfos[i];
                var type = info.ParameterType;

                ref readonly var parameterOnStack = ref parameters[i];
                object parameter;
                if (type == typeof(uint))
                    parameter = parameterOnStack.ExpectValueI32();
                else if (type == typeof(ulong))
                    parameter = parameterOnStack.ExpectValueI64();
                else if (type == typeof(float))
                    parameter = parameterOnStack.ExpectValueF32();
                else if (type == typeof(double))
                    parameter = parameterOnStack.ExpectValueF64();
                else
                    throw new InvalidOperationException(
                        $"The external function contains incompatible parameter type: {type}");

                parametersObj[i] = parameter;
            }

            var result = Delegate.DynamicInvoke(parametersObj);

            if (hasReturn)
            {
                var item = result switch
                {
                    uint v => new StackValueItem(v),
                    ulong v => new StackValueItem(v),
                    float v => new StackValueItem(v),
                    double v => new StackValueItem(v),
                    _ => throw new InvalidOperationException(
                        $"The external function contains incompatible return type: {result.GetType()}")
                };

                results[0] = item;
            }
        }
    }
}