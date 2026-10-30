using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WaaS.Models
{
    /// <summary>
    ///     The line number table in the DWARF debugging information (<c>.debug_line</c>) of a module, which maps the
    ///     instructions to the positions in the source files. DWARF 4 and 5 are supported.
    /// </summary>
    /// <remarks>
    ///     The addresses in DWARF of WebAssembly are the offsets from the start of the contents of the code section, and
    ///     this table converts them to the offsets from the start of the module (<see cref="Instruction.Offset" />).
    /// </remarks>
    public sealed class DwarfLineTable
    {
        private readonly DwarfLineRow[] rows;

        private DwarfLineTable(DwarfLineRow[] rows, string[] files)
        {
            this.rows = rows;
            Files = files;
        }

        /// <summary>
        ///     The rows sorted by the offset, excluding the ends of the sequences.
        /// </summary>
        public IReadOnlyList<DwarfLineRow> Rows => rows;

        /// <summary>
        ///     The paths of the source files as recorded, which may be relative to the directory of the compilation.
        /// </summary>
        public IReadOnlyList<string> Files { get; }

        /// <summary>
        ///     Reads the table of the module, or returns null if the module has no line number table.
        /// </summary>
        public static DwarfLineTable Read(Module module)
        {
            ReadOnlyMemory<byte>? debugLine = null;
            ReadOnlyMemory<byte> debugLineStr = default, debugStr = default;
            CodeSection code = null;
            foreach (var section in module.Sections.Span)
                switch (section)
                {
                    case CustomSection { Name: ".debug_line" } s:
                        debugLine = s.Data;
                        break;
                    case CustomSection { Name: ".debug_line_str" } s:
                        debugLineStr = s.Data;
                        break;
                    case CustomSection { Name: ".debug_str" } s:
                        debugStr = s.Data;
                        break;
                    case CodeSection s:
                        code = s;
                        break;
                }

            if (debugLine == null || code == null) return null;
            return Parse(debugLine.Value.Span, debugLineStr.Span, debugStr.Span, code.ContentOffset);
        }

        /// <summary>
        ///     Parses <c>.debug_line</c>. <paramref name="codeOffset" /> is the offset of the contents of the code section
        ///     in the module.
        /// </summary>
        public static DwarfLineTable Parse(ReadOnlySpan<byte> debugLine, ReadOnlySpan<byte> debugLineStr,
            ReadOnlySpan<byte> debugStr, uint codeOffset)
        {
            var rows = new List<DwarfLineRow>();
            var files = new List<string>();
            var fileIndices = new Dictionary<string, int>();

            var position = 0;
            while (position < debugLine.Length)
            {
                var unit = new Reader(debugLine, position);
                var unitLength = (ulong)unit.U32();
                var is64 = unitLength == 0xFFFF_FFFF;
                if (is64) unitLength = unit.U64();
                var unitEnd = checked(unit.Position + (int)unitLength);
                var version = unit.U16();
                if (version is < 2 or > 5) throw new InvalidDataException($"unsupported version of .debug_line: {version}");
                if (version >= 5)
                {
                    unit.U8(); // address_size
                    unit.U8(); // segment_selector_size
                }

                var headerLength = is64 ? unit.U64() : unit.U32();
                var programStart = checked(unit.Position + (int)headerLength);
                var minimumInstructionLength = unit.U8();
                if (version >= 4) unit.U8(); // maximum_operations_per_instruction
                var defaultIsStatement = unit.U8() != 0;
                var lineBase = (sbyte)unit.U8();
                var lineRange = unit.U8();
                var opcodeBase = unit.U8();
                var standardOpcodeLengths = new byte[opcodeBase];
                for (var i = 1; i < opcodeBase; i++) standardOpcodeLengths[i] = unit.U8();

                // the files of this unit, mapped to the indices in the table
                var unitFiles = new List<int>();
                if (version >= 5)
                {
                    var directories = ReadEntries(ref unit, debugLineStr, debugStr, is64, null);
                    foreach (var file in ReadEntries(ref unit, debugLineStr, debugStr, is64, directories))
                        unitFiles.Add(AddFile(file));
                }
                else
                {
                    var directories = new List<string> { "" };
                    while (true)
                    {
                        var directory = unit.CString();
                        if (directory.Length == 0) break;
                        directories.Add(directory);
                    }

                    // the file index starts at 1 in DWARF 4
                    unitFiles.Add(-1);
                    while (true)
                    {
                        var name = unit.CString();
                        if (name.Length == 0) break;
                        var directoryIndex = (int)unit.Uleb();
                        unit.Uleb(); // modification time
                        unit.Uleb(); // length
                        unitFiles.Add(AddFile(Combine(directories, directoryIndex, name)));
                    }
                }

                RunProgram(new Reader(debugLine, programStart), unitEnd);
                position = unitEnd;

                void RunProgram(Reader program, int end)
                {
                    ulong address = 0;
                    ulong file = 1, line = 1, column = 0;
                    var isStatement = defaultIsStatement;
                    // the linker writes a tombstone (-1) to the addresses of the functions removed from the module
                    var isRemoved = false;

                    void Emit(bool isEnd)
                    {
                        if (isRemoved) return;
                        var index = (int)file < unitFiles.Count ? unitFiles[(int)file] : -1;
                        rows.Add(new DwarfLineRow(checked((uint)(address + codeOffset)), index, (int)line, (int)column,
                            isStatement, isEnd));
                    }

                    while (program.Position < end)
                    {
                        var opcode = program.U8();
                        if (opcode >= opcodeBase)
                        {
                            // special opcode
                            var adjusted = opcode - opcodeBase;
                            address += (ulong)(adjusted / lineRange * minimumInstructionLength);
                            line = (ulong)((long)line + lineBase + adjusted % lineRange);
                            Emit(false);
                            continue;
                        }

                        switch (opcode)
                        {
                            case 0:
                            {
                                // extended opcode
                                var length = (int)program.Uleb();
                                var next = program.Position + length;
                                switch (program.U8())
                                {
                                    case 1: // DW_LNE_end_sequence
                                        Emit(true);
                                        address = 0;
                                        file = 1;
                                        line = 1;
                                        column = 0;
                                        isStatement = defaultIsStatement;
                                        isRemoved = false;
                                        break;
                                    case 2: // DW_LNE_set_address
                                        address = length - 1 == 8 ? program.U64() : program.U32();
                                        isRemoved = address >= (length - 1 == 8 ? 0xFFFF_FFFF_FFFF_FFF0 : 0xFFFF_FFF0);
                                        break;
                                }

                                program.Position = next;
                                break;
                            }
                            case 1: // DW_LNS_copy
                                Emit(false);
                                break;
                            case 2: // DW_LNS_advance_pc
                                address += program.Uleb() * minimumInstructionLength;
                                break;
                            case 3: // DW_LNS_advance_line
                                line = (ulong)((long)line + program.Sleb());
                                break;
                            case 4: // DW_LNS_set_file
                                file = program.Uleb();
                                break;
                            case 5: // DW_LNS_set_column
                                column = program.Uleb();
                                break;
                            case 6: // DW_LNS_negate_stmt
                                isStatement = !isStatement;
                                break;
                            case 8: // DW_LNS_const_add_pc
                                address += (ulong)((255 - opcodeBase) / lineRange * minimumInstructionLength);
                                break;
                            case 9: // DW_LNS_fixed_advance_pc
                                address += program.U16();
                                break;
                            default:
                                // the other standard opcodes (basic block, prologue end, epilogue begin, isa) are skipped
                                for (var i = 0; i < standardOpcodeLengths[opcode]; i++) program.Uleb();
                                break;
                        }
                    }
                }
            }

            // a row covers the offsets until the next row in the same sequence
            var sorted = new List<DwarfLineRow>(rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.IsEndSequence) continue;
                var end = i + 1 < rows.Count ? rows[i + 1].Offset : row.Offset;
                sorted.Add(row.WithEnd(Math.Max(end, row.Offset)));
            }

            sorted.Sort((a, b) => a.Offset.CompareTo(b.Offset));
            return new DwarfLineTable(sorted.ToArray(), files.ToArray());

            int AddFile(string path)
            {
                if (fileIndices.TryGetValue(path, out var index)) return index;
                index = files.Count;
                files.Add(path);
                fileIndices[path] = index;
                return index;
            }
        }

        /// <summary>
        ///     Finds the row covering the instruction at the offset in the module.
        /// </summary>
        public bool TryFind(uint offset, out DwarfLineRow row)
        {
            int low = 0, high = rows.Length - 1, found = -1;
            while (low <= high)
            {
                var middle = (low + high) / 2;
                if (rows[middle].Offset <= offset)
                {
                    found = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            // rows of the same offset are sorted stably, and the last one is used
            if (found >= 0 && offset < Math.Max(rows[found].EndOffset, rows[found].Offset + 1))
            {
                row = rows[found];
                return true;
            }

            row = default;
            return false;
        }

        /// <summary>
        ///     Gets the path of the file of a row.
        /// </summary>
        public string GetFile(in DwarfLineRow row)
        {
            return row.FileIndex >= 0 ? Files[row.FileIndex] : null;
        }

        private static string Combine(List<string> directories, int index, string name)
        {
            if (IsAbsolute(name) || index <= 0 || index >= directories.Count) return name;
            var directory = directories[index];
            return directory.EndsWith("/") || directory.EndsWith("\\") ? directory + name : directory + "/" + name;
        }

        private static bool IsAbsolute(string path)
        {
            return path.StartsWith("/") || path.StartsWith("\\") || (path.Length >= 2 && path[1] == ':');
        }

        // reads the directories or the files of DWARF 5
        private static List<string> ReadEntries(ref Reader reader, ReadOnlySpan<byte> debugLineStr,
            ReadOnlySpan<byte> debugStr, bool is64, List<string> directories)
        {
            var formatCount = reader.U8();
            var formats = new (ulong contentType, ulong form)[formatCount];
            for (var i = 0; i < formatCount; i++) formats[i] = (reader.Uleb(), reader.Uleb());

            var count = (int)reader.Uleb();
            var result = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                string path = null;
                var directoryIndex = 0;
                foreach (var (contentType, form) in formats)
                {
                    var value = ReadForm(ref reader, form, debugLineStr, debugStr, is64);
                    switch (contentType)
                    {
                        case 1: // DW_LNCT_path
                            path = value as string;
                            break;
                        case 2: // DW_LNCT_directory_index
                            directoryIndex = Convert.ToInt32(value);
                            break;
                    }
                }

                path ??= "";
                if (directories != null && !IsAbsolute(path) && directoryIndex < directories.Count &&
                    directories[directoryIndex].Length > 0)
                    path = directories[directoryIndex].TrimEnd('/', '\\') + "/" + path;
                result.Add(path);
            }

            return result;
        }

        private static object ReadForm(ref Reader reader, ulong form, ReadOnlySpan<byte> debugLineStr,
            ReadOnlySpan<byte> debugStr, bool is64)
        {
            switch (form)
            {
                case 0x08: // DW_FORM_string
                    return reader.CString();
                case 0x1F: // DW_FORM_line_strp
                    return new Reader(debugLineStr, (int)(is64 ? reader.U64() : reader.U32())).CString();
                case 0x0E: // DW_FORM_strp
                    return new Reader(debugStr, (int)(is64 ? reader.U64() : reader.U32())).CString();
                case 0x0B: // DW_FORM_data1
                    return (ulong)reader.U8();
                case 0x05: // DW_FORM_data2
                    return (ulong)reader.U16();
                case 0x06: // DW_FORM_data4
                    return (ulong)reader.U32();
                case 0x07: // DW_FORM_data8
                    return reader.U64();
                case 0x0F: // DW_FORM_udata
                    return reader.Uleb();
                case 0x1E: // DW_FORM_data16
                    reader.Position += 16;
                    return null;
                case 0x09: // DW_FORM_block
                    reader.Position += (int)reader.Uleb();
                    return null;
                default:
                    throw new InvalidDataException($"unsupported form in .debug_line: 0x{form:X}");
            }
        }

        private ref struct Reader
        {
            private readonly ReadOnlySpan<byte> data;
            public int Position;

            public Reader(ReadOnlySpan<byte> data, int position)
            {
                this.data = data;
                Position = position;
            }

            public byte U8()
            {
                return data[Position++];
            }

            public ushort U16()
            {
                var value = (ushort)(data[Position] | (data[Position + 1] << 8));
                Position += 2;
                return value;
            }

            public uint U32()
            {
                var value = (uint)(data[Position] | (data[Position + 1] << 8) | (data[Position + 2] << 16) |
                                   (data[Position + 3] << 24));
                Position += 4;
                return value;
            }

            public ulong U64()
            {
                var low = U32();
                return low | ((ulong)U32() << 32);
            }

            public ulong Uleb()
            {
                ulong result = 0;
                var shift = 0;
                while (true)
                {
                    var b = data[Position++];
                    if (shift < 64) result |= (ulong)(b & 0x7F) << shift;
                    shift += 7;
                    if ((b & 0x80) == 0) return result;
                }
            }

            public long Sleb()
            {
                long result = 0;
                var shift = 0;
                byte b;
                do
                {
                    b = data[Position++];
                    if (shift < 64) result |= (long)(b & 0x7F) << shift;
                    shift += 7;
                } while ((b & 0x80) != 0);

                if (shift < 64 && (b & 0x40) != 0) result |= -1L << shift;
                return result;
            }

            public string CString()
            {
                var start = Position;
                while (data[Position] != 0) Position++;
                var value = Encoding.UTF8.GetString(data.Slice(start, Position - start));
                Position++;
                return value;
            }
        }
    }

    /// <summary>
    ///     A row of <see cref="DwarfLineTable" />: the instructions from <see cref="Offset" /> to
    ///     <see cref="EndOffset" /> (offsets in the module) are compiled from the position in the source file.
    /// </summary>
    public readonly struct DwarfLineRow
    {
        internal DwarfLineRow(uint offset, int fileIndex, int line, int column, bool isStatement, bool isEndSequence,
            uint endOffset = 0)
        {
            Offset = offset;
            FileIndex = fileIndex;
            Line = line;
            Column = column;
            IsStatement = isStatement;
            IsEndSequence = isEndSequence;
            EndOffset = endOffset;
        }

        public uint Offset { get; }
        public uint EndOffset { get; }

        /// <summary>
        ///     The index in <see cref="DwarfLineTable.Files" />, or -1 if the file is unknown.
        /// </summary>
        public int FileIndex { get; }

        /// <summary>
        ///     The line from 1, or 0 if the instructions do not belong to a line.
        /// </summary>
        public int Line { get; }

        public int Column { get; }

        /// <summary>
        ///     Whether the instructions are a recommended place for a breakpoint.
        /// </summary>
        public bool IsStatement { get; }

        internal bool IsEndSequence { get; }

        internal DwarfLineRow WithEnd(uint endOffset)
        {
            return new DwarfLineRow(Offset, FileIndex, Line, Column, IsStatement, IsEndSequence, endOffset);
        }
    }
}
