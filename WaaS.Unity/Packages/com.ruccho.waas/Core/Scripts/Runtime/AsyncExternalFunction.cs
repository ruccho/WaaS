using System;
using System.Threading.Tasks;
using WaaS.Models;

namespace WaaS.Runtime
{
    public abstract class AsyncExternalFunction : IInvocableFunction
    {
        public abstract FunctionType Type { get; }
        public abstract ValueTask InvokeAsync(ReadOnlySpan<StackValueItem> parameters, Memory<StackValueItem> results);
    }
}