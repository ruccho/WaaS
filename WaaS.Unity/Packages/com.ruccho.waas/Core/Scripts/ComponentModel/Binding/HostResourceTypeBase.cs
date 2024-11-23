#nullable enable

using System;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Base class for host-provided resource types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class HostResourceTypeBase<T> : IHostResourceType<T>
    {
        private readonly ResourceTable<T> resources = new();

        public T ToHostResource(uint resourceId)
        {
            return resources.Get(unchecked((int)resourceId));
        }

        public uint AllocateResourceId(T hostResource)
        {
            return unchecked((uint)resources.Add(hostResource));
        }

        public uint New(uint rep)
        {
            throw new InvalidOperationException("resource.new is not supported for host resource types.");
        }

        public void Drop(uint index)
        {
            OnDrop(resources.RemoveAt(unchecked((int)index)));
        }

        public uint Rep(uint index)
        {
            return index;
        }

        public virtual IInstance? Instance => throw new InvalidOperationException();

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
    }
}