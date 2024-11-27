#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Runtime
{
    internal readonly struct StackValueItems
    {
        private readonly bool isSingle;
        private readonly StackValueItem singleItem;
        private readonly ReadOnlyMemory<StackValueItem> items;
        private readonly ValuePusher itemsLifetime;

        public ReadOnlySpan<StackValueItem> UnsafeItems
        {
            get
            {
                if (isSingle) return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(singleItem), 1);

                return !itemsLifetime.IsDisposed ? items.Span : throw new InvalidOperationException();
            }
        }

        public StackValueItems(StackValueItem singleItem) : this()
        {
            isSingle = true;
            this.singleItem = singleItem;
        }

        public StackValueItems(ReadOnlyMemory<StackValueItem> items, ValuePusher itemsLifetime) : this()
        {
            isSingle = false;
            this.items = items;
            this.itemsLifetime = itemsLifetime;
        }
    }
}