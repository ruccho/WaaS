using System;

namespace WaaS.Models
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    internal class OperandAttribute : Attribute
    {
        public OperandAttribute(sbyte index)
        {
            Index = index;
        }

        public sbyte Index { get; }
        public bool Signed { get; set; }
        public ulong MinValue { get; set; }
    }
}