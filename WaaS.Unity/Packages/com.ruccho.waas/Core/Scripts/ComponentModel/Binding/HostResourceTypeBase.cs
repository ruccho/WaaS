#nullable enable

using System;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Base class for host-provided resource types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class HostResourceTypeBase<T> : IHostResourceType<T>, IBorrowLendingResourceType
    {
        private ResourceTable<Entry> resources = new();

        public T ToHostResource(uint resourceId)
        {
            return resources.Get(unchecked((int)resourceId)).value;
        }

        public uint AllocateResourceId(T hostResource)
        {
            return unchecked((uint)resources.Add(new Entry(hostResource, false)));
        }

        public uint LendBorrow(uint index)
        {
            var entry = resources.Get(unchecked((int)index));
            return unchecked((uint)resources.Add(new Entry(entry.value, true)));
        }

        public uint New(uint rep)
        {
            throw new InvalidOperationException("resource.new is not supported for host resource types.");
        }

        public void Drop(uint index)
        {
            var entry = resources.RemoveAt(unchecked((int)index));

            // dropping a lent borrow only ends the borrow
            if (!entry.isBorrow) OnDrop(entry.value);
        }

        public uint Rep(uint index)
        {
            return index;
        }

        // host resource types are not defined by any component instance
        public virtual IInstance? Instance => null;

        public bool ValidateEquals(IType other)
        {
            return ReferenceEquals(other, this);
        }

        public Owned Wrap(T value)
        {
            return Ownership.GetHandle(this,
                AllocateResourceId(value));
        }

        public T Unwrap(Borrowed handle)
        {
            if (handle.Type != this) throw new ArgumentException(nameof(handle));
            return ToHostResource(handle.GetValue());
        }

        public T Unwrap(Owned handle, bool moveOut = true)
        {
            if (handle.Type != this) throw new ArgumentException(nameof(handle));
            return ToHostResource(handle.GetValue(moveOut));
        }

        protected virtual void OnDrop(T value)
        {
        }

        public void Reset()
        {
            resources = new();
        }

        private readonly struct Entry
        {
            public readonly T value;
            public readonly bool isBorrow;

            public Entry(T value, bool isBorrow)
            {
                this.value = value;
                this.isBorrow = isBorrow;
            }
        }
    }
}