using System;

namespace WaaS.ComponentModel.Binding
{
    public class EnumFormatterProvider : IProceduralFormatterProvider
    {
        public bool TryCreateFormatter<T>(out IFormatter<T> formatter)
        {
            if (typeof(T).IsEnum && !Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                formatter = (IFormatter<T>)Activator.CreateInstance(typeof(EnumFormatter<>).MakeGenericType(typeof(T)));
                return true;
            }

            formatter = default!;
            return false;
        }
    }
}