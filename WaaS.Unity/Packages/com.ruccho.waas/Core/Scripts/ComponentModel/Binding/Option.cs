#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Component option type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ComponentVariant]
    [StructLayout(LayoutKind.Auto)]
    public readonly partial struct Option<T> : IEquatable<Option<T>>
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

        public bool Equals(Option<T> other)
        {
            if (Case != other.Case) return false;

            return Case switch
            {
                VariantCase.None => None!.Value == other.None!.Value,
                VariantCase.Some => EqualityComparer<T?>.Default.Equals(Some, other.Some),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override bool Equals(object? obj)
        {
            return obj is Option<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Case switch
            {
                VariantCase.None => HashCode.Combine(None, (int)Case),
                VariantCase.Some => HashCode.Combine(Some, (int)Case),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static bool operator ==(Option<T> left, Option<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Option<T> left, Option<T> right)
        {
            return !left.Equals(right);
        }
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