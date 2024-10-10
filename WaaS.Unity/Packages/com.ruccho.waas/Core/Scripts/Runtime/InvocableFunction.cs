using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    public interface IInvocableFunction : IExternal
    {
        FunctionType Type { get; }
        IStackFrame CreateFrame(ExecutionContext context, ReadOnlySpan<StackValueItem> inputValues);
    }

    public class InstanceFunction : IInvocableFunction
    {
        public readonly Function function;
        public readonly Instance instance;

        public InstanceFunction(Instance instance, Function function)
        {
            this.instance = instance;
            this.function = function;
        }

        public FunctionType Type => function.Type;

        public IStackFrame CreateFrame(ExecutionContext context, ReadOnlySpan<StackValueItem> inputValues)
        {
            // TODO: pooling
            return new WasmStackFrame(context, this, inputValues);
        }
    }
}