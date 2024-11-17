using System;
using System.Runtime.InteropServices;

namespace WaaS.Models
{
    /// <summary>
    ///     Represents types of parameters and results of a function.
    /// </summary>
    public class FunctionType
    {
        internal FunctionType(ref ModuleReader reader)
        {
            if (reader.ReadUnaligned<byte>() != 0x60) throw new InvalidModuleException("Invalid functype");

            {
                var numParamTypes = reader.ReadVectorSize();
                var parameterTypes = new ValueType[numParamTypes];
                for (var i = 0; i < numParamTypes; i++) parameterTypes[i] = reader.ReadUnaligned<ValueType>();

                ParameterTypes = parameterTypes;
            }

            {
                var numResultTypes = reader.ReadVectorSize();
                if (numResultTypes > 1) throw new InvalidModuleException("invalid result arity");
                var resultTypes = new ValueType[numResultTypes];
                for (var i = 0; i < numResultTypes; i++) resultTypes[i] = reader.ReadUnaligned<ValueType>();

                ResultTypes = resultTypes;
            }
        }

        public FunctionType(ReadOnlySpan<ValueType> parameterTypes, ReadOnlySpan<ValueType> resultTypes)
        {
            ParameterTypes = parameterTypes.ToArray();
            ResultTypes = resultTypes.ToArray();
        }

        internal FunctionType(ValueType[] parameterTypes, ValueType[] resultTypes)
        {
            ParameterTypes = parameterTypes;
            ResultTypes = resultTypes;
        }

        public ReadOnlyMemory<ValueType> ParameterTypes { get; }
        public ReadOnlyMemory<ValueType> ResultTypes { get; }

        public bool Match(FunctionType other)
        {
            if (this == other) return true;

            return MemoryMarshal.AsBytes(ParameterTypes.Span)
                       .SequenceEqual(MemoryMarshal.AsBytes(other.ParameterTypes.Span)) &&
                   MemoryMarshal.AsBytes(ResultTypes.Span)
                       .SequenceEqual(MemoryMarshal.AsBytes(other.ResultTypes.Span));
        }

        public override string ToString()
        {
            return $"({string.Join(", ", ParameterTypes.ToArray())}) -> ({string.Join(", ", ResultTypes.ToArray())})";
        }
    }
}