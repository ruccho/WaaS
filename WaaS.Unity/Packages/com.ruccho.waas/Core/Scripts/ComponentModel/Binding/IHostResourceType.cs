#nullable enable
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    public interface IHostResourceType<T> : IResourceType
    {
        T ToHostResource(uint resourceId);
        uint AllocateResourceId(T hostResource);
    }
}