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
        // the index in this table is used as the rep of the resource
        private ResourceTable<T> resources = new();

        public T ToHostResource(uint resourceId)
        {
            return resources.Get(unchecked((int)resourceId));
        }

        public uint AllocateResourceId(T hostResource)
        {
            return unchecked((uint)resources.Add(hostResource));
        }

        public void Drop(uint rep)
        {
            OnDrop(resources.RemoveAt(unchecked((int)rep)));
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
    }
}