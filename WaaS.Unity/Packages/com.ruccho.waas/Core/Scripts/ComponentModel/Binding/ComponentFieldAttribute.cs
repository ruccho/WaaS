using System;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Indicates the property or the field is a field of the containing record type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ComponentFieldAttribute : Attribute
    {
    }
}