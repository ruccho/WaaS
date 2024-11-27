using System;
using System.Runtime.InteropServices;

namespace WaaS.Models
{
    /// <summary>
    ///     Export section in a WebAssembly module.
    /// </summary>
    public class ExportSection : Section
    {
        internal ExportSection(ref ModuleReader reader)
        {
            var numExports = reader.ReadVectorSize();

            var exports = new Export[numExports];

            for (var i = 0; i < exports.Length; i++) exports[i] = new Export(ref reader);

            Exports = exports;
        }

        public ReadOnlyMemory<Export> Exports { get; }
    }

    /// <summary>
    ///     Single export entry in an export section.
    /// </summary>
    public readonly struct Export : IEquatable<Export>
    {
        public string Name { get; }
        public ExportDescriptor Descriptor { get; }

        internal Export(ref ModuleReader reader)
        {
            Name = reader.ReadUtf8String();
            Descriptor = new ExportDescriptor(ref reader);
        }

        public bool Equals(Export other)
        {
            return Name == other.Name && Descriptor.Equals(other.Descriptor);
        }

        public override bool Equals(object obj)
        {
            return obj is Export other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Descriptor);
        }

        public static bool operator ==(Export left, Export right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Export left, Export right)
        {
            return !left.Equals(right);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct ExportDescriptor : IEquatable<ExportDescriptor>
    {
        [FieldOffset(1)] private readonly uint functionIndex;
        [FieldOffset(1)] private readonly uint tableIndex;
        [FieldOffset(1)] private readonly uint memoryType;
        [FieldOffset(1)] private readonly uint globalType;

        [field: FieldOffset(0)] public ExportKind Kind { get; }

        public uint? FunctionIndex => Kind == ExportKind.Function ? functionIndex : null;
        public uint? TableIndex => Kind == ExportKind.Table ? tableIndex : null;
        public uint? MemoryIndex => Kind == ExportKind.Memory ? memoryType : null;
        public uint? GlobalIndex => Kind == ExportKind.Global ? globalType : null;

        internal ExportDescriptor(ref ModuleReader reader)
        {
            this = default;

            Kind = reader.ReadUnaligned<ExportKind>();

            switch (Kind)
            {
                case ExportKind.Function:
                {
                    functionIndex = reader.ReadUnalignedLeb128U32();
                    break;
                }
                case ExportKind.Table:
                {
                    tableIndex = reader.ReadUnalignedLeb128U32();
                    break;
                }
                case ExportKind.Memory:
                {
                    memoryType = reader.ReadUnalignedLeb128U32();
                    break;
                }
                case ExportKind.Global:
                {
                    globalType = reader.ReadUnalignedLeb128U32();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Equals(ExportDescriptor other)
        {
            if (Kind != other.Kind) return false;

            switch (Kind)
            {
                case ExportKind.Function:
                {
                    return functionIndex == other.functionIndex;
                }
                case ExportKind.Table:
                {
                    return tableIndex.Equals(other.tableIndex);
                }
                case ExportKind.Memory:
                {
                    return memoryType.Equals(other.memoryType);
                }
                case ExportKind.Global:
                {
                    return globalType.Equals(other.globalType);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool Equals(object obj)
        {
            return obj is ExportDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add((int)Kind);

            switch (Kind)
            {
                case ExportKind.Function:
                {
                    hash.Add(functionIndex);
                    break;
                }
                case ExportKind.Table:
                {
                    hash.Add(tableIndex);
                    break;
                }
                case ExportKind.Memory:
                {
                    hash.Add(memoryType);
                    break;
                }
                case ExportKind.Global:
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

    public enum ExportKind : byte
    {
        Function = 0,
        Table = 1,
        Memory = 2,
        Global = 3
    }
}