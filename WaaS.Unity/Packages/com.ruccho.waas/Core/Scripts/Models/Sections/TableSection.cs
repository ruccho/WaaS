﻿using System;

namespace WaaS.Models
{
    /// <summary>
    ///     Table section in a WebAssembly module.
    /// </summary>
    public class TableSection : Section
    {
        internal TableSection(ref ModuleReader reader)
        {
            var numTableTypes = reader.ReadVectorSize();

            var tableTypes = new TableType[numTableTypes];

            for (var i = 0; i < tableTypes.Length; i++) tableTypes[i] = new TableType(ref reader);

            TableTypes = tableTypes;
        }

        public ReadOnlyMemory<TableType> TableTypes { get; }
    }

    /// <summary>
    ///     Single table entry in a table section.
    /// </summary>
    public readonly struct TableType : IEquatable<TableType>
    {
        public ElementType ElementType { get; }
        public Limits Limits { get; }

        internal TableType(ref ModuleReader reader)
        {
            ElementType = reader.ReadUnaligned<ElementType>();
            Limits = new Limits(ref reader, 32);
        }

        public bool Equals(TableType other)
        {
            return ElementType == other.ElementType && Limits.Equals(other.Limits);
        }

        public override bool Equals(object obj)
        {
            return obj is TableType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)ElementType, Limits);
        }

        public static bool operator ==(TableType left, TableType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TableType left, TableType right)
        {
            return !left.Equals(right);
        }
    }
}