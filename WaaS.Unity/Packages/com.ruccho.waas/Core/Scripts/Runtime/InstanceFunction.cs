using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents an instance of a function.
    /// </summary>
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

        public StackFrame CreateFrame(ExecutionContext context, ReadOnlySpan<StackValueItem> inputValues)
        {
            return new StackFrame(WasmStackFrame.Get(context, this, inputValues));
        }
    }
}