using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WaaS.Unity.Editor
{
    internal unsafe class NativeMethods
    {
        private const string DllName =
#if UNITY_EDITOR_WIN
                "waas_unity_native_editor"
#else
                "libwaas_unity_native_editor"
#endif
            ;

        // the delegates are kept alive while the native code may call them through the function pointers
        private static readonly Action<nint, nint, nint> OnSuccessDelegate = OnSuccess;
        private static readonly Action<nint, nint> OnErrorDelegate = OnError;
        private static readonly nint OnSuccessPointer = Marshal.GetFunctionPointerForDelegate(OnSuccessDelegate);
        private static readonly nint OnErrorPointer = Marshal.GetFunctionPointerForDelegate(OnErrorDelegate);

        private static void OnSuccess(nint userData, nint ptr, nint len)
        {
            var result = new byte[len];
            Marshal.Copy(ptr, result, 0, (int)len);
            Unsafe.AsRef<object>((void*)userData) = result;
        }

        private static void OnError(nint userData, nint message)
        {
            Unsafe.AsRef<object>((void*)userData) = Marshal.PtrToStringAnsi(message);
        }

        [DllImport(DllName)]
        private static extern void decode_wit(
            byte* source,
            nint source_len,
            void* user_data,
            nint on_success,
            nint on_error);

        /// <summary>
        ///     Decodes the type of a component and returns it as WIT.
        /// </summary>
        public static string DecodeWit(ReadOnlySpan<byte> component)
        {
            object result = null;
            var resultPtr = Unsafe.AsPointer(ref result);
            fixed (byte* sourcePtr = component)
            {
                decode_wit(sourcePtr, component.Length, resultPtr, OnSuccessPointer, OnErrorPointer);
            }

            if (result is byte[] bytes) return System.Text.Encoding.UTF8.GetString(bytes);
            if (result is string message) throw new Exception(message);
            throw new Exception();
        }

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        private static extern void componentize(
            byte* source,
            nint source_len,
            string wit_path,
            string world,
            StringEncoding encoding,
            void* user_data,
            nint on_success,
            nint on_error);

        public static byte[] Componentize(
            ReadOnlySpan<byte> source,
            string witPath,
            string world,
            StringEncoding encoding)
        {
            object result = null;
            var resultPtr = Unsafe.AsPointer(ref result);
            fixed (byte* sourcePtr = source)
            {
                componentize(sourcePtr, source.Length, witPath, world, encoding, resultPtr, OnSuccessPointer,
                    OnErrorPointer);
            }

            if (result is byte[] bytes) return bytes;

            if (result is string message) throw new Exception(message);

            throw new Exception();
        }
    }

    internal enum StringEncoding : uint
    {
        UTF8,
        UTF16,
        CompactUTF16
    }
}