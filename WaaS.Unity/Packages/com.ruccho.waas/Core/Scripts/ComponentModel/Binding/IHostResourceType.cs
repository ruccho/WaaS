#nullable enable
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Host-provided resource type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHostResourceType<T> : IResourceType
    {
        T ToHostResource(uint resourceId);
        uint AllocateResourceId(T hostResource);
    }
}