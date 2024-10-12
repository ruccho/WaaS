using System;

namespace WaaS.ComponentModel.Binding
{
    public class ComponentFieldAttribute : Attribute
    {
        public string Name { get; }

        public ComponentFieldAttribute(string name)
        {
            Name = name;
        }
    }
}