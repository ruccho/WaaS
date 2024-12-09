using System;
using System.Runtime.InteropServices;

namespace WaaS.Models
{
    /// <summary>
    ///     Import section in a WebAssembly module.
    /// </summary>
    public class ImportSection : Section
    {
        internal ImportSection(ref ModuleReader reader)
        {
            var numImports = reader.ReadVectorSize();
            var imports = new Import[numImports];
            for (var i = 0; i < imports.Length; i++) imports[i] = new Import(ref reader);

            Imports = imports;
        }

        public ReadOnlyMemory<Import> Imports { get; }
    }

    /// <summary>
    ///     Single import entry in an import section.
    /// </summary>
    public readonly struct Import : IEquatable<Import>
    {
        public string ModuleName { get; }
        public string Name { get; }
        public ImportDescriptor Descriptor { get; }

        internal Import(ref ModuleReader reader)
        {
            ModuleName = reader.ReadUtf8String();
            Name = reader.ReadUtf8String();
            Descriptor = new ImportDescriptor(ref reader);
        }

        public bool Equals(Import other)
        {
            return ModuleName == other.ModuleName && Name == other.Name && Descriptor.Equals(other.Descriptor);
        }

        public override bool Equals(object obj)
        {
            return obj is Import other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModuleName, Name, Descriptor);
        }

        public static bool operator ==(Import left, Import right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Import left, Import right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    ///     Descriptor of an import entry.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ImportDescriptor : IEquatable<ImportDescriptor>
    {
        [FieldOffset(1)] private readonly uint typeIndex;
        [FieldOffset(1)] private readonly TableType tableType;
        [FieldOffset(1)] private readonly MemoryType memoryType;
        [FieldOffset(1)] private readonly GlobalType globalType;

        [field: FieldOffset(0)] public ImportKind Kind { get; }

        public uint? TypeIndex => Kind == ImportKind.Type ? typeIndex : null;
        public TableType? TableType => Kind == ImportKind.Table ? tableType : null;
        public MemoryType? MemoryType => Kind == ImportKind.Memory ? memoryType : null;
        public GlobalType? GlobalType => Kind == ImportKind.Global ? globalType : null;

        internal ImportDescriptor(ref ModuleReader reader)
        {
            this = default;

            Kind = reader.ReadUnaligned<ImportKind>();

            switch (Kind)
            {
                case ImportKind.Type:
                {
                    typeIndex = reader.ReadUnalignedLeb128U32();
                    break;
                }
                case ImportKind.Table:
                {
                    tableType = new TableType(ref reader);
                    break;
                }
                case ImportKind.Memory:
                {
                    memoryType = new MemoryType(ref reader);
                    break;
                }
                case ImportKind.Global:
                {
                    globalType = new GlobalType(ref reader);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Equals(ImportDescriptor other)
        {
            if (Kind != other.Kind) return false;

            switch (Kind)
            {
                case ImportKind.Type:
                {
                    return typeIndex == other.typeIndex;
                }
                case ImportKind.Table:
                {
                    return tableType.Equals(other.tableType);
                }
                case ImportKind.Memory:
                {
                    return memoryType.Equals(other.memoryType);
                }
                case ImportKind.Global:
                {
                    return globalType.Equals(other.globalType);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool Equals(object obj)
        {
            return obj is ImportDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add((int)Kind);

            switch (Kind)
            {
                case ImportKind.Type:
                {
                    hash.Add(typeIndex);
                    break;
                }
                case ImportKind.Table:
                {
                    hash.Add(tableType);
                    break;
                }
                case ImportKind.Memory:
                {
                    hash.Add(memoryType);
                    break;
                }
                case ImportKind.Global:
                {
                    hash.Add(globalType);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }


            return hash.ToHashCode();
        }
    }

    /// <summary>
    ///     Kind of an import entry.
    /// </summary>
    public enum ImportKind : byte
    {
        Type = 0,
        Table = 1,
        Memory = 2,
        Global = 3
    }

    /// <summary>
    ///     Type of table element.
    /// </summary>
    public enum ElementType : byte
    {
        FuncRef = 0x70
    }
}