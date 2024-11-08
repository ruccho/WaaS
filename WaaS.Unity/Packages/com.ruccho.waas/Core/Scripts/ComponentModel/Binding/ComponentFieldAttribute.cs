using System;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Indicates the property is a field of the containing record type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ComponentFieldAttribute : Attribute
    {
    }
}