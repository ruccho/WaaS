using System;

namespace WaaS.ComponentModel.Binding
{
    public class ComponentInterfaceAttribute : Attribute
    {
        public ComponentInterfaceAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}