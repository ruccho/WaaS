using System;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Indicates to generate formatter for the alias type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class ComponentAliasAttribute : Attribute
    {
        public ComponentAliasAttribute(Type target)
        {
            Target = target;
        }

        public Type Target { get; }
    }
}