#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Models
{
    /// <summary>
    ///     Utilities to compare and describe types, used to validate imports and to build error messages.
    /// </summary>
    internal static class TypeDescriptions
    {
        private const int MaxDepth = 6;

        /// <summary>
        ///     Compares labels of records, variants, enums and flags.
        ///     Labels are compared ignoring the case and the separators so that the names generated from C# identifiers
        ///     (e.g. "YCoord" for "y-coord") match the names in WIT.
        /// </summary>
        public static bool LabelEquals(string lhs, string rhs)
        {
            int i = 0, j = 0;
            while (true)
            {
                while (i < lhs.Length && lhs[i] is '-' or '_') i++;
                while (j < rhs.Length && rhs[j] is '-' or '_') j++;
                if (i == lhs.Length || j == rhs.Length) return i == lhs.Length && j == rhs.Length;
                if (char.ToLowerInvariant(lhs[i]) != char.ToLowerInvariant(rhs[j])) return false;
                i++;
                j++;
            }
        }

        public static bool LabelsEqual(ReadOnlySpan<string> lhs, ReadOnlySpan<string> rhs)
        {
            if (lhs.Length != rhs.Length) return false;
            for (var i = 0; i < lhs.Length; i++)
                if (!LabelEquals(lhs[i], rhs[i]))
                    return false;
            return true;
        }

        /// <summary>
        ///     Returns the name of the sort of the item.
        /// </summary>
        public static string DescribeSort(ISortedExportable? item)
        {
            return item switch
            {
                null => "nothing",
                IFunction => "function",
                IInstance => "instance",
                IComponent => "component",
                IType => "type",
                ICoreModule => "core module",
                IValue => "value",
                _ => item.GetType().Name
            };
        }

        /// <summary>
        ///     Returns the name of the sort that the descriptor requires.
        /// </summary>
        public static string DescribeSort(IExportableDescriptor<ISortedExportable> desc)
        {
            return desc switch
            {
                ExportableDescriptorCoreModuleType => "core module",
                ExportableDescriptorFunction => "function",
                ExportableDescriptorType => "type",
                ExportableDescriptorComponent => "component",
                ExportableDescriptorInstance => "instance",
                _ => desc.GetType().Name
            };
        }

        /// <summary>
        ///     Returns the name of the sort of the export.
        /// </summary>
        public static string DescribeSort(IExport<ISortedExportable> export)
        {
            if (export.Descriptor != null) return DescribeSort(export.Descriptor);
            return export switch
            {
                IExport<IFunction> => "function",
                IExport<IInstance> => "instance",
                IExport<IComponent> => "component",
                IExport<IType> => "type",
                IExport<ICoreModule> => "core module",
                IExport<IValue> => "value",
                _ => export.GetType().Name
            };
        }

        /// <summary>
        ///     Describes the type in a WIT-like syntax.
        /// </summary>
        public static string Describe(IType? type)
        {
            var builder = new StringBuilder();
            Describe(type, builder, 0);
            return builder.ToString();
        }

        private static void Describe(IType? type, StringBuilder builder, int depth)
        {
            if (depth > MaxDepth)
            {
                builder.Append("...");
                return;
            }

            depth++;

            switch (type)
            {
                case null:
                    builder.Append("(none)");
                    break;
                case IPrimitiveValueType primitive:
                    builder.Append(primitive.Kind.ToString().ToLowerInvariant());
                    break;
                case IListType list:
                    builder.Append("list<");
                    Describe(list.ElementType, builder, depth);
                    builder.Append('>');
                    break;
                case IOptionType option:
                    builder.Append("option<");
                    Describe(option.Type, builder, depth);
                    builder.Append('>');
                    break;
                case IResultType result:
                    builder.Append("result");
                    if (result.Type == null && result.ErrorType == null) break;
                    builder.Append('<');
                    if (result.Type == null) builder.Append('_');
                    else Describe(result.Type, builder, depth);
                    if (result.ErrorType != null)
                    {
                        builder.Append(", ");
                        Describe(result.ErrorType, builder, depth);
                    }

                    builder.Append('>');
                    break;
                case ITupleType tuple:
                {
                    builder.Append("tuple<");
                    var cases = tuple.Cases.Span;
                    for (var i = 0; i < cases.Length; i++)
                    {
                        if (i > 0) builder.Append(", ");
                        Describe(cases[i], builder, depth);
                    }

                    builder.Append('>');
                    break;
                }
                case IRecordType record:
                {
                    builder.Append("record { ");
                    var fields = record.Fields.Span;
                    for (var i = 0; i < fields.Length; i++)
                    {
                        if (i > 0) builder.Append(", ");
                        builder.Append(fields[i].Label).Append(": ");
                        Describe(fields[i].Type, builder, depth);
                    }

                    builder.Append(" }");
                    break;
                }
                case IVariantType variant:
                {
                    builder.Append("variant { ");
                    var cases = variant.Cases.Span;
                    for (var i = 0; i < cases.Length; i++)
                    {
                        if (i > 0) builder.Append(", ");
                        builder.Append(cases[i].Label);
                        if (cases[i].Type == null) continue;
                        builder.Append('(');
                        Describe(cases[i].Type, builder, depth);
                        builder.Append(')');
                    }

                    builder.Append(" }");
                    break;
                }
                case IEnumType @enum:
                    builder.Append("enum { ").Append(string.Join(", ", @enum.Labels.ToArray())).Append(" }");
                    break;
                case IFlagsType flags:
                    builder.Append("flags { ").Append(string.Join(", ", flags.Labels.ToArray())).Append(" }");
                    break;
                case IOwnedType owned:
                    builder.Append("own<");
                    Describe(owned.Type, builder, depth);
                    builder.Append('>');
                    break;
                case IBorrowedType borrowed:
                    builder.Append("borrow<");
                    Describe(borrowed.Type, builder, depth);
                    builder.Append('>');
                    break;
                case IResourceType resource:
                    builder.Append(resource.Instance == null
                        ? $"resource ({resource.GetType().Name})"
                        : "resource (defined by a component)");
                    break;
                case IFunctionType function:
                {
                    builder.Append("func(");
                    var parameters = function.Parameters.Span;
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (i > 0) builder.Append(", ");
                        builder.Append(parameters[i].Label).Append(": ");
                        Describe(parameters[i].Type, builder, depth);
                    }

                    builder.Append(')');
                    if (function.Result != null)
                    {
                        builder.Append(" -> ");
                        Describe(function.Result, builder, depth);
                    }

                    break;
                }
                case IInstanceType instance:
                {
                    builder.Append("instance { ");
                    var first = true;
                    foreach (var (name, desc) in instance.Exports)
                    {
                        if (!first) builder.Append(", ");
                        first = false;
                        builder.Append(name).Append(": ").Append(DescribeSort(desc));
                    }

                    builder.Append(" }");
                    break;
                }
                case IComponentType:
                    builder.Append("component");
                    break;
                default:
                    builder.Append(type.GetType().Name);
                    break;
            }
        }

        /// <summary>
        ///     Finds where <paramref name="provided" /> differs from <paramref name="expected" />.
        /// </summary>
        /// <returns>A description of the difference, or null if the types match.</returns>
        public static string? FindMismatch(IType expected, IType provided)
        {
            if (expected.ValidateEquals(provided)) return null;
            return FindMismatchCore(expected, provided) ?? Unexpected(expected, provided);
        }

        private static string Unexpected(IType? expected, IType? provided)
        {
            return $"expected {Describe(expected)}, provided {Describe(provided)}";
        }

        private static string? FindMismatchOptional(IValueType? expected, IValueType? provided)
        {
            if (expected == null || provided == null)
                return expected == null && provided == null ? null : Unexpected(expected, provided);
            return FindMismatch(expected, provided);
        }

        private static string? FindMismatchLabels(string kind, ReadOnlySpan<string> expected,
            ReadOnlySpan<string> provided)
        {
            if (expected.Length != provided.Length)
                return $"expected {expected.Length} {kind}, provided {provided.Length}";
            for (var i = 0; i < expected.Length; i++)
                if (!LabelEquals(expected[i], provided[i]))
                    return $"{kind} {i}: expected '{expected[i]}', provided '{provided[i]}'";
            return null;
        }

        private static string? FindMismatchCore(IType expected, IType provided)
        {
            switch (expected)
            {
                case IFunctionType expectedFunction when provided is IFunctionType providedFunction:
                {
                    var expectedParameters = expectedFunction.Parameters.Span;
                    var providedParameters = providedFunction.Parameters.Span;
                    if (expectedParameters.Length != providedParameters.Length)
                        return
                            $"expected {expectedParameters.Length} parameters, provided {providedParameters.Length}";
                    for (var i = 0; i < expectedParameters.Length; i++)
                    {
                        var mismatch = FindMismatch(expectedParameters[i].Type, providedParameters[i].Type);
                        if (mismatch != null) return $"parameter '{expectedParameters[i].Label}': {mismatch}";
                    }

                    var resultMismatch = FindMismatchOptional(expectedFunction.Result, providedFunction.Result);
                    return resultMismatch != null ? $"result: {resultMismatch}" : null;
                }
                case IRecordType expectedRecord when provided is IRecordType providedRecord:
                {
                    var expectedFields = expectedRecord.Fields.Span;
                    var providedFields = providedRecord.Fields.Span;
                    if (expectedFields.Length != providedFields.Length)
                        return $"expected {expectedFields.Length} fields, provided {providedFields.Length}";
                    for (var i = 0; i < expectedFields.Length; i++)
                    {
                        if (!LabelEquals(expectedFields[i].Label, providedFields[i].Label))
                            return
                                $"field {i}: expected '{expectedFields[i].Label}', provided '{providedFields[i].Label}'";
                        var mismatch = FindMismatch(expectedFields[i].Type, providedFields[i].Type);
                        if (mismatch != null) return $"field '{expectedFields[i].Label}': {mismatch}";
                    }

                    return null;
                }
                case IVariantType expectedVariant when provided is IVariantType providedVariant:
                {
                    var expectedCases = expectedVariant.Cases.Span;
                    var providedCases = providedVariant.Cases.Span;
                    if (expectedCases.Length != providedCases.Length)
                        return $"expected {expectedCases.Length} cases, provided {providedCases.Length}";
                    for (var i = 0; i < expectedCases.Length; i++)
                    {
                        if (!LabelEquals(expectedCases[i].Label, providedCases[i].Label))
                            return
                                $"case {i}: expected '{expectedCases[i].Label}', provided '{providedCases[i].Label}'";
                        var mismatch = FindMismatchOptional(expectedCases[i].Type, providedCases[i].Type);
                        if (mismatch != null) return $"case '{expectedCases[i].Label}': {mismatch}";
                    }

                    return null;
                }
                case ITupleType expectedTuple when provided is ITupleType providedTuple:
                {
                    var expectedCases = expectedTuple.Cases.Span;
                    var providedCases = providedTuple.Cases.Span;
                    if (expectedCases.Length != providedCases.Length)
                        return $"expected {expectedCases.Length} elements, provided {providedCases.Length}";
                    for (var i = 0; i < expectedCases.Length; i++)
                    {
                        var mismatch = FindMismatch(expectedCases[i], providedCases[i]);
                        if (mismatch != null) return $"element {i}: {mismatch}";
                    }

                    return null;
                }
                case IEnumType expectedEnum when provided is IEnumType providedEnum:
                    return FindMismatchLabels("cases", expectedEnum.Labels.Span, providedEnum.Labels.Span);
                case IFlagsType expectedFlags when provided is IFlagsType providedFlags:
                    return FindMismatchLabels("flags", expectedFlags.Labels.Span, providedFlags.Labels.Span);
                case IListType expectedList when provided is IListType providedList:
                {
                    var mismatch = FindMismatch(expectedList.ElementType, providedList.ElementType);
                    return mismatch != null ? $"list element: {mismatch}" : null;
                }
                case IOptionType expectedOption when provided is IOptionType providedOption:
                {
                    var mismatch = FindMismatch(expectedOption.Type, providedOption.Type);
                    return mismatch != null ? $"option: {mismatch}" : null;
                }
                case IResultType expectedResult when provided is IResultType providedResult:
                {
                    var mismatch = FindMismatchOptional(expectedResult.Type, providedResult.Type);
                    if (mismatch != null) return $"ok: {mismatch}";
                    mismatch = FindMismatchOptional(expectedResult.ErrorType, providedResult.ErrorType);
                    return mismatch != null ? $"error: {mismatch}" : null;
                }
                case IOwnedType expectedOwned when provided is IOwnedType providedOwned:
                {
                    var mismatch = FindMismatch(expectedOwned.Type, providedOwned.Type);
                    return mismatch != null ? $"own: {mismatch} (a different resource type)" : null;
                }
                case IBorrowedType expectedBorrowed when provided is IBorrowedType providedBorrowed:
                {
                    var mismatch = FindMismatch(expectedBorrowed.Type, providedBorrowed.Type);
                    return mismatch != null ? $"borrow: {mismatch} (a different resource type)" : null;
                }
                case IInstanceType expectedInstance when provided is IInstanceType providedInstance:
                {
                    foreach (var (name, desc) in expectedInstance.Exports)
                    {
                        if (!providedInstance.Exports.TryGetValue(name, out var providedDesc))
                            return $"export '{name}' is missing";
                        if (desc.GetType() != providedDesc.GetType())
                            return $"export '{name}': expected {DescribeSort(desc)}, provided {DescribeSort(providedDesc)}";
                    }

                    return null;
                }
                default:
                    return null;
            }
        }

        /// <summary>
        ///     Checks whether the exports and the imports of two components are compatible by their names and sorts.
        /// </summary>
        /// <returns>A description of the difference, or null if they are compatible.</returns>
        public static string? FindComponentMismatch(
            IReadOnlyDictionary<string, IExportableDescriptor<ISortedExportable>> expectedImports,
            IReadOnlyDictionary<string, IExportableDescriptor<ISortedExportable>> expectedExports,
            IEnumerable<IImport<ISortedExportable>> providedImports,
            IEnumerable<IExport<ISortedExportable>> providedExports)
        {
            // the provided component can export more and import less than expected
            Dictionary<string, string> exports = new();
            foreach (var export in providedExports) exports[export.Name.Name] = DescribeSort(export);

            foreach (var (name, desc) in expectedExports)
            {
                if (!exports.TryGetValue(name, out var sort)) return $"export '{name}' is missing";
                var expectedSort = DescribeSort(desc);
                if (sort != expectedSort) return $"export '{name}': expected {expectedSort}, provided {sort}";
            }

            foreach (var import in providedImports)
            {
                var name = import.Name.Name;
                var sort = DescribeSort(import.Descriptor);
                if (!expectedImports.TryGetValue(name, out var desc))
                    return $"import '{name}' ({sort}) is not expected to be imported";
                var expectedSort = DescribeSort(desc);
                if (sort != expectedSort) return $"import '{name}': expected {expectedSort}, provided {sort}";
            }

            return null;
        }
    }
}
