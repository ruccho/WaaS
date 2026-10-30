#nullable enable

namespace WaaS.ComponentModel.Runtime
{
    public static class ComponentInstanceExtensions
    {
        /// <summary>
        ///     Drops the resources whose handles are still owned by the component instance, such as resources a guest
        ///     did not drop. Call it when the instance is no longer used. The handles become invalid.
        /// </summary>
        public static void DropOwnedResources(this IInstance instance)
        {
            HandleTable.Of(instance).DropOwned();
        }
    }
}
