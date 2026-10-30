using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wukuro.Editor
{
    /// <summary>
    ///     Generates the Rust marker types of the object types declared by <see cref="WukuroObjectTypeAttribute" />.
    /// </summary>
    public static class WukuroObjectTypeExporter
    {
        /// <summary>
        ///     The object types declared in the loaded assemblies, ordered by the Rust name.
        /// </summary>
        public static IReadOnlyList<Type> CollectTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetCustomAttributes(typeof(WukuroObjectTypeAttribute), false))
                .Cast<WukuroObjectTypeAttribute>()
                .Select(attribute => attribute.Type)
                .Where(type => type != null)
                .Distinct()
                .OrderBy(GetRustName, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        ///     The name of the Rust marker type, which is the name of the C# type.
        /// </summary>
        public static string GetRustName(Type type)
        {
            return type.Name;
        }

        /// <summary>
        ///     The name recorded for the type, which is resolved by <see cref="Type.GetType(string)" />.
        /// </summary>
        public static string GetTypeName(Type type)
        {
            return $"{type.FullName}, {type.Assembly.GetName().Name}";
        }

        public static string GenerateRust(IEnumerable<Type> types, string header)
        {
            var builder = new StringBuilder(header);
            var names = new HashSet<string>();
            foreach (var type in types)
            {
                var name = GetRustName(type);
                if (!names.Add(name))
                    throw new InvalidOperationException($"The Rust name {name} of {type.FullName} is duplicated.");

                builder.Append('\n');
                builder.Append($"/// `{type.FullName}`\n");
                builder.Append($"pub struct {name};\n\n");
                builder.Append($"impl ::wukuro::ObjectType for {name} {{\n");
                builder.Append($"    const TYPE_NAME: &'static str = \"{GetTypeName(type)}\";\n");
                builder.Append("}\n");
            }

            return builder.ToString();
        }

        /// <summary>
        ///     Generates the MoonBit marker types. The type name is read from the attribute by the MoonBit importer.
        /// </summary>
        public static string GenerateMoonBit(IEnumerable<Type> types, string header)
        {
            var builder = new StringBuilder(header);
            var names = new HashSet<string>();
            foreach (var type in types)
            {
                var name = GetRustName(type);
                if (!names.Add(name))
                    throw new InvalidOperationException($"The MoonBit name {name} of {type.FullName} is duplicated.");

                builder.Append('\n');
                builder.Append("///|\n");
                builder.Append($"/// `{type.FullName}`\n");
                builder.Append($"#wukuro.object_type(\"{GetTypeName(type)}\")\n");
                builder.Append($"pub struct {name} {{}}\n");
            }

            return builder.ToString();
        }
    }
}
