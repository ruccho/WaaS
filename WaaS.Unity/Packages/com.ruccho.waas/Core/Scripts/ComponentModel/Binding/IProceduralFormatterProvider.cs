#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Provides formatters procedurally.
    /// </summary>
    public interface IProceduralFormatterProvider
    {
        bool TryCreateFormatter<T>([NotNullWhen(true)] out IFormatter<T>? formatter);
    }
}