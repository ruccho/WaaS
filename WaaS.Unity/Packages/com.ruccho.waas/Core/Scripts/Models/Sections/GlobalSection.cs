using System;

namespace WaaS.Models
{
    public class GlobalSection : Section
    {
        internal GlobalSection(ref ModuleReader reader)
        {
            var numGlobals = reader.ReadVectorSize();

            var globals = new Global[numGlobals];

            for (var i = 0; i < globals.Length; i++) globals[i] = new Global(ref reader);

            Globals = globals;
        }

        public ReadOnlyMemory<Global> Globals { get; }
    }

    public class Global
    {
        internal Global(ref ModuleReader reader)
        {
            Type = new GlobalType(ref reader);
            Expression = new ConstantExpression(ref reader);
        }

        public GlobalType Type { get; }
        public ConstantExpression Expression { get; }
    }

    public readonly struct GlobalType : IEquatable<GlobalType>
    {
        public ValueType ValueType { get; }

        public Mutability Mutability { get; }

        internal GlobalType(ref ModuleReader reader)
        {
            ValueType = reader.ReadUnaligned<ValueType>();
            Mutability = reader.ReadUnaligned<Mutability>();
        }


        public bool Equals(GlobalType other)
        {
            return ValueType == other.ValueType && Mutability == other.Mutability;
        }

        public override bool Equals(object obj)
        {
            return obj is GlobalType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)ValueType, (int)Mutability);
        }
    }

    public enum Mutability : byte
    {
        Const = 0,
        Var = 1
    }
}