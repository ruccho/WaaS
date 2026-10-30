using System;
using UnityEngine;
using WaaS.ComponentModel.Binding;
using Wukuro.Core;
using Object = UnityEngine.Object;

namespace Wukuro
{
    /// <summary>
    ///     A reference to a Unity object of type <typeparamref name="T" /> in a record passed to scripts, such as an
    ///     argument of an entry point. It is the <c>object</c> resource of <c>wukuro:core</c> in WIT, and the inspector
    ///     accepts only objects of type <typeparamref name="T" />.
    /// </summary>
    /// <remarks>
    ///     A record received by a host function gets the object of the resource, and the resource is dropped.
    ///     A GameObject is converted to its component when <typeparamref name="T" /> is a component.
    /// </remarks>
    [Serializable]
    [ComponentAlias(typeof(Owned<ITypes.IObjectResourceImpl>))]
    public partial struct ObjectOf<T> : IEquatable<ObjectOf<T>> where T : Object
    {
        [SerializeField] private T _value;

        public ObjectOf(T value)
        {
            _value = value;
        }

        public T Value => _value;

        public static implicit operator T(ObjectOf<T> value)
        {
            return value._value;
        }

        public static implicit operator ObjectOf<T>(T value)
        {
            return new ObjectOf<T>(value);
        }

        /// <summary>
        ///     Wraps the object to a resource passed to scripts. Used by the formatter.
        /// </summary>
        public static implicit operator Owned<ITypes.IObjectResourceImpl>(ObjectOf<T> value)
        {
            return (Owned<ITypes.IObjectResourceImpl>)ObjectImpl.Instance.Wrap(value._value);
        }

        /// <summary>
        ///     Takes the object out of a resource passed from scripts and drops the resource. Used by the formatter.
        /// </summary>
        public static implicit operator ObjectOf<T>(Owned<ITypes.IObjectResourceImpl> handle)
        {
            var value = ObjectImpl.Instance.Unwrap(handle.handle, false);
            handle.handle.Dispose();
            return new ObjectOf<T>(value is Object obj && !obj ? null : WukuroObjects.Convert<T>(value));
        }

        public bool Equals(ObjectOf<T> other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectOf<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value != null ? _value.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return _value != null ? _value.ToString() : "null";
        }
    }
}
