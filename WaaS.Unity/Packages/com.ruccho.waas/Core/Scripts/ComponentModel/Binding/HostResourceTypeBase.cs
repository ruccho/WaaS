using System;
using WaaS.ComponentModel.Runtime;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Binding
{
    public abstract class HostResourceTypeBase<T> : IHostResourceType<T> where T : class
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

        public void Drop(ExecutionContext context, uint index)
        {
            resources.RemoveAt(unchecked((int)index));
        }

        public Owned Wrap(T value)
        {
            return Ownership.GetHandle(this,
                AllocateResourceId(value), null);
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
    }
}