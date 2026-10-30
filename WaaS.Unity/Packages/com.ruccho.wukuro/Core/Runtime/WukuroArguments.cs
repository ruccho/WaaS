using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;
using Wukuro.Core;
using Object = UnityEngine.Object;

namespace Wukuro
{
    [Serializable]
    internal class Arguments : IArguments
    {
        [SerializeReference] private ArgumentContainerBase[] _arguments = new ArgumentContainerBase[0];

        public bool TryGetArgument(IContext context, string name, out ITypes.Argument result)
        {
            // NOTE: should we use a dictionary for faster lookup?
            foreach (var container in _arguments)
            {
                if (container != null && container.Name == name)
                {
                    // values of argument types are given by TryGetValue
                    if (container.IsArgumentType)
                    {
                        result = default;
                        return false;
                    }

                    result = container switch
                    {
                        ArgumentContainer<bool> c => ITypes.Argument.CreateTyBool(c.Value),
                        ArgumentContainer<sbyte> c => ITypes.Argument.CreateTyS8(c.Value),
                        ArgumentContainer<short> c => ITypes.Argument.CreateTyS16(c.Value),
                        ArgumentContainer<int> c => ITypes.Argument.CreateTyS32(c.Value),
                        ArgumentContainer<long> c => ITypes.Argument.CreateTyS64(c.Value),
                        ArgumentContainer<byte> c => ITypes.Argument.CreateTyU8(c.Value),
                        ArgumentContainer<ushort> c => ITypes.Argument.CreateTyU16(c.Value),
                        ArgumentContainer<uint> c => ITypes.Argument.CreateTyU32(c.Value),
                        ArgumentContainer<ulong> c => ITypes.Argument.CreateTyU64(c.Value),
                        ArgumentContainer<float> c => ITypes.Argument.CreateTyF32(c.Value),
                        ArgumentContainer<double> c => ITypes.Argument.CreateTyF64(c.Value),
                        ArgumentContainer<ComponentChar> c => ITypes.Argument.CreateTyChar(
                            new WaaS.ComponentModel.Binding.ComponentChar(c.Value)),
                        ArgumentContainer<string> c => ITypes.Argument.CreateTyString(c.Value),
                        ArgumentContainer<UnityEvent> c => ITypes.Argument.CreateTyAction(
                            (Owned<ITypes.IActionResourceImpl>)context.ActionType.Wrap(() => { c.Value?.Invoke(); })),
                        ArgumentContainer<Object> c => ITypes.Argument.CreateTyObject(
                            (Owned<ITypes.IObjectResourceImpl>)context.ObjectType.Wrap(c.Value)),
                        _ => throw new NotImplementedException()
                    };
                    return true;
                }
            }

            result = default;
            return false;
        }

        public bool TryGetValue(string name, out object value)
        {
            foreach (var container in _arguments)
                if (container != null && container.Name == name && container.IsArgumentType)
                {
                    value = container.BoxedValue;
                    return true;
                }

            value = null;
            return false;
        }
    }

    [Serializable]
    public abstract class ArgumentContainerBase
    {
        [SerializeField] private string _name;
        public string Name => _name;

        public ArgumentContainerBase()
        {
        }

        protected ArgumentContainerBase(string name)
        {
            _name = name;
        }

        /// <summary>
        ///     Type of the value in WIT, such as "s32" or "borrow&lt;action&gt;".
        /// </summary>
        public abstract string WitType { get; }

        /// <summary>
        ///     Whether the value is of an argument type (<see cref="WukuroArgumentTypes" />).
        /// </summary>
        internal virtual bool IsArgumentType => false;

        internal virtual object BoxedValue => null;

        /// <summary>
        ///     Creates a container for a parameter of an entry point, or returns null if the type is not supported.
        ///     A parameter of a record or an enum is given a container of the C# type declaring it.
        /// </summary>
        public static ArgumentContainerBase Create(EntryParameterInfo parameter)
        {
            if (parameter.TypeInterface != null)
            {
                var type = WukuroArgumentTypes.Find(parameter.TypeInterface, parameter.WitType);
                return type == null
                    ? null
                    : (ArgumentContainerBase)Activator.CreateInstance(
                        typeof(ArgumentContainer<>).MakeGenericType(type), parameter.Name,
                        type.IsValueType ? Activator.CreateInstance(type) : null);
            }

            return Create(parameter.Name, parameter.WitType);
        }

        /// <summary>
        ///     Creates a container for a parameter of an entry point, or returns null if the type is not supported.
        /// </summary>
        public static ArgumentContainerBase Create(string name, string witType)
        {
            return witType switch
            {
                "bool" => new ArgumentContainer<bool>(name, default),
                "s8" => new ArgumentContainer<sbyte>(name, default),
                "s16" => new ArgumentContainer<short>(name, default),
                "s32" => new ArgumentContainer<int>(name, default),
                "s64" => new ArgumentContainer<long>(name, default),
                "u8" => new ArgumentContainer<byte>(name, default),
                "u16" => new ArgumentContainer<ushort>(name, default),
                "u32" => new ArgumentContainer<uint>(name, default),
                "u64" => new ArgumentContainer<ulong>(name, default),
                "f32" => new ArgumentContainer<float>(name, default),
                "f64" => new ArgumentContainer<double>(name, default),
                "char" => new ArgumentContainer<ComponentChar>(name, default),
                "string" => new ArgumentContainer<string>(name, ""),
                "borrow<action>" => new ArgumentContainer<UnityEvent>(name, new UnityEvent()),
                "borrow<object>" => new ArgumentContainer<Object>(name, null),
                _ => null
            };
        }
    }

    [Serializable]
    public class ArgumentContainer<T> : ArgumentContainerBase
    {
        [SerializeField] private T _value;

        public T Value => _value;

        public ArgumentContainer(string name, T value) : base(name)
        {
            _value = value;
        }

        public override string WitType => _value switch
        {
            bool => "bool",
            sbyte => "s8",
            short => "s16",
            int => "s32",
            long => "s64",
            byte => "u8",
            ushort => "u16",
            uint => "u32",
            ulong => "u64",
            float => "f32",
            double => "f64",
            ComponentChar => "char",
            _ when typeof(T) == typeof(string) => "string",
            _ when typeof(T) == typeof(UnityEvent) => "borrow<action>",
            _ when typeof(Object).IsAssignableFrom(typeof(T)) => "borrow<object>",
            _ when IsArgumentTypeOfT => WukuroArgumentTypes.GetWitName(typeof(T)),
            _ => ""
        };

        // ReSharper disable once StaticMemberInGenericType
        private static readonly bool IsArgumentTypeOfT = WukuroArgumentTypes.IsArgumentType(typeof(T));

        internal override bool IsArgumentType => IsArgumentTypeOfT;

        internal override object BoxedValue => _value;
    }

    [Serializable]
    public struct ComponentChar : IEquatable<ComponentChar>
    {
        [SerializeField] private uint _value;

        public ComponentChar(uint value)
        {
            _value = value;
        }

        public bool Equals(ComponentChar other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is ComponentChar other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)_value;
        }

        public static bool operator ==(ComponentChar left, ComponentChar right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComponentChar left, ComponentChar right)
        {
            return !left.Equals(right);
        }

        public static implicit operator uint(ComponentChar componentChar)
        {
            return componentChar._value;
        }

        public static implicit operator ComponentChar(uint value)
        {
            return new ComponentChar { _value = value };
        }
    }
}