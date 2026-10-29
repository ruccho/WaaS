using WaaS.ComponentModel.Binding;
using Wukuro.Core;

namespace Wukuro
{
    /// <summary>
    ///     Accesses the values of <c>wukuro:core/types.context</c> resources in host implementations.
    /// </summary>
    public static class WukuroContexts
    {
        /// <summary>
        ///     The resource type of <c>wukuro:core/types.context</c>.
        /// </summary>
        public static ITypes.IContextResourceImpl ResourceType => ContextImpl.Instance;

        /// <summary>
        ///     Returns the context of the script passed to a host function.
        /// </summary>
        public static IContext Get(Borrowed<ITypes.IContextResourceImpl> handle)
        {
            return ContextImpl.Instance.Unwrap(handle.handle);
        }

        /// <summary>
        ///     Returns the module of type <typeparamref name="T" /> added to the runner of the script with
        ///     <see cref="WukuroRunner.AddModule" />, or throws if it is not added.
        /// </summary>
        public static T GetModule<T>(Borrowed<ITypes.IContextResourceImpl> handle) where T : IWukuroModule
        {
            return Get(handle).GetModule<T>();
        }
    }
}
