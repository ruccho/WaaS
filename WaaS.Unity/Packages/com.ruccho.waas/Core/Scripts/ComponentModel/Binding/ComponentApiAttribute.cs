#nullable enable

using System;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Indicates the method is API of the component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ComponentApiAttribute : Attribute
    {
        public ComponentApiAttribute(string name)
        {
            Name = name;
        }

        public string? Name { get; }
    }
}