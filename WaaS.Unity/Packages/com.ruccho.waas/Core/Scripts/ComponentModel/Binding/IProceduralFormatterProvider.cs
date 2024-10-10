#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace WaaS.ComponentModel.Binding
{
    public interface IProceduralFormatterProvider
    {
        bool TryCreateFormatter<T>([NotNullWhen(true)] out IFormatter<T>? formatter);
    }
}