using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests;

[ComponentInterface(@"waas:native-test/api")]
public partial interface INativeTestApi
{
    [ComponentApi(@"add")]
    ValueTask<uint> Add(uint a, uint b);
}

/// <summary>
///     Tests the native library of the Unity editor committed to the package on the platform running the tests, so
///     that the libraries for the platforms other than the one used for the development are also tested in CI.
/// </summary>
public class NativeEditorLibraryTests
{
    private const string Wit = """
                               package waas:native-test;

                               interface api {
                                 add: func(a: u32, b: u32) -> u32;
                               }

                               world native-test {
                                 export api;
                               }
                               """;

    private const string CoreWat = """
                                   (module
                                     (memory (export "memory") 1)
                                     (func (export "waas:native-test/api#add") (param i32 i32) (result i32)
                                       (i32.add (local.get 0) (local.get 1)))
                                   )
                                   """;

    private nint library;

    [OneTimeSetUp]
    public void LoadLibrary()
    {
        var (directory, file) = (RuntimeInformation.OSArchitecture, OperatingSystem.IsWindows(),
                OperatingSystem.IsMacOS(), OperatingSystem.IsLinux()) switch
            {
                (Architecture.X64, true, _, _) => ("x86_64-pc-windows-msvc", "waas_unity_native_editor.dll"),
                (Architecture.X64, _, true, _) => ("x86_64-apple-darwin", "libwaas_unity_native_editor.dylib"),
                (Architecture.Arm64, _, true, _) => ("aarch64-apple-darwin", "libwaas_unity_native_editor.dylib"),
                (Architecture.X64, _, _, true) => ("x86_64-unknown-linux-gnu", "libwaas_unity_native_editor.so"),
                _ => (null, null)
            };
        if (directory == null) Assert.Ignore($"No native library is built for {RuntimeInformation.RuntimeIdentifier}.");

        var path = Path.Combine(GetRepositoryRoot(), "WaaS.Unity", "Packages", "com.ruccho.waas", "Editor", "Plugins",
            directory, file!);
        library = NativeLibrary.Load(path);
    }

    [OneTimeTearDown]
    public void FreeLibrary()
    {
        if (library != 0) NativeLibrary.Free(library);
    }

    private static string GetRepositoryRoot([CallerFilePath] string path = "")
    {
        // WaaS/WaaS.Tests/NativeEditorLibraryTests.cs
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
    }

    [Test]
    public async Task ComponentizedModuleRunsOnWaaS()
    {
        var witDirectory = Path.Combine(Utils.TempDirectory, "native-test-wit");
        Directory.CreateDirectory(witDirectory);
        await File.WriteAllTextAsync(Path.Combine(witDirectory, "native-test.wit"), Wit);

        var binary = Componentize(Utils.GetModuleBinary(CoreWat), witDirectory, "native-test");

        var component = Component.Create(binary);
        var instance = component.Instantiate(new Dictionary<string, ISortedExportable>());
        Assert.That(instance.TryGetExport("waas:native-test/api", out IInstance? api), Is.True);

        using var context = new ExecutionContext();
        Assert.That(await new INativeTestApi.Wrapper(api!, context).Add(40, 2), Is.EqualTo(42u));

        // the type of the component is decoded as WIT
        Assert.That(DecodeWit(binary), Does.Contain("add: func(a: u32, b: u32) -> u32"));
    }

    [Test]
    public void ErrorsAreReported()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => DecodeWit([0x00, 0x61, 0x73, 0x6D]));
        Assert.That(exception!.Message, Is.Not.Empty);
    }

    private unsafe byte[] Componentize(byte[] module, string witPath, string world)
    {
        var componentize = (delegate* unmanaged[Cdecl]<byte*, nint, byte*, byte*, uint, nint, nint, nint, void>)
            NativeLibrary.GetExport(library, "componentize");
        var witPathBytes = Encoding.UTF8.GetBytes(witPath + "\0");
        var worldBytes = Encoding.UTF8.GetBytes(world + "\0");

        object? result = null;
        fixed (byte* source = module)
        fixed (byte* witPathPointer = witPathBytes)
        fixed (byte* worldPointer = worldBytes)
        {
            componentize(source, module.Length, witPathPointer, worldPointer, 0 /* UTF-8 */,
                (nint)Unsafe.AsPointer(ref result), (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&OnSuccess,
                (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&OnError);
        }

        return GetResult(result);
    }

    private unsafe string DecodeWit(byte[] component)
    {
        var decodeWit = (delegate* unmanaged[Cdecl]<byte*, nint, nint, nint, nint, void>)
            NativeLibrary.GetExport(library, "decode_wit");

        object? result = null;
        fixed (byte* source = component)
        {
            decodeWit(source, component.Length, (nint)Unsafe.AsPointer(ref result),
                (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&OnSuccess,
                (nint)(delegate* unmanaged[Cdecl]<nint, nint, void>)&OnError);
        }

        return Encoding.UTF8.GetString(GetResult(result));
    }

    private static byte[] GetResult(object? result)
    {
        return result switch
        {
            byte[] bytes => bytes,
            string message => throw new InvalidOperationException(message),
            _ => throw new InvalidOperationException("The native library reported no result.")
        };
    }

    // the user data is the address of the local variable that receives the result
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnSuccess(nint userData, nint pointer, nint length)
    {
        Unsafe.AsRef<object>((void*)userData) = new ReadOnlySpan<byte>((void*)pointer, (int)length).ToArray();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OnError(nint userData, nint message)
    {
        Unsafe.AsRef<object>((void*)userData) = Marshal.PtrToStringUTF8(message) ?? "";
    }
}
