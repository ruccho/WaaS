using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Builds WIT packages from the definitions generated for interfaces with <see cref="ComponentInterfaceAttribute" />.
    /// </summary>
    public static class WitPackageBuilder
    {
        /// <summary>
        ///     Builds the WIT source of each package from the given types.
        ///     Types without <see cref="ComponentWitDefinitionAttribute" /> are ignored.
        /// </summary>
        /// <returns>package name (e.g. "my-namespace:my-package") -> WIT source of the package</returns>
        public static IReadOnlyDictionary<string, string> Build(IEnumerable<Type> types)
        {
            var definitions = types
                .Select(type => type.GetCustomAttribute<ComponentWitDefinitionAttribute>())
                .Where(definition => definition != null);
            return Build(definitions);
        }

        public static IReadOnlyDictionary<string, string> Build(
            IEnumerable<ComponentWitDefinitionAttribute> definitions)
        {
            var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var group in definitions.GroupBy(d => d.Package))
            {
                var builder = new StringBuilder();
                builder.Append("package ").Append(group.Key).Append(";\n");

                var names = new HashSet<string>();
                foreach (var definition in group.OrderBy(d => d.InterfaceName, StringComparer.Ordinal))
                {
                    if (!names.Add(definition.InterfaceName))
                        throw new InvalidOperationException(
                            $"The interface {group.Key}/{definition.InterfaceName} is defined more than once.");

                    builder.Append('\n');
                    builder.Append(definition.Definition);
                }

                result.Add(group.Key, builder.ToString());
            }

            return result;
        }

        /// <summary>
        ///     Returns a directory name for the package that can be used in a file system.
        /// </summary>
        public static string GetDirectoryName(string package)
        {
            return package.Replace(':', '-').Replace('@', '-');
        }
    }
}
