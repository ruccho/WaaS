#if WAAS_NATIVE_INTERPRETER
using System;
using System.Runtime.InteropServices;

namespace WaaS.Runtime
{
    internal sealed unsafe partial class CompiledFunction
    {
        private GCHandle[] pinned;

        ~CompiledFunction()
        {
            if (pinned == null) return;
            foreach (var handle in pinned) handle.Free();
        }

        /// <summary>
        ///     Fixes the addresses of the arrays and writes them to the data for the interpreter on pointers.
        /// </summary>
        public void FillNative(NativeFunction* native)
        {
            lock (this)
            {
                pinned ??= new[]
                {
                    GCHandle.Alloc(RegisterCode, GCHandleType.Pinned),
                    GCHandle.Alloc(InitialLocals, GCHandleType.Pinned),
                    GCHandle.Alloc(RegisterBranchTargets, GCHandleType.Pinned)
                };
            }

            native->Code = (RegisterInstruction*)pinned[0].AddrOfPinnedObject();
            native->InitialLocals = (StackValueItem*)pinned[1].AddrOfPinnedObject();
            native->BranchTargets = (int*)pinned[2].AddrOfPinnedObject();
            native->NumParameters = NumParameters;
            native->NumLocals = InitialLocals.Length;
            native->NumResults = NumResults;
            native->MaxStackDepth = MaxStackDepth;
        }
    }
}
#endif
