using System;

namespace WaaS.Unity.Editor
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    internal class ManagedReferenceTypeDisplayNameAttribute : Attribute
    {
        public ManagedReferenceTypeDisplayNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}