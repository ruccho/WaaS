using System;

namespace WaaS.ComponentModel.Binding
{
    public class ComponentAliasAttribute : Attribute
    {
        public Type Target { get; }

        public ComponentAliasAttribute(Type target)
        {
            Target = target;
        }
    }
}