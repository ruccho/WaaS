using System;

namespace WaaS
{
    /// <summary>
    ///     Prevents Unity's managed code stripping from removing the annotated member.
    ///     UnityLinker recognizes any attribute named PreserveAttribute, regardless of its namespace or assembly,
    ///     so this definition avoids a dependency on UnityEngine.
    ///     When applied to a type, the type and its parameterless constructor are preserved.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct |
                    AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method |
                    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event |
                    AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
    internal sealed class PreserveAttribute : Attribute
    {
    }
}