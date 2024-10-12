using System;

namespace WaaS.ComponentModel.Binding
{
    public class ComponentInterfaceAttribute : Attribute
    {
        public string Name { get; }
        
        public ComponentInterfaceAttribute(string name)
        {
            this.Name = name;
        }
    }
}