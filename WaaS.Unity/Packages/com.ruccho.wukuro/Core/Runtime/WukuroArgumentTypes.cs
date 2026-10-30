using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;

namespace Wukuro
{
    /// <summary>
    ///     Records and enums defined in C# that can be parameters of entry points (argument types). An argument type is a
    ///     record (<see cref="ComponentRecordAttribute" />) or an enum declared in an interface with
    ///     <see cref="ComponentInterfaceAttribute" />, so the same type can be a parameter of host functions.
    /// </summary>
    public static class WukuroArgumentTypes
    {
        private static readonly ConcurrentDictionary<Type, Action<object, ValuePusher>> Pushers = new();
        private static Dictionary<(string, string), Type> types;

        /// <summary>
        ///     Whether the type is a record or an enum declared in a component interface.
        /// </summary>
        public static bool IsArgumentType(Type type)
        {
            if (type == null || type.IsGenericType || GetInterfaceName(type) == null) return false;
            return type.IsEnum || type.IsDefined(typeof(ComponentRecordAttribute), false);
        }

        /// <summary>
        ///     The interface that declares the type, such as "my-game:events/types", or null if it is not declared in a
        ///     component interface.
        /// </summary>
        public static string GetInterfaceName(Type type)
        {
            return type.DeclaringType?.GetCustomAttribute<ComponentInterfaceAttribute>()?.Name;
        }

        /// <summary>
        ///     The name of the type in WIT, such as "door-settings" for <c>DoorSettings</c>.
        /// </summary>
        public static string GetWitName(Type type)
        {
            return ToComponentName(type.Name);
        }

        /// <summary>
        ///     Finds the argument type declared in the interface (such as "my-game:events/types") with the name in WIT, or
        ///     returns null.
        /// </summary>
        public static Type Find(string interfaceName, string witName)
        {
            if (string.IsNullOrEmpty(interfaceName) || string.IsNullOrEmpty(witName)) return null;

            // the version of the package is not a part of the names in C#
            var at = interfaceName.IndexOf('@');
            if (at >= 0) interfaceName = interfaceName.Substring(0, at);

            types ??= AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetTypes)
                .Where(t => t.IsInterface && t.IsDefined(typeof(ComponentInterfaceAttribute), false))
                .SelectMany(t => t.GetNestedTypes())
                .Where(IsArgumentType)
                .GroupBy(t => (GetInterfaceName(t), GetWitName(t)))
                .ToDictionary(g => g.Key, g => g.First());
            return types.TryGetValue((interfaceName, witName), out var type) ? type : null;
        }

        /// <summary>
        ///     The fields of a record in the order of the fields in WIT.
        /// </summary>
        public static IReadOnlyList<MemberInfo> GetFields(Type record)
        {
            return record.GetMembers(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m is FieldInfo or PropertyInfo && m.IsDefined(typeof(ComponentFieldAttribute), false))
                .OrderBy(m => m.MetadataToken)
                .ToArray();
        }

        public static Type GetFieldType(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => field.FieldType,
                PropertyInfo property => property.PropertyType,
                _ => throw new ArgumentException(nameof(member))
            };
        }

        /// <summary>
        ///     Converts a name in C# to the name in WIT in the same way as the source generator of WaaS
        ///     ("DoorSettings" -> "door-settings", "HTTPServer" -> "http-server").
        /// </summary>
        public static string ToComponentName(string name)
        {
            var builder = new StringBuilder(name.Length + 8);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c))
                {
                    var previous = name[i - 1];
                    var isNextLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                    if (!char.IsUpper(previous) || isNextLower) builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }

        /// <summary>
        ///     Pushes a value of an argument type with the formatter of its type.
        /// </summary>
        internal static void Push(object value, ValuePusher pusher)
        {
            var push = Pushers.GetOrAdd(value.GetType(), type =>
                (Action<object, ValuePusher>)typeof(WukuroArgumentTypes)
                    .GetMethod(nameof(CreatePusher), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(type)
                    .Invoke(null, null));
            push(value, pusher);
        }

        private static Action<object, ValuePusher> CreatePusher<T>()
        {
            return (value, pusher) => FormatterProvider.GetFormatter<T>().Push((T)value, pusher);
        }

        private static IEnumerable<Type> GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }
    }
}
