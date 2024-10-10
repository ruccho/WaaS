#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace WaaS.ComponentModel.Binding
{
    public class TupleFormatterProvider : IProceduralFormatterProvider
    {
        public bool TryCreateFormatter<T>([NotNullWhen(true)] out IFormatter<T>? formatter)
        {
            Type? formatterType;
            if (typeof(T).IsGenericType && (typeof(T).FullName?.StartsWith("System.Tuple`") ?? false))
            {
                var typeArguments = typeof(T).GetGenericArguments();
                formatterType = typeArguments.Length switch
                {
                    1 => typeof(TupleFormatter<>),
                    2 => typeof(TupleFormatter<,>),
                    3 => typeof(TupleFormatter<,,>),
                    4 => typeof(TupleFormatter<,,,>),
                    5 => typeof(TupleFormatter<,,,,>),
                    6 => typeof(TupleFormatter<,,,,,>),
                    7 => typeof(TupleFormatter<,,,,,,>),
                    8 => typeof(TupleFormatter<,,,,,,,>),
                    _ => null
                };

                formatterType = formatterType?.MakeGenericType(typeArguments);
            }
            else if (typeof(T).IsGenericType && (typeof(T).FullName?.StartsWith("System.ValueTuple`") ?? false))
            {
                var typeArguments = typeof(T).GetGenericArguments();
                formatterType = typeArguments.Length switch
                {
                    1 => typeof(ValueTupleFormatter<>),
                    2 => typeof(ValueTupleFormatter<,>),
                    3 => typeof(ValueTupleFormatter<,,>),
                    4 => typeof(ValueTupleFormatter<,,,>),
                    5 => typeof(ValueTupleFormatter<,,,,>),
                    6 => typeof(ValueTupleFormatter<,,,,,>),
                    7 => typeof(ValueTupleFormatter<,,,,,,>),
                    8 => typeof(ValueTupleFormatter<,,,,,,,>),
                    _ => null
                };

                formatterType = formatterType?.MakeGenericType(typeArguments);
            }
            else
            {
                formatterType = null;
            }


            if (formatterType == null)
            {
                formatter = null;
                return false;
            }

            formatter = Activator.CreateInstance(formatterType) as IFormatter<T>;
            return formatter != null;
        }
    }
}