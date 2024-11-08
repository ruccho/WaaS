using System;

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
            TableIndex = reader.ReadUnalignedLeb128U32();
            Offset = new ConstantExpression(ref reader);
            var numFunctions = reader.ReadVectorSize();
            var functionIndices = new uint[numFunctions];
            for (var i = 0; i < functionIndices.Length; i++) functionIndices[i] = reader.ReadUnalignedLeb128U32();

            FunctionIndices = functionIndices;
        }

        public uint TableIndex { get; }
        public ConstantExpression Offset { get; }
        public ReadOnlyMemory<uint> FunctionIndices { get; }
    }
}