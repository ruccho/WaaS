#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Component result type.
    /// </summary>
    /// <typeparam name="TOk"></typeparam>
    /// <typeparam name="TError"></typeparam>
    [ComponentVariant]
    [StructLayout(LayoutKind.Auto)]
    public readonly partial struct Result<TOk, TError> : IEquatable<Result<TOk, TError>>
    {
        [ComponentCase] public TOk? Ok { get; private init; }
        [ComponentCase] public TError? Error { get; private init; }

        public bool Equals(Result<TOk, TError> other)
        {
            if (Case != other.Case) return false;

            return Case switch
            {
                VariantCase.Ok => EqualityComparer<TOk?>.Default.Equals(Ok, other.Ok),
                VariantCase.Error => EqualityComparer<TError?>.Default.Equals(Error, other.Error),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override bool Equals(object? obj)
        {
            return obj is Result<TOk, TError> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Case switch
            {
                VariantCase.Ok => HashCode.Combine(Ok, (int)Case),
                VariantCase.Error => HashCode.Combine(Error, (int)Case),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static bool operator ==(Result<TOk, TError> left, Result<TOk, TError> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Result<TOk, TError> left, Result<TOk, TError> right)
        {
            return !left.Equals(right);
        }
    }
}