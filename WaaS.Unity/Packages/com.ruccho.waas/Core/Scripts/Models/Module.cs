using System;
using System.Buffers;
using System.Collections.Generic;

namespace WaaS.Models
{
    /// <summary>
    ///     Represents a core WebAssembly module.
    /// </summary>
    public class Module
    {
        private HashSet<uint> declaredFunctionReferences;
        private NameSection names;

        internal Module(ref ModuleReader reader, long? size = null)
        {
            long rest = 0;
            if (size.HasValue)
                checked
                {
                    rest = reader.Available - size.Value;
                }

            if (rest < 0) throw new ArgumentException(nameof(size));

            // offsets of the instructions are recorded relative to the start of this module
            var previousOrigin = reader.Origin;
            reader.Origin = reader.Position;

            var preamble = reader.ReadUnaligned<Preamble>();

            if (!preamble.IsValid())
                throw new InvalidModuleException(
                    $"invalid preamble (magic: 0x{preamble.magic:X8}, version: 0x{preamble.version:X8})");

            List<Section> sections = new();

            FunctionSection functionSection = null;
            TypeSection typeSection = null;
            CodeSection codeSection = null;

            var lastSectionOrder = 0;
            while (reader.Available > rest)
            {
                var section = Section.Read(ref reader);
                sections.Add(section);

                // the sections except the custom sections appear at most once in the order
                var order = GetSectionOrder(section);
                if (order != 0)
                {
                    if (order <= lastSectionOrder) throw new InvalidModuleException("unexpected content after last section");
                    lastSectionOrder = order;
                }

                switch (section)
                {
                    case FunctionSection s:
                    {
                        functionSection = s;
                        break;
                    }
                    case TypeSection s:
                    {
                        TypeSection = typeSection = s;
                        break;
                    }
                    case CodeSection s:
                    {
                        codeSection = s;
                        break;
                    }
                    case MemorySection s:
                    {
                        MemorySection = s;
                        break;
                    }
                    case ImportSection s:
                    {
                        ImportSection = s;
                        break;
                    }
                    case GlobalSection s:
                    {
                        GlobalSection = s;
                        break;
                    }
                    case ElementSection s:
                    {
                        ElementSection = s;
                        break;
                    }
                    case TableSection s:
                    {
                        TableSection = s;
                        break;
                    }
                    case DataSection s:
                    {
                        DataSection = s;
                        break;
                    }
                    case StartSection s:
                    {
                        if (StartSection != null) throw new InvalidModuleException("junk after last section");
                        StartSection = s;
                        break;
                    }
                    case ExportSection s:
                    {
                        ExportSection = s;
                        break;
                    }
                    case DataCountSection s:
                    {
                        DataCountSection = s;
                        break;
                    }
                }
            }

            if (DataCountSection != null && DataCountSection.Count != (DataSection?.Data.Length ?? 0))
                throw new InvalidModuleException("data count and data section have inconsistent lengths");

            reader.Origin = previousOrigin;

            Preamble = preamble;
            Sections = sections.ToArray();

            var functions = codeSection != null ? codeSection.Functions.Span : ReadOnlySpan<FunctionBody>.Empty;
            var typeIndices = functionSection != null ? functionSection.TypeIndices.Span : ReadOnlySpan<uint>.Empty;
            var types = typeSection != null ? typeSection.FuncTypes.Span : ReadOnlySpan<FunctionType>.Empty;

            if (typeIndices.Length != functions.Length)
                throw new InvalidModuleException("function and code section have inconsistent lengths");

            var functionInstances = new Function[functions.Length];
            for (var i = 0; i < functions.Length; i++)
            {
                var function = functions[i];
                var typeIndex = typeIndices[i];
                var functionType = types[checked((int)typeIndex)];

                functionInstances[i] = new Function(functionType, function, typeIndex, i);
            }

            InternalFunctions = functionInstances;

            int numImportedFunctions = 0, numImportedTables = 0, numImportedMemories = 0, numImportedGlobals = 0;
            if (ImportSection != null)
                foreach (var import in ImportSection.Imports.Span)
                    switch (import.Descriptor.Kind)
                    {
                        case ImportKind.Type:
                            numImportedFunctions++;
                            break;
                        case ImportKind.Table:
                            numImportedTables++;
                            break;
                        case ImportKind.Memory:
                            numImportedMemories++;
                            break;
                        case ImportKind.Global:
                            numImportedGlobals++;
                            break;
                    }

            ImportedFunctionCount = numImportedFunctions;
            ImportedGlobalCount = numImportedGlobals;
            FunctionCount = numImportedFunctions + functionInstances.Length;
            TableCount = numImportedTables + (TableSection?.TableTypes.Length ?? 0);
            MemoryCount = numImportedMemories + (MemorySection?.MemoryTypes.Length ?? 0);
            GlobalCount = numImportedGlobals + (GlobalSection?.Globals.Length ?? 0);

            ValidateModuleFields();

            foreach (var functionInstance in functionInstances)
                functionInstance.Validate(new ValidationContext(this, functionInstance));
        }

        /// <summary>
        ///     Validates the fields outside the function bodies, and collects the functions declared to be referenced.
        /// </summary>
        private void ValidateModuleFields()
        {
            var context = new ValidationContext(this, null);

            if (TypeSection != null)
                foreach (var type in TypeSection.FuncTypes.Span)
                {
                    foreach (var t in type.ParameterTypes.Span) ValidateValueType(t);
                    foreach (var t in type.ResultTypes.Span) ValidateValueType(t);
                }

            if (ImportSection != null)
                foreach (var import in ImportSection.Imports.Span)
                    if (import.Descriptor.TypeIndex is { } typeIndex &&
                        typeIndex >= (TypeSection?.FuncTypes.Length ?? 0))
                        throw new InvalidModuleException($"unknown type {typeIndex}");

            // functions are declared by the references outside the function bodies
            if (ExportSection != null)
                foreach (var export in ExportSection.Exports.Span)
                    if (export.Descriptor.FunctionIndex is { } functionIndex)
                        DeclareFunctionReference(functionIndex);

            if (ElementSection != null)
                foreach (var element in ElementSection.Elements.Span)
                foreach (var item in element.Items.Span)
                    DeclareFunctionReferences(item);

            if (GlobalSection != null)
                foreach (var global in GlobalSection.Globals.Span)
                    DeclareFunctionReferences(global.Expression);

            if (GlobalSection != null)
                foreach (var global in GlobalSection.Globals.Span)
                {
                    ValidateValueType(global.Type.ValueType);
                    // the initializers can only read the imported globals
                    ((ConstantExpression)global.Expression).Validate(context, global.Type.ValueType,
                        ImportedGlobalCount);
                }

            if (ElementSection != null)
                foreach (var element in ElementSection.Elements.Span)
                {
                    foreach (var item in element.Items.Span)
                        if (item is ConstantExpression expression)
                            expression.Validate(context, element.Type, ImportedGlobalCount);
                        else if (item is ConstantExpressionFunction function)
                            context.ValidateFunctionReference(function.FunctionIndex, false);

                    if (element.Mode == ElementMode.Active)
                    {
                        var tableType = context.GetTableType(element.TableIndex);
                        if ((ValueType)tableType.ElementType != element.Type)
                            throw new InvalidModuleException("type mismatch");
                        ((ConstantExpression)element.Offset).Validate(context, ValueType.I32, ImportedGlobalCount);
                    }
                }

            if (DataSection != null)
                foreach (var data in DataSection.Data.Span)
                    if (!data.IsPassive)
                    {
                        context.GetMemoryType(data.MemoryIndex);
                        ((ConstantExpression)data.Offset).Validate(context, ValueType.I32, ImportedGlobalCount);
                    }
        }

        private static int GetSectionOrder(Section section)
        {
            return section switch
            {
                Models.TypeSection => 1,
                Models.ImportSection => 2,
                Models.FunctionSection => 3,
                Models.TableSection => 4,
                Models.MemorySection => 5,
                Models.GlobalSection => 6,
                Models.ExportSection => 7,
                Models.StartSection => 8,
                Models.ElementSection => 9,
                Models.DataCountSection => 10,
                Models.CodeSection => 11,
                Models.DataSection => 12,
                _ => 0
            };
        }

        private static void ValidateValueType(ValueType type)
        {
            if (!type.IsDefined()) throw new InvalidModuleException("malformed value type");
        }

        private void DeclareFunctionReferences(IConstantExpression expression)
        {
            switch (expression)
            {
                case ConstantExpressionFunction function:
                    DeclareFunctionReference(function.FunctionIndex);
                    break;
                case ConstantExpression constant:
                    foreach (var instr in constant.Instructions.Span)
                        if (instr is RefFunc refFunc)
                            DeclareFunctionReference(refFunc.FunctionIndex);
                    break;
            }
        }

        private void DeclareFunctionReference(uint functionIndex)
        {
            (declaredFunctionReferences ??= new HashSet<uint>()).Add(functionIndex);
        }

        internal bool IsFunctionReferenceDeclared(uint functionIndex)
        {
            return declaredFunctionReferences != null && declaredFunctionReferences.Contains(functionIndex);
        }

        public Preamble Preamble { get; }
        public ReadOnlyMemory<Section> Sections { get; }
        public ReadOnlyMemory<Function> InternalFunctions { get; }
        public MemorySection MemorySection { get; }
        public ImportSection ImportSection { get; }
        public GlobalSection GlobalSection { get; }
        public ElementSection ElementSection { get; }
        public TableSection TableSection { get; }
        public DataSection DataSection { get; }
        public DataCountSection DataCountSection { get; }
        public StartSection StartSection { get; }
        public ExportSection ExportSection { get; }
        public TypeSection TypeSection { get; }

        public string SourceDescription { get; set; }

        /// <summary>
        ///     Number of the imported functions. The index of an internal function in the function index space is
        ///     <c>ImportedFunctionCount + Function.FunctionIndex</c>.
        /// </summary>
        public int ImportedFunctionCount { get; }

        public int ImportedGlobalCount { get; }

        /// <summary>
        ///     The numbers of the items in the index spaces, including the imported ones.
        /// </summary>
        public int FunctionCount { get; }

        public int TableCount { get; }
        public int MemoryCount { get; }
        public int GlobalCount { get; }

        /// <summary>
        ///     Names in the "name" custom section. Empty if the module does not have the section.
        ///     The section is parsed when this property is accessed first.
        /// </summary>
        public NameSection Names => names ??= NameSection.Create(Sections.Span);

        public static Module Create(ReadOnlySpan<byte> buffer)
        {
            var reader = new ModuleReader(buffer);
            return new Module(ref reader);
        }

        public static Module Create(ReadOnlySequence<byte> buffer)
        {
            var reader = new ModuleReader(buffer);
            return new Module(ref reader);
        }
    }
}