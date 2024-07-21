using System.Diagnostics;
using System.Text;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;

namespace WaaS.Tests;

internal static class Utils
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    private static string ParseWat(string wat)
    {
        var dir = Path.Combine(Path.GetTempPath(), "WaaS.Tests");
        var watPath = Path.Combine(dir, "temp.wat");
        var wasmPath = Path.Combine(dir, "temp.wasm");

        File.WriteAllText(watPath, wat, Utf8NoBom);

        var psi = new ProcessStartInfo("wasm-tools", $"parse -o \"{wasmPath}\" \"{watPath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = dir
        };

        using var wat2wasm = new Process();

        wat2wasm.StartInfo = psi;

        wat2wasm.OutputDataReceived += (sender, args) => { Console.WriteLine(args.Data); };
        wat2wasm.ErrorDataReceived += (sender, args) => { Console.WriteLine(args.Data); };

        wat2wasm.Start();
        wat2wasm.BeginErrorReadLine();
        wat2wasm.BeginOutputReadLine();

        wat2wasm.WaitForExit();
        return wasmPath;
    }

    public static Instance GetInstance(string wat, Imports imports)
    {

        var module = Module.Create(File.ReadAllBytes(ParseWat(wat)));
        return new Instance(module, imports);
    }

    public static IComponent GetComponent(string wat)
    {
        return Component.Create(File.ReadAllBytes(ParseWat(wat)));
    }

    public static IInstance GetInstance(string wat, IReadOnlyDictionary<string, ISortedExportable> imports)
    {
        var component = Component.Create(File.ReadAllBytes(ParseWat(wat)));
        return component.Instantiate(null, imports);
    }
}