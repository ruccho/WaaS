using System;
using System.Collections.Generic;

namespace WaaS.Models
{
    /// <summary>
    ///     Contents of the "name" custom section, which gives names to the module and the functions for debugging.
    /// </summary>
    public class NameSection
    {
        private const string SectionName = "name";

        private static readonly IReadOnlyDictionary<uint, string> EmptyNames = new Dictionary<uint, string>();

        private NameSection(string moduleName, IReadOnlyDictionary<uint, string> functionNames)
        {
            ModuleName = moduleName;
            FunctionNames = functionNames;
        }

        /// <summary>
        ///     Name of the module, or null if it is not specified.
        /// </summary>
        public string ModuleName { get; }

        /// <summary>
        ///     Names of the functions keyed by the index in the function index space, which includes the imported
        ///     functions.
        /// </summary>
        public IReadOnlyDictionary<uint, string> FunctionNames { get; }

        public bool TryGetFunctionName(uint functionIndex, out string name)
        {
            return FunctionNames.TryGetValue(functionIndex, out name);
        }

        internal static NameSection Create(ReadOnlySpan<Section> sections)
        {
            foreach (var section in sections)
                if (section is CustomSection { Name: SectionName } custom)
                    return Parse(custom.Data.Span);

            return new NameSection(null, EmptyNames);
        }

        /// <summary>
        ///     Parses the content of the name section.
        ///     A malformed section does not make the module invalid, so the names read before the error are kept.
        /// </summary>
        private static NameSection Parse(ReadOnlySpan<byte> data)
        {
            string moduleName = null;
            Dictionary<uint, string> functionNames = null;

            var reader = new ModuleReader(data);
            try
            {
                while (reader.Available > 0)
                {
                    var id = reader.ReadUnaligned<byte>();
                    var size = reader.ReadUnalignedLeb128U32();
                    var next = reader.Position + checked((int)size);

                    switch (id)
                    {
                        case 0:
                        {
                            moduleName = reader.ReadUtf8String();
                            break;
                        }
                        case 1:
                        {
                            var count = reader.ReadVectorSize();
                            functionNames ??= new Dictionary<uint, string>();
                            for (var i = 0; i < count; i++)
                            {
                                var index = reader.ReadUnalignedLeb128U32();
                                var name = reader.ReadUtf8String();
                                functionNames[index] = name;
                            }

                            break;
                        }
                    }

                    // skip the rest of the subsection including unknown ones
                    var rest = next - reader.Position;
                    if (rest < 0) break;
                    if (rest > 0) reader.Read(rest);
                }
            }
            catch (Exception)
            {
                // ignore the rest of the malformed section
            }
            finally
            {
                reader.Dispose();
            }

            return new NameSection(moduleName, functionNames ?? EmptyNames);
        }
    }
}
