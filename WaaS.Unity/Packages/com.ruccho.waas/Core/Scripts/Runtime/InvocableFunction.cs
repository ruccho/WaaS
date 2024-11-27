using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a function that can be invoked from a WebAssembly module.
    /// </summary>
    public interface IInvocableFunction : IExternal
    {
        FunctionType Type { get; }
        StackFrame CreateFrame(ExecutionContext context, ReadOnlySpan<StackValueItem> inputValues);
    }
}