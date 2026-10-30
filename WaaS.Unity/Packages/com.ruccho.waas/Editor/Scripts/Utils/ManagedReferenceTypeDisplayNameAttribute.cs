using System;

namespace WaaS.Unity.Editor
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ManagedReferenceTypeDisplayNameAttribute : Attribute
    {
        public ManagedReferenceTypeDisplayNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}