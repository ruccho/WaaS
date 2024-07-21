using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    public class TableInstance
    {
        public TableInstance(GlobalInstance globals,
            ImportSection importSection, TableSection tableSection, ElementSection elementSection,
            IImports importObject)
        {
            var numTables = tableSection?.TableTypes.Length ?? 0;
            var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;
            foreach (var import in imports)
                if (import.Description.TableType != null)
                    numTables++;

            if (numTables > 1) throw new InvalidModuleException("multiple tables");

            var tables = new Table[numTables];

            var cursor = 0;
            foreach (var import in imports)
            {
                var tableType = import.Description.TableType;
                if (tableType.HasValue)
                {
                    var t = tableType.Value;

                    if (!importObject.TryGetImportable(import.ModuleName, import.Name,
                            out Table<IInvocableFunction> externalTable))
                        throw new InvalidOperationException();

                    if (!externalTable.Limits.IsImportable(t.Limits))
                        throw new
                            InvalidModuleException("incompatible import type");

                    tables[cursor++] = t.ElementType switch
                    {
                        ElementType.FuncRef => externalTable,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }
            }

            var tableTypes = tableSection != null ? tableSection.TableTypes.Span : Span<TableType>.Empty;
            foreach (var tableType in tableTypes)
                tables[cursor++] = tableType.ElementType switch
                {
                    ElementType.FuncRef => new Table<IInvocableFunction>(tableType.Limits),
                    _ => throw new ArgumentOutOfRangeException()
                };

            var elements = elementSection != null ? elementSection.Elements.Span : Span<Element>.Empty;

            // check all elements bounds before assignment
            foreach (var elementInitializer in elements)
            {
                var offset = elementInitializer.Offset.Evaluate(globals).ExpectValueI32();
                var tableIndex = elementInitializer.TableIndex;
                var table = tables[tableIndex];
                if (table is not Table<IInvocableFunction> funcTable) throw new InvalidModuleException();

                if (funcTable.Limits.Min < offset + elementInitializer.FunctionIndices.Length)
                    throw new InvalidModuleException();
            }

            Tables = tables;
        }

        public ReadOnlyMemory<Table> Tables { get; }

        public void Initialize(FunctionInstance functionInstance, GlobalInstance globals, ElementSection elementSection)
        {
            var tables = Tables.Span;
            var elements = elementSection != null ? elementSection.Elements.Span : Span<Element>.Empty;

            foreach (var elementInitializer in elements)
            {
                var offset = elementInitializer.Offset.Evaluate(globals).ExpectValueI32();
                var tableIndex = elementInitializer.TableIndex;
                var table = tables[checked((int)tableIndex)];
                if (table is not Table<IInvocableFunction> funcTable) throw new InvalidModuleException();

                var initialization = elementInitializer.FunctionIndices.Span;

                for (var i = 0; i < initialization.Length; i++)
                {
                    var element = initialization[i];
                    var function = functionInstance.Functions.Span[checked((int)element)];
                    funcTable.Elements[checked((int)(offset + i))] = function;
                }
            }
        }
    }
}