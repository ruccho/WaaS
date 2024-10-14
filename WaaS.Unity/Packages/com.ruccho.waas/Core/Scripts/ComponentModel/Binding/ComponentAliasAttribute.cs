using System;

namespace WaaS.ComponentModel.Binding
{
    public class ComponentAliasAttribute : Attribute
    {
        public ComponentAliasAttribute(Type target)
        {
            Target = target;
        }

        public Type Target { get; }
    }
}