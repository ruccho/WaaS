using System;
using WaaS.ComponentModel.Binding;
using Wukuro.Core;
using Object = UnityEngine.Object;

namespace Wukuro
{
    /// <summary>
    ///     Accesses the values of <c>wukuro:core/types.object</c> resources in host implementations.
    /// </summary>
    public static class WukuroObjects
    {
        /// <summary>
        ///     The resource type of <c>wukuro:core/types.object</c>.
        /// </summary>
        public static ITypes.IObjectResourceImpl ResourceType => ObjectImpl.Instance;

        /// <summary>
        ///     Returns the object passed as an argument, or throws if it is not of type <typeparamref name="T" />.
        ///     A <see cref="UnityEngine.GameObject" /> is converted to its component when <typeparamref name="T" /> is a component
        ///     or an interface.
        /// </summary>
        public static T Get<T>(Borrowed<ITypes.IObjectResourceImpl> handle) where T : class
        {
            return Convert<T>(ObjectImpl.Instance.Unwrap(handle.handle));
        }

        internal static T Convert<T>(object value) where T : class
        {
            switch (value)
            {
                case T typed:
                    return typed;
                case UnityEngine.GameObject gameObject
                    when typeof(T).IsInterface || typeof(UnityEngine.Component).IsAssignableFrom(typeof(T)):
                    if (gameObject.GetComponent(typeof(T)) is T found) return found;
                    break;
                case UnityEngine.Component component when typeof(T) == typeof(UnityEngine.GameObject):
                    return component.gameObject as T;
                case UnityEngine.Component component
                    when typeof(T).IsInterface || typeof(UnityEngine.Component).IsAssignableFrom(typeof(T)):
                    if (component.GetComponent(typeof(T)) is T sibling) return sibling;
                    break;
            }

            throw new ArgumentException(
                $"The object argument is not {typeof(T).Name} ({(value is Object obj && obj ? obj.GetType().Name : "null")}).");
        }
    }
}
