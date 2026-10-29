using System;

namespace Wukuro
{
    /// <summary>
    ///     Declares a type of Unity objects passed to scripts. A marker type implementing <c>wukuro::ObjectType</c> is
    ///     generated for Rust (<see cref="Wukuro.Editor.WukuroObjectTypeExporter" />), and a parameter
    ///     <c>&amp;ObjectOf&lt;T&gt;</c> of an entry point accepts only objects of the type in the inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class WukuroObjectTypeAttribute : Attribute
    {
        public WukuroObjectTypeAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }
}
