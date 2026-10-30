using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Maps the objects of reference values to the handles held by <see cref="StackValueItem" />.
    /// </summary>
    /// <remarks>
    ///     The handles are shared by all instances, so that references can be passed between instances. The objects are
    ///     held weakly, and the same object always has the same handle while it is alive. The handle 0 is the null
    ///     reference.
    /// </remarks>
    internal static class References
    {
        private static readonly object Lock = new();
        private static readonly ConditionalWeakTable<object, Handle> Handles = new();
        private static readonly Dictionary<uint, WeakReference<object>> Objects = new();
        private static uint nextHandle = 1;
        private static int purgeThreshold = 1024;

        public static uint GetHandle(object value)
        {
            if (value == null) return 0;

            lock (Lock)
            {
                if (Handles.TryGetValue(value, out var handle)) return handle.Value;

                if (Objects.Count >= purgeThreshold) Purge();

                var newHandle = nextHandle;
                nextHandle = nextHandle == uint.MaxValue ? 1 : nextHandle + 1;
                Handles.Add(value, new Handle(newHandle));
                Objects[newHandle] = new WeakReference<object>(value);
                return newHandle;
            }
        }

        public static object Get(uint handle)
        {
            if (handle == 0) return null;

            lock (Lock)
            {
                if (Objects.TryGetValue(handle, out var reference) && reference.TryGetTarget(out var value))
                    return value;
            }

            throw new TrapException("the object of the reference was collected");
        }

        private static void Purge()
        {
            List<uint> dead = null;
            foreach (var (handle, reference) in Objects)
                if (!reference.TryGetTarget(out _))
                    (dead ??= new List<uint>()).Add(handle);

            if (dead != null)
                foreach (var handle in dead)
                    Objects.Remove(handle);

            purgeThreshold = Math.Max(1024, Objects.Count * 2);
        }

        private sealed class Handle
        {
            public Handle(uint value)
            {
                Value = value;
            }

            public uint Value { get; }
        }
    }
}
