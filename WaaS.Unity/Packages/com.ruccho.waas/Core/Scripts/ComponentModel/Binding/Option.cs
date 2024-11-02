#nullable enable

using System;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Optional reference type to indicate formatter that the value is nullable.
    ///     For value types, use Nullable&lt;T&gt; instead.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ComponentVariant]
    public readonly partial struct Option<T>
    {
        [ComponentCase] public None? None { get; private init; }

        [ComponentCase] public T? Some { get; private init; }

        public Option(T value)
        {
            Some = value;
            None = null;
            Case = VariantCase.Some;
        }

        public static readonly Option<T> NoneValue = new()
        {
            Some = default,
            None = new None(),
            Case = VariantCase.None
        };
    }

    public static class OptionExtensions
    {
        public static T? ToNullable<T>(this in Option<T> option) where T : struct
        {
            return option.Case switch
            {
                Option<T>.VariantCase.None => null,
                Option<T>.VariantCase.Some => option.Some,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static Option<T> ToOption<T>(this in T? nullable) where T : struct
        {
            return nullable.HasValue ? new Option<T>(nullable.Value) : Option<T>.NoneValue;
        }
    }
}