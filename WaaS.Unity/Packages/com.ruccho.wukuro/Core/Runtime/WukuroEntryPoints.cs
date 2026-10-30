using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WaaS.Unity;

namespace Wukuro
{
    /// <summary>
    ///     Conventions of the entry points exported by scripts.
    ///     An entry point `foo` is exported as the interface `wukuro-entry:foo/entry` with a function `run`.
    /// </summary>
    public static class WukuroEntryPoints
    {
        public const string PackageNamespace = "wukuro-entry";
        public const string InterfaceName = "entry";
        public const string FunctionName = "run";

        private static readonly Regex EntryRegex = new(
            @"package\s+wukuro-entry:(?<name>[a-z0-9-]+)\s*\{.*?interface\s+entry\s*\{(?<uses>.*?)\brun:\s*func\((?<params>[^)]*)\)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        // e.g. "use my-game:events/types.{door-settings, door-kind};"
        private static readonly Regex UseRegex = new(
            @"\buse\s+(?<interface>[^\s{]+?)\.\{(?<names>[^}]*)\}",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        /// <summary>
        ///     Creates the arguments of an entry point with the default values: zero, false, an empty string, an action
        ///     doing nothing, a null object, and the default values of records and enums. The parameters of the types
        ///     not supported by the inspector are not set. This is useful to run the scripts in tests.
        /// </summary>
        public static IArguments CreateDefaultArguments(EntryPointInfo entry)
        {
            return new Arguments(entry.Parameters.Where(p => p.IsArgument).Select(ArgumentContainerBase.Create)
                .Where(c => c != null).ToArray());
        }

        public static string GetExportName(string entry)
        {
            return $"{PackageNamespace}:{entry}/{InterfaceName}";
        }

        /// <summary>
        ///     Reads the entry points from the WIT of a component (<see cref="WaaS.Unity.ComponentAsset.Wit" />).
        /// </summary>
        public static IReadOnlyList<EntryPointInfo> Parse(string wit)
        {
            if (string.IsNullOrEmpty(wit)) return new EntryPointInfo[0];

            return EntryRegex.Matches(wit).Cast<Match>()
                .Where(m => wit.Contains($"export {GetExportName(m.Groups["name"].Value)};"))
                .Select(m => new EntryPointInfo(m.Groups["name"].Value,
                    ParseParameters(m.Groups["params"].Value, ParseUses(m.Groups["uses"].Value))))
                .OrderBy(e => e.Name)
                .ToArray();
        }

        /// <summary>
        ///     Name of the custom section where <c>#[entry]</c> records the object types of the parameters
        ///     (<c>&amp;ObjectOf&lt;T&gt;</c>). Each record is <c>entry\tparameter\ttype name\n</c>.
        /// </summary>
        public const string ObjectTypesSection = "wukuro-entry-types";

        /// <summary>
        ///     Reads the object types of the parameters recorded in the script. The key is (entry point, parameter) in
        ///     kebab-case and the value is the assembly-qualified name of the C# type.
        /// </summary>
        public static IReadOnlyDictionary<(string entry, string parameter), string> GetObjectTypes(ComponentAsset script)
        {
            var result = new Dictionary<(string, string), string>();
            if (script == null) return result;
            foreach (var section in script.GetCustomSections(ObjectTypesSection))
            foreach (var record in Encoding.UTF8.GetString(section.Span).Split('\n'))
            {
                var columns = record.Split('\t');
                if (columns.Length == 3) result[(columns[0], columns[1])] = columns[2];
            }

            return result;
        }

        private static IReadOnlyList<EntryParameterInfo> ParseParameters(string parameters,
            IReadOnlyDictionary<string, string> uses)
        {
            return parameters.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Select(p =>
                {
                    var colon = p.IndexOf(':');
                    var witType = p.Substring(colon + 1).Trim();
                    return new EntryParameterInfo(p.Substring(0, colon).Trim(), witType,
                        uses.TryGetValue(witType, out var @interface) ? @interface : null);
                })
                .ToArray();
        }

        /// <summary>
        ///     Reads the types used from other interfaces: name of the type -> interface.
        /// </summary>
        private static IReadOnlyDictionary<string, string> ParseUses(string body)
        {
            var result = new Dictionary<string, string>();
            foreach (Match use in UseRegex.Matches(body))
            {
                var @interface = use.Groups["interface"].Value;
                // the types of wukuro:core are given by the runner or by the built-in arguments
                if (@interface.StartsWith("wukuro:core/")) continue;
                foreach (var name in use.Groups["names"].Value.Split(','))
                {
                    var trimmed = name.Trim();
                    if (trimmed.Length > 0) result[trimmed] = @interface;
                }
            }

            return result;
        }
    }

    public class EntryPointInfo
    {
        public EntryPointInfo(string name, IReadOnlyList<EntryParameterInfo> parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        public string Name { get; }
        public IReadOnlyList<EntryParameterInfo> Parameters { get; }
    }

    public class EntryParameterInfo
    {
        public EntryParameterInfo(string name, string witType, string typeInterface = null)
        {
            Name = name;
            WitType = witType;
            TypeInterface = typeInterface;
        }

        /// <summary>
        ///     Name of the parameter in kebab-case.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Type of the parameter in WIT, such as "s32" or "borrow&lt;action&gt;".
        /// </summary>
        public string WitType { get; }

        /// <summary>
        ///     The interface that defines the type of the parameter, such as "my-game:events/types", if the type is a
        ///     record or an enum used from another interface. Null for the built-in types.
        /// </summary>
        public string TypeInterface { get; }

        /// <summary>
        ///     Whether the value is given by the arguments. The context is given by the runner.
        /// </summary>
        public bool IsArgument => WitType != "borrow<context>";
    }
}
