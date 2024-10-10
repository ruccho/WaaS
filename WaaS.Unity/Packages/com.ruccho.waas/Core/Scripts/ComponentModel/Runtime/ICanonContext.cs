#nullable enable

namespace WaaS.ComponentModel.Runtime
{
    internal interface ICanonContext
    {
        ICanonOptions Options { get; }
        IFunction ComponentFunction { get; }
        uint Realloc(uint originalPtr, uint originalSize, uint alignment, uint newSize);
    }
}