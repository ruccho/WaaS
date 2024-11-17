using System;
using System.Runtime.InteropServices;
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
    ///     Single element entry in an element section.
    /// </summary>
    public class Element
    {
        internal Element(ref ModuleReader reader)
        {
            var kind = reader.ReadUnalignedLeb128U32();

            // > The initial integer can be interpreted as a bitfield. Bit 0 distinguishes a passive or declarative
            // > segment from an active segment, bit 1 indicates the presence of an explicit table index for an active
            // > segment and otherwise distinguishes passive from declarative segments, bit 2 indicates the use of element
            // > type and element expressions instead of element kind and element indices.
            var isActive = (kind & 0x01) == 0x00;
            var hasTableIndexOrDeclarative = (kind & 0x02) != 0x00;
            var hasElementExpressions = (kind & 0x04) != 0x00;

            if (isActive)
            {
                TableIndex = hasTableIndexOrDeclarative ? reader.ReadUnalignedLeb128U32() : 0;
                Offset = new ConstantExpression(ref reader);
            }
            else
            {
                throw new NotSupportedException("Passive and declarative segments are not supported yet.");
            }

            IConstantExpression[] functionIndices;
            if (hasElementExpressions)
            {
                if (reader.ReadUnaligned<ReferenceType>() != ReferenceType.Function)
                    throw new NotSupportedException("Reference types are not supported yet.");

                var numElements = reader.ReadVectorSize();
                functionIndices = new IConstantExpression[numElements];
                for (var i = 0; i < numElements; i++)
                {
                    functionIndices[i] = new ConstantExpression(ref reader);
                }
            }
            else
            {
                var elementKind = kind == 0 ? ElementKind.Function : reader.ReadUnaligned<ElementKind>();
                if(elementKind != ElementKind.Function) throw new NotSupportedException("Only function elements are supported.");
                var numFunctions = reader.ReadVectorSize();
                functionIndices = new IConstantExpression[numFunctions];
                for (var i = 0; i < functionIndices.Length; i++)
                    functionIndices[i] =
                        new ConstantExpressionConstant(new StackValueItem(reader.ReadUnalignedLeb128U32()));
            }

            FunctionIndices = functionIndices;
        }

        public uint TableIndex { get; }
        public IConstantExpression Offset { get; }
        public ReadOnlyMemory<IConstantExpression> FunctionIndices { get; }
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