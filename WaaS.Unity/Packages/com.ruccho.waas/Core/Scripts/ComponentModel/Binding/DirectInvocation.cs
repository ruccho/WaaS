using System.Runtime.CompilerServices;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Conversions from core values used by the generated bindings. Numbers lifted from a guest have the same
    ///     canonicalization as <see cref="WaaS.ComponentModel.Runtime.ValueLifter" />.
    /// </summary>
    public static class DirectInvocation
    {
        public static float LiftF32(in StackValueItem value)
        {
            var result = value.ExpectValueF32();
            if (float.IsNaN(result)) Unsafe.As<float, uint>(ref result) = 0x7fc00000;
            return result;
        }

        public static double LiftF64(in StackValueItem value)
        {
            var result = value.ExpectValueF64();
            if (double.IsNaN(result)) Unsafe.As<double, ulong>(ref result) = 0x7ff8000000000000;
            return result;
        }
    }
}
