using System;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Indicates the interface is a component resource implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class ComponentResourceAttribute : Attribute
    {
        /// <summary>
        /// </summary>
        /// <param name="name">Component name of the resource.</param>
        public ComponentResourceAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}