using System;
using WaaS.Runtime;

namespace WaaS.Models
{
    /// <summary>
    ///     Element section in a WebAssembly module.
    /// </summary>
    public class ElementSection : Section
    {
        internal ElementSection(ref ModuleReader reader)
        {
            var numElements = reader.ReadVectorSize();
            var elements = new Element[numElements];
            for (var i = 0; i < elements.Length; i++) elements[i] = new Element(ref reader);

            Elements = elements;
        }

        public ReadOnlyMemory<Element> Elements { get; }
    }

    /// <summary>
    ///     Single element segment in an element section.
    /// </summary>
    public class Element
    {
        internal Element(ref ModuleReader reader)
        {
            var kind = reader.ReadUnalignedLeb128U32();
            if (kind > 7) throw new InvalidModuleException("malformed elements segment kind");

            // > The initial integer can be interpreted as a bitfield. Bit 0 distinguishes a passive or declarative
            // > segment from an active segment, bit 1 indicates the presence of an explicit table index for an active
            // > segment and otherwise distinguishes passive from declarative segments, bit 2 indicates the use of element
            // > type and element expressions instead of element kind and element indices.
            var isActive = (kind & 0x01) == 0x00;
            var hasTableIndexOrDeclarative = (kind & 0x02) != 0x00;
            var hasElementExpressions = (kind & 0x04) != 0x00;

            if (isActive)
            {
                Mode = ElementMode.Active;
                TableIndex = hasTableIndexOrDeclarative ? reader.ReadUnalignedLeb128U32() : 0;
                Offset = new ConstantExpression(ref reader);
            }
            else
            {
                Mode = hasTableIndexOrDeclarative ? ElementMode.Declarative : ElementMode.Passive;
            }

            // the element type is omitted for the kinds 0 and 4
            var hasType = kind is not (0 or 4);

            IConstantExpression[] items;
            if (hasElementExpressions)
            {
                Type = ValueType.FuncRef;
                if (hasType)
                {
                    Type = reader.ReadUnaligned<ValueType>();
                    if (!Type.IsReference()) throw new InvalidModuleException("malformed reference type");
                }

                var numElements = reader.ReadVectorSize();
                items = new IConstantExpression[numElements];
                for (var i = 0; i < numElements; i++) items[i] = new ConstantExpression(ref reader);
            }
            else
            {
                Type = ValueType.FuncRef;
                if (hasType && reader.ReadUnaligned<ElementKind>() != ElementKind.Function)
                    throw new InvalidModuleException("malformed element kind");
                var numFunctions = reader.ReadVectorSize();
                items = new IConstantExpression[numFunctions];
                for (var i = 0; i < items.Length; i++)
                    items[i] = new ConstantExpressionFunction(reader.ReadUnalignedLeb128U32());
            }

            Items = items;
        }

        public ElementMode Mode { get; }

        /// <summary>
        ///     The index of the table initialized by an active segment.
        /// </summary>
        public uint TableIndex { get; }

        /// <summary>
        ///     The offset in the table initialized by an active segment. <c>null</c> for the other segments.
        /// </summary>
        public IConstantExpression Offset { get; }

        /// <summary>
        ///     <see cref="ValueType.FuncRef" /> or <see cref="ValueType.ExternRef" />.
        /// </summary>
        public ValueType Type { get; }

        /// <summary>
        ///     The expressions of the references in the segment.
        /// </summary>
        public ReadOnlyMemory<IConstantExpression> Items { get; }
    }

    public enum ElementMode : byte
    {
        Active,
        Passive,
        Declarative
    }

    public enum ElementKind : byte
    {
        Function = 0x00
    }

    public enum ReferenceType : byte
    {
        Function = 0x70,
        External = 0x6F
    }
}
