using System;
using System.Runtime.InteropServices;

namespace WaaS.Models
{
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

    public readonly struct Import : IEquatable<Import>
    {
        public string ModuleName { get; }
        public string Name { get; }
        public ImportDescriptor Description { get; }

        internal Import(ref ModuleReader reader)
        {
            ModuleName = reader.ReadUtf8String();
            Name = reader.ReadUtf8String();
            Description = new ImportDescriptor(ref reader);
        }

        public bool Equals(Import other)
        {
            return ModuleName == other.ModuleName && Name == other.Name && Description.Equals(other.Description);
        }

        public override bool Equals(object obj)
        {
            return obj is Import other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ModuleName, Name, Description);
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

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ImportDescriptor : IEquatable<ImportDescriptor>
    {
        [FieldOffset(0)] private readonly ImportKind kind;

        [FieldOffset(1)] private readonly uint typeIndex;
        [FieldOffset(1)] private readonly TableType tableType;
        [FieldOffset(1)] private readonly MemoryType memoryType;
        [FieldOffset(1)] private readonly GlobalType globalType;

        public uint? TypeIndex => kind == ImportKind.Type ? typeIndex : null;
        public TableType? TableType => kind == ImportKind.Table ? tableType : null;
        public MemoryType? MemoryType => kind == ImportKind.Memory ? memoryType : null;
        public GlobalType? GlobalType => kind == ImportKind.Global ? globalType : null;

        internal ImportDescriptor(ref ModuleReader reader)
        {
            this = default;

            kind = reader.ReadUnaligned<ImportKind>();

            switch (kind)
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
            if (kind != other.kind) return false;

            switch (kind)
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
            hash.Add((int)kind);

            switch (kind)
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

    public enum ImportKind : byte
    {
        Type = 0,
        Table = 1,
        Memory = 2,
        Global = 3
    }

    public enum ElementType : byte
    {
        FuncRef = 0x70
    }
}