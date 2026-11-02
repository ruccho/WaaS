#if WAAS_NATIVE_INTERPRETER
#if WAAS_BURST
using Unity.Burst;
#endif

namespace WaaS.Runtime
{
    /// <summary>
    ///     The reasons for which <see cref="NativeInterpreter.Run" /> returns.
    /// </summary>
    internal static class NativeExit
    {
        public const int Completed = 0;
        public const int Call = 1;
        public const int CallIndirect = 2;
        public const int Fallback = 3;
        public const int OutOfFuel = 4;
        public const int OutOfBounds = 5;
        public const int DivideByZero = 6;
        public const int Grow = 7;
        public const int Poll = 8;
        public const int ReturnToAnotherInstance = 9;
        public const int InvalidOperation = 10;
    }

    internal unsafe struct NativeInstance
    {
        public NativeFunction* Functions;
        public StackValueItem** Globals;
    }

    /// <summary>
    ///     A function for the interpreter on pointers. <see cref="Code" /> is null for the functions that the managed side
    ///     has to call.
    /// </summary>
    internal unsafe struct NativeFunction
    {
        public RegisterInstruction* Code;
        public StackValueItem* InitialLocals;
        public int* BranchTargets;
        public NativeInstance* Instance;
        public System.IntPtr Handle;
        public int NumParameters;
        public int NumLocals;
        public int NumResults;
        public int MaxStackDepth;
    }

    internal unsafe struct NativeActivation
    {
        public NativeFunction* Function;

        // the index of the call in the register code
        public int ReturnPc;
        public int FrameBase;
    }

    internal unsafe struct NativeFrameState
    {
        public NativeFunction* Function;
        public RegisterInstruction* Ip;
        public StackValueItem* Values;
        public long Capacity;
        public byte* Memory;
        public ulong MemoryLength;
        public NativeActivation* Activations;
        public long Fuel;
        public int FrameBase;
        public int Poll;
        public int ActivationCount;
        public int ActivationLimit;
        public int ExitArgument;
    }

    internal static partial class NativeInterpreter
    {
        /// <summary>
        ///     Whether the loop is compiled to native code. It runs as slowly as to be useless otherwise.
        /// </summary>
        public static readonly bool IsAvailable = Probe();

#if WAAS_BURST
        [BurstCompile(CompileSynchronously = true)]
        private static bool Probe()
        {
            var result = true;
            DiscardedByBurst(ref result);
            return result;
        }

        [BurstDiscard]
        private static void DiscardedByBurst(ref bool value)
        {
            value = false;
        }
#else
        private static bool Probe()
        {
            return true;
        }
#endif
    }
}
#endif
