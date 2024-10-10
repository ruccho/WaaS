#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Optional reference type to indicate formatter that the value is nullable.
    ///     For value types, use Nullable&lt;T&gt; instead.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct Option<T> where T : class
    {
        public readonly T? value;

        public bool TryGetValue([NotNullWhen(true)] out T? value)
        {
            if (this.value != null)
            {
                value = this.value;
                return true;
            }

            value = null;
            return false;
        }
    }
}