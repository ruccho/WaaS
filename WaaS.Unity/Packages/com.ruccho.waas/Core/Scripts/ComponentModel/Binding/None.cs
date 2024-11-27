#nullable enable
using System;
using STask;
using WaaS.ComponentModel.Runtime;

#pragma warning disable CS1998

namespace WaaS.ComponentModel.Binding
{
    public readonly struct None : IEquatable<None>
    {
        static None()
        {
            FormatterProvider.Register(new Formatter());
        }

        private class Formatter : IFormatter<None>
        {
            public async STask<None> PullAsync(Pullable adapter)
            {
                return default;
            }

            public void Push(None value, ValuePusher pusher)
            {
            }
        }

        public bool Equals(None other)
        {
            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is None other && Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(None left, None right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(None left, None right)
        {
            return !left.Equals(right);
        }
    }
}