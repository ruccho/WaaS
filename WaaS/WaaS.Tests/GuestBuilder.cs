using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WaaS.Tests;

internal static class GuestBuilder
{
    private static readonly Dictionary<string, string> Built = new();

    /// <summary>
    ///     Builds a guest crate under Guests/ and returns the path of the component.
    /// </summary>
    /// <param name="name">name of the crate</param>
    /// <param name="witOverrides">
    ///     WIT files that replace the files in the wit directory of the crate (file name -> source).
    ///     When specified, the crate is copied to a temporary directory and built there.
    /// </param>
    public static string Build(string name, IReadOnlyDictionary<string, string>? witOverrides = null)
    {
        lock (Built)
        {
            var key = witOverrides == null ? name : $"{name}+overrides";
            if (Built.TryGetValue(key, out var cached)) return cached;

            var crateDir = Path.Combine(GetSourceDirectory(), "Guests", name);
            var targetDir = Environment.GetEnvironmentVariable("WAAS_TESTS_GUEST_TARGET_DIR") is { Length: > 0 } dir
                ? dir
                : Path.Combine(Utils.TempDirectory, "guest-target");

            if (witOverrides != null)
            {
                var copiedDir = Path.Combine(Utils.TempDirectory, "guest-overrides", name);
                if (Directory.Exists(copiedDir)) Directory.Delete(copiedDir, true);
                CopyDirectory(crateDir, copiedDir);
                foreach (var (fileName, source) in witOverrides)
                    File.WriteAllText(Path.Combine(copiedDir, "wit", fileName), source);
                crateDir = copiedDir;
                targetDir += "-overrides";
            }
            Run("cargo",
                $"build --release --target wasm32-unknown-unknown --target-dir \"{targetDir}\"", crateDir);

            var modulePath = Path.Combine(targetDir, "wasm32-unknown-unknown", "release",
                $"{name.Replace('-', '_')}.wasm");
            var componentPath = Path.Combine(targetDir, $"{name}.component.wasm");
            Run("wasm-tools", $"component new \"{modulePath}\" -o \"{componentPath}\"", crateDir);

            Built[key] = componentPath;
            return componentPath;
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        foreach (var directory in Directory.GetDirectories(source))
        {
            var name = Path.GetFileName(directory);
            if (name is "target") continue;
            CopyDirectory(directory, Path.Combine(destination, name));
        }
    }

    private static string GetSourceDirectory([CallerFilePath] string path = "")
    {
        return Path.GetDirectoryName(path)!;
    }

    private static void Run(string fileName, string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new Exception($"{fileName} {arguments} failed:\n{stdout.Result}\n{stderr.Result}");
    }
}
