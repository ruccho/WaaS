using System;
using System.Threading.Tasks;
using WaaS.Models;

namespace WaaS.Runtime
{
    public abstract class AsyncExternalFunction : IInvocableFunction
    {
        public abstract FunctionType Type { get; }

        public StackFrame CreateFrame(ExecutionContext context, ReadOnlySpan<StackValueItem> inputValues)
        {
            return new StackFrame(AsyncExternalStackFrame.Get(this, inputValues));
        }

        public abstract ValueTask InvokeAsync(ReadOnlySpan<StackValueItem> parameters, Memory<StackValueItem> results);
    }


    public unsafe class AsyncExternalFunctionPointer : AsyncExternalFunction
    {
        private readonly delegate*<object /*state*/, ReadOnlySpan<StackValueItem> /*parameters*/
            , Memory<StackValueItem> /*results*/, ValueTask> invoke;

        private readonly object state;

        public AsyncExternalFunctionPointer(
            delegate*<object, ReadOnlySpan<StackValueItem>, Memory<StackValueItem>, ValueTask> invoke, object state,
            FunctionType type)
        {
            this.invoke = invoke;
            this.state = state;
            Type = type;
        }

        public override FunctionType Type { get; }

        public override ValueTask InvokeAsync(ReadOnlySpan<StackValueItem> parameters, Memory<StackValueItem> results)
        {
            return invoke(state, parameters, results);
        }
    }

    public class AsyncExternalFunctionDelegate : AsyncExternalFunction
    {
        public delegate ValueTask InvokeDelegate(object state, ReadOnlySpan<StackValueItem> parameters,
            Memory<StackValueItem> results);

        private readonly InvokeDelegate invoke;

        private readonly object state;

        public AsyncExternalFunctionDelegate(
            InvokeDelegate invoke, object state,
            FunctionType type)
        {
            this.invoke = invoke;
            this.state = state;
            Type = type;
        }

        public override FunctionType Type { get; }

        public override ValueTask InvokeAsync(ReadOnlySpan<StackValueItem> parameters, Memory<StackValueItem> results)
        {
            return invoke.Invoke(state, parameters, results);
        }
    }
}