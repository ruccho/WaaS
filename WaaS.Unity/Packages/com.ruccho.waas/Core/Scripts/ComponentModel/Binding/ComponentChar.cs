#nullable enable

using System;

namespace WaaS.ComponentModel.Binding
{
    public readonly struct ComponentChar : IEquatable<ComponentChar>
    {
        public readonly uint value;

        public ComponentChar(uint value)
        {
            this.value = value;
        }

        public static implicit operator uint(ComponentChar value)
        {
            return value.value;
        }

        public static implicit operator ComponentChar(uint value)
        {
            return new ComponentChar(value);
        }

        public bool Equals(ComponentChar other)
        {
            return value == other.value;
        }

        public override bool Equals(object obj)
        {
            return obj is ComponentChar other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)value;
        }

        public static bool operator ==(ComponentChar left, ComponentChar right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentChar left, ComponentChar right)
        {
            return !left.Equals(right);
        }
    }
}