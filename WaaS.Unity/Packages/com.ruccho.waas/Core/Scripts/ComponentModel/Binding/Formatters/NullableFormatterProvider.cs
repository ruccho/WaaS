using System;

namespace WaaS.ComponentModel.Binding
{
    internal class NullableFormatterProvider : IProceduralFormatterProvider
    {
        public bool TryCreateFormatter<T>(out IFormatter<T> formatter)
        {
            var type = typeof(T);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var valueType = type.GetGenericArguments()[0];
                var formatterType = typeof(NullableFormatter<>).MakeGenericType(valueType);
                formatter = (IFormatter<T>)Activator.CreateInstance(formatterType);
                return true;
            }

            formatter = default;
            return false;
        }
    }
}