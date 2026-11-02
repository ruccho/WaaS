#if WAAS_NATIVE_INTERPRETER
using System.Runtime.InteropServices;

namespace WaaS.Runtime
{
    public unsafe partial class Instance
    {
        private NativeInstanceData nativeData;

        /// <summary>
        ///     Gets the data of this instance for the interpreter on pointers, which is created when it is first needed.
        /// </summary>
        internal NativeInstance* GetNative()
        {
            lock (this)
            {
                return (nativeData ??= new NativeInstanceData(this)).Instance;
            }
        }

        private sealed class NativeInstanceData
        {
            public readonly NativeInstance* Instance;

            public NativeInstanceData(Instance instance)
            {
                var functions = instance.FunctionInstance.FunctionArray;
                var globals = instance.GlobalInstance.GlobalArray;

                Instance = (NativeInstance*)Marshal.AllocHGlobal(sizeof(NativeInstance));
                Instance->Functions = (NativeFunction*)Marshal.AllocHGlobal(sizeof(NativeFunction) * functions.Length);
                Instance->Globals = (StackValueItem**)Marshal.AllocHGlobal(sizeof(StackValueItem*) * globals.Length);

                for (var i = 0; i < globals.Length; i++) Instance->Globals[i] = globals[i].GetPointer();

                for (var i = 0; i < functions.Length; i++)
                {
                    var native = Instance->Functions + i;
                    *native = default;
                    native->Instance = Instance;

                    // the functions of other instances and the host are called by the managed side
                    if (functions[i] is not InstanceFunction function || function.instance != instance) continue;

                    function.function.Compiled.FillNative(native);
                    native->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(function, GCHandleType.Weak));
                    function.Native = native;
                }
            }

            ~NativeInstanceData()
            {
                Marshal.FreeHGlobal((System.IntPtr)Instance->Globals);
                Marshal.FreeHGlobal((System.IntPtr)Instance->Functions);
                Marshal.FreeHGlobal((System.IntPtr)Instance);
            }
        }
    }
}
#endif
