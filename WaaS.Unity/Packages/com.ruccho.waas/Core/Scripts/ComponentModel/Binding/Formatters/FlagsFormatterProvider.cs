using System;

namespace WaaS.ComponentModel.Binding
{
    internal class FlagsFormatterProvider : IProceduralFormatterProvider
    {
        public bool TryCreateFormatter<T>(out IFormatter<T> formatter)
        {
            if (typeof(T).IsEnum && Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                var type = typeof(FlagsFormatter<>).MakeGenericType(typeof(T));
                formatter = (IFormatter<T>)Activator.CreateInstance(type)!;
                return true;
            }

            formatter = default!;
            return false;
        }
    }
}