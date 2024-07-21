using System;
using System.Buffers;
using System.Collections.Generic;

namespace WaaS.Models
{
    public class Module
    {
        internal Module(ref ModuleReader reader, long? size = null)
        {
            long rest = 0;
            if (size.HasValue)
                checked
                {
                    rest = reader.Available - size.Value;
                }

            if (rest < 0) throw new ArgumentException(nameof(size));


            var preamble = reader.ReadUnaligned<Preamble>();

            if (!preamble.IsValid())
                throw new InvalidModuleException(
                    $"invalid preamble (magic: 0x{preamble.magic:X8}, version: 0x{preamble.version:X8})");

            List<Section> sections = new();

            FunctionSection functionSection = null;
            TypeSection typeSection = null;
            CodeSection codeSection = null;

            while (reader.Available > rest)
            {
                var section = Section.Read(ref reader);
                sections.Add(section);

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
                }
            }

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

                functionInstances[i] = new Function(functionType, function, typeIndex);
            }

            InternalFunctions = functionInstances;

            foreach (var functionInstance in functionInstances)
                functionInstance.Validate(new ValidationContext(this, functionInstance));
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
        public StartSection StartSection { get; }
        public ExportSection ExportSection { get; }
        public TypeSection TypeSection { get; }

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