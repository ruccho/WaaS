#nullable enable

using System;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    public static class FormatterProvider
    {
        private static readonly IProceduralFormatterProvider[] proceduralProviders;

        static FormatterProvider()
        {
            proceduralProviders = new IProceduralFormatterProvider[]
            {
                new TupleFormatterProvider(),
                new ReadOnlyMemoryFormatterProvider()
            };

            Register(new PrimitiveValueFormatter<bool>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.Bool }, static (value, pusher) => pusher.Push(value)));
            Register(new PrimitiveValueFormatter<byte>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.U8 }, static (value, pusher) => pusher.Push(value)));
            Register(new PrimitiveValueFormatter<sbyte>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.S8 }, static (value, pusher) => pusher.Push(value)));
            Register(new PrimitiveValueFormatter<ushort>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.U16 }, static (value, pusher) => pusher.Push(value)));
            Register(new PrimitiveValueFormatter<short>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.S16 }, static (value, pusher) => pusher.Push(value)));
            Register(new PrimitiveValueFormatter<uint>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.U32 }, static (value, pusher) => pusher.Push(value)));
            Register(new PrimitiveValueFormatter<int>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.S32 }, static (value, pusher) => pusher.Push(value)));
            Register(new PrimitiveValueFormatter<ulong>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.U64 }, static (value, pusher) => pusher.Push(value)));
            Register(new PrimitiveValueFormatter<long>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.S64 }, static (value, pusher) => pusher.Push(value)));
            Register(new PrimitiveValueFormatter<float>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.F32 }, static (value, pusher) => pusher.Push(value)));
            Register(new PrimitiveValueFormatter<double>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.F64 }, static (value, pusher) => pusher.Push(value)));
            Register(new PrimitiveValueFormatter<string>(new PrimitiveValueType
                { Kind = PrimitiveValueTypeKind.String }, static (value, pusher) => pusher.Push(value)));
            Register(new CharFormatter());
        }

        public static void Register<T>(IFormatter<T> formatter)
        {
            CacheCheck<T>.suppressAutoInitialization = true;
            Cache<T>.formatter ??= formatter;
        }

        public static IFormatter<T> GetFormatter<T>()
        {
            return Cache<T>.formatter ?? throw new InvalidOperationException();
        }

        private static class CacheCheck<T>
        {
            public static bool suppressAutoInitialization;
        }

        private static class Cache<T>
        {
            public static IFormatter<T>? formatter;

            static Cache()
            {
                if (CacheCheck<T>.suppressAutoInitialization) return;

                // resolve generic formatters
                foreach (var provider in proceduralProviders)
                    if (provider.TryCreateFormatter<T>(out var formatter))
                    {
                        Cache<T>.formatter = formatter;
                        return;
                    }
            }
        }
    }
}