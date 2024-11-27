using System;

namespace WaaS.Models
{
    /// <summary>
    ///     Base class for all sections in a WebAssembly module.
    /// </summary>
    public abstract class Section
    {
        internal static Section Read(ref ModuleReader reader)
        {
            var sectionId = reader.ReadUnaligned<SectionId>();
            var sectionSize = reader.ReadUnalignedLeb128U32();

            var next = reader.Position + sectionSize;

            Section section = sectionId switch
            {
                SectionId.Custom => new CustomSection(ref reader, sectionSize),
                SectionId.Type => new TypeSection(ref reader),
                SectionId.Import => new ImportSection(ref reader),
                SectionId.Function => new FunctionSection(ref reader),
                SectionId.Table => new TableSection(ref reader),
                SectionId.Memory => new MemorySection(ref reader),
                SectionId.Global => new GlobalSection(ref reader),
                SectionId.Export => new ExportSection(ref reader),
                SectionId.Start => new StartSection(ref reader),
                SectionId.Element => new ElementSection(ref reader),
                SectionId.Code => new CodeSection(ref reader),
                SectionId.Data => new DataSection(ref reader),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (reader.Position != next)
                throw new InvalidModuleException(
                    $"section {sectionId} size mismatch. expected position: 0x{next:X}, actual: 0x{reader.Position:X}");

            return section;
        }
    }
}