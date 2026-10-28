using System;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Holds the WIT definition of an interface declared with <see cref="ComponentInterfaceAttribute" />.
    ///     This attribute is applied by the source generator and is used to export WIT files.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class ComponentWitDefinitionAttribute : Attribute
    {
        /// <param name="package">package name of the interface, such as "my-namespace:my-package".</param>
        /// <param name="interfaceName">name of the interface in the package.</param>
        /// <param name="definition">WIT definition of the interface, without the package declaration.</param>
        public ComponentWitDefinitionAttribute(string package, string interfaceName, string definition)
        {
            Package = package;
            InterfaceName = interfaceName;
            Definition = definition;
        }

        public string Package { get; }
        public string InterfaceName { get; }
        public string Definition { get; }
    }
}
