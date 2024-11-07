#nullable enable

using System;

namespace WaaS.ComponentModel.Binding
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ComponentApiAttribute : Attribute
    {
        public ComponentApiAttribute(bool ignore)
        {
            Ignore = ignore;
        }

        public ComponentApiAttribute(string name)
        {
            Name = name;
        }

        public bool Ignore { get; }
        public string? Name { get; }
    }
}