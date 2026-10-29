using System;
using System.Collections.Generic;
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

        private static readonly Action<nint, nint, nint, nint> OnFileDelegate = OnFile;
        private static readonly nint OnFilePointer = Marshal.GetFunctionPointerForDelegate(OnFileDelegate);

        private static void OnFile(nint userData, nint name, nint ptr, nint len)
        {
            var contents = new byte[len];
            Marshal.Copy(ptr, contents, 0, (int)len);
            var files = (List<(string, byte[])>)GCHandle.FromIntPtr(userData).Target;
            files.Add((Marshal.PtrToStringAnsi(name), contents));
        }

        private static void OnFileError(nint userData, nint message)
        {
            var files = (List<(string, byte[])>)GCHandle.FromIntPtr(userData).Target;
            files.Clear();
            files.Add((null, System.Text.Encoding.UTF8.GetBytes(Marshal.PtrToStringAnsi(message) ?? "")));
        }

        private static readonly Action<nint, nint> OnFileErrorDelegate = OnFileError;
        private static readonly nint OnFileErrorPointer = Marshal.GetFunctionPointerForDelegate(OnFileErrorDelegate);

        [DllImport(DllName, CharSet = CharSet.Ansi)]
        private static extern void generate_moonbit_bindings(
            string wit_path,
            string world,
            string project_name,
            nint user_data,
            nint on_file,
            nint on_error);

        /// <summary>
        ///     Generates MoonBit bindings of a world. Returns the generated files with their paths relative to the output
        ///     directory.
        /// </summary>
        public static IReadOnlyList<(string Path, byte[] Contents)> GenerateMoonBitBindings(
            string witPath,
            string world,
            string projectName)
        {
            var files = new List<(string, byte[])>();
            var handle = GCHandle.Alloc(files);
            try
            {
                generate_moonbit_bindings(witPath, world, projectName, GCHandle.ToIntPtr(handle), OnFilePointer,
                    OnFileErrorPointer);
            }
            finally
            {
                handle.Free();
            }

            // an error is reported as a single entry without a path
            if (files.Count == 1 && files[0].Item1 == null)
                throw new Exception(System.Text.Encoding.UTF8.GetString(files[0].Item2));

            return files;
        }
    }

    internal enum StringEncoding : uint
    {
        UTF8,
        UTF16,
        CompactUTF16
    }
}