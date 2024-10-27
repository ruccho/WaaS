using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    public interface IInvocableFunction : IExternal
    {
        FunctionType Type { get; }
        StackFrame CreateFrame(ExecutionContext context, ReadOnlySpan<StackValueItem> inputValues);
    }
}