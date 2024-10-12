#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Optional reference type to indicate formatter that the value is nullable.
    ///     For value types, use Nullable&lt;T&gt; instead.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct Option<T>
    {
        public readonly bool isSome;
        public readonly T value;

        public bool TryGetValue([NotNullWhen(true)] out T value)
        {
            if (isSome)
            {
                value = this.value;
                return true;
            }

            value = default;
            return false;
        }

        public Option(T value)
        {
            this.isSome = true;
            this.value = value;
        }
    }
}