#nullable enable
using WaaS.ComponentModel.Models;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Runtime
{
    public interface ICanonOptions
    {
        CanonOptionStringEncodingKind StringEncoding { get; }
        IInvocableFunction? ReallocFunction { get; }
        Memory? MemoryToRealloc { get; }
    }
}