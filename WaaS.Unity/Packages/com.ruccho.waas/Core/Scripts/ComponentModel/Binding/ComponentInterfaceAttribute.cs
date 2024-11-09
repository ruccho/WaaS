using System;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Indicates the interface is a component interface or world.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class ComponentInterfaceAttribute : Attribute
    {
        /// <summary>
        /// </summary>
        /// <param name="name">component name of the interface.</param>
        public ComponentInterfaceAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}