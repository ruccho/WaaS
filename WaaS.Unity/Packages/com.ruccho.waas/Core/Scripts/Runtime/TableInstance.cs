using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     A collection of tables in a WebAssembly instance.
    /// </summary>
    public class TableInstance
    {
        private readonly object[][] elementSegments;

        public TableInstance(GlobalInstance globals, FunctionInstance functions,
            ImportSection importSection, TableSection tableSection, ElementSection elementSection,
            IImports importObject)
        {
            var numTables = tableSection?.TableTypes.Length ?? 0;
            var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;
            foreach (var import in imports)
                if (import.Descriptor.TableType != null)
                    numTables++;

            var tables = new Table[numTables];

            var cursor = 0;
            foreach (var import in imports)
            {
                var tableType = import.Descriptor.TableType;
                if (tableType.HasValue)
                {
                    var t = tableType.Value;

                    if (!importObject.TryGetImportable(import.ModuleName, import.Name, out Table externalTable))
                        throw new InvalidOperationException();

                    if ((ValueType)t.ElementType != externalTable.ElementType ||
                        !externalTable.CurrentLimits.IsImportable(t.Limits))
                        throw new InvalidModuleException("incompatible import type");

                    tables[cursor++] = externalTable;
                }
            }

            var tableTypes = tableSection != null ? tableSection.TableTypes.Span : Span<TableType>.Empty;
            foreach (var tableType in tableTypes)
                tables[cursor++] = tableType.ElementType switch
                {
                    ElementType.FuncRef => new Table<IInvocableFunction>(tableType.Limits),
                    ElementType.ExternRef => new Table<object>(tableType.Limits),
                    _ => throw new ArgumentOutOfRangeException()
                };

            // the references in the segments are evaluated at instantiation
            var elements = elementSection != null ? elementSection.Elements.Span : Span<Element>.Empty;
            elementSegments = new object[elements.Length][];
            for (var i = 0; i < elements.Length; i++)
            {
                var items = elements[i].Items.Span;
                var segment = new object[items.Length];
                for (var j = 0; j < items.Length; j++)
                    segment[j] = items[j].Evaluate(globals.Globals.Span, functions.Functions.Span).ExpectReference();
                elementSegments[i] = segment;
            }

            Tables = tables;
            TableArray = tables;
        }

        public ReadOnlyMemory<Table> Tables { get; }

        internal Table[] TableArray { get; }

        /// <summary>
        ///     Initializes the tables with the active segments, and drops the active and declarative segments.
        /// </summary>
        /// <exception cref="TrapException">A segment is out of the bounds of the table.</exception>
        public void Initialize(FunctionInstance functionInstance, GlobalInstance globals, ElementSection elementSection)
        {
            var elements = elementSection != null ? elementSection.Elements.Span : Span<Element>.Empty;

            for (var i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                if (element.Mode == ElementMode.Active)
                {
                    var offset = element.Offset.Evaluate(globals).ExpectValueI32();
                    Init(element.TableIndex, (uint)i, offset, 0, (uint)elementSegments[i].Length);
                }

                if (element.Mode != ElementMode.Passive) DropElementSegment((uint)i);
            }
        }

        internal void Init(uint tableIndex, uint elementIndex, uint destOffset, uint srcOffset, uint count)
        {
            var table = TableArray[tableIndex];
            var segment = elementSegments[elementIndex];
            if ((ulong)srcOffset + count > (ulong)segment.Length) throw new TrapException("out of bounds table access");
            table.ThrowIfOutOfBounds(destOffset, count);
            for (var i = 0u; i < count; i++) table.SetObject(destOffset + i, segment[srcOffset + i]);
        }

        internal void DropElementSegment(uint elementIndex)
        {
            elementSegments[elementIndex] = Array.Empty<object>();
        }
    }
}
