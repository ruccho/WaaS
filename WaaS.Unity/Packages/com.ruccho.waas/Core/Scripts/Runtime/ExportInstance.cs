using System;
using System.Collections.Generic;
using WaaS.Models;

namespace WaaS.Runtime
{
    public class ExportInstance
    {
        private readonly Dictionary<string, IExternal> items = new();

        public ExportInstance(FunctionInstance functionInstance, ExportSection exportSection,
            TableInstance tables, MemoryInstance memories, GlobalInstance globals)
        {
            if (exportSection == null) return;
            foreach (var export in exportSection.Exports.Span)
            {
                var desc = export.Descriptor;
                IExternal item = desc.Kind switch
                {
                    ExportKind.Function => functionInstance.Functions.Span[checked((int)desc.FunctionIndex.Value)],
                    ExportKind.Table => tables.Tables.Span[checked((int)desc.TableIndex.Value)],
                    ExportKind.Memory => memories.Memories.Span[checked((int)desc.MemoryIndex.Value)],
                    ExportKind.Global => globals.Globals.Span[checked((int)desc.GlobalIndex.Value)],
                    _ => throw new ArgumentOutOfRangeException()
                };
                items.Add(export.Name, item);
            }
        }

        public IReadOnlyDictionary<string, IExternal> Items => items;
    }

    public interface IExternal
    {
    }
}