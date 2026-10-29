#nullable enable

using System.Runtime.CompilerServices;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Runtime
{
    /// <summary>
    ///     The table of resource handles owned by a component instance, as defined by the Canonical ABI.
    ///     Handles passed to a component are indices into the table of that component instance,
    ///     so a component cannot access resources that were not passed to it.
    /// </summary>
    internal sealed class HandleTable
    {
        private static readonly ConditionalWeakTable<IInstance, HandleTable> Tables = new();

        // index 0 is reserved and never used as a handle
        private readonly ResourceTable<Handle> handles = new();

        public static HandleTable Of(IInstance instance)
        {
            return Tables.GetValue(instance, static _ => new HandleTable());
        }

        public uint AddOwn(IResourceType type, uint rep)
        {
            return unchecked((uint)handles.Add(new Handle(type, rep, true)));
        }

        public uint AddBorrow(IResourceType type, uint rep)
        {
            return unchecked((uint)handles.Add(new Handle(type, rep, false)));
        }

        public Handle Get(uint index, IResourceType type)
        {
            if (index > int.MaxValue || !handles.TryGet(unchecked((int)index), out var handle))
                throw new TrapException("Invalid resource handle.");
            if (!ReferenceEquals(handle.Type, type)) throw new TrapException("Resource type mismatch.");
            return handle;
        }

        public Handle Remove(uint index, IResourceType type)
        {
            // validate before removing
            Get(index, type);
            return handles.RemoveAt(unchecked((int)index));
        }

        public readonly struct Handle
        {
            public IResourceType Type { get; }
            public uint Rep { get; }
            public bool IsOwn { get; }

            public Handle(IResourceType type, uint rep, bool isOwn)
            {
                Type = type;
                Rep = rep;
                IsOwn = isOwn;
            }
        }
    }
}
