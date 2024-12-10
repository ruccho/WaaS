using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WaaS.Unity.Editor
{
    internal unsafe class NativeMethods
    {
        private const string DllName = "waas_unity_native_editor";

        private static nint? onSuccess;
        private static nint? onError;

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

            static void OnSuccess(nint userData, nint ptr, nint len)
            {
                var result = new byte[len];
                Marshal.Copy(ptr, result, 0, (int)len);
                Unsafe.AsRef<object>((void*)userData) = result;
            }

            static void OnError(nint userData, nint message)
            {
                Unsafe.AsRef<object>((void*)userData) = Marshal.PtrToStringAnsi(message);
            }

            var onSuccess = NativeMethods.onSuccess ??=
                Marshal.GetFunctionPointerForDelegate((Action<nint, nint, nint>)OnSuccess);
            var onError = NativeMethods.onError ??= Marshal.GetFunctionPointerForDelegate((Action<nint, nint>)OnError);

            var resultPtr = Unsafe.AsPointer(ref result);
            fixed (byte* sourcePtr = source)
            {
                componentize(sourcePtr, source.Length, witPath, world, encoding, resultPtr, onSuccess, onError);
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