using ConsoleAppFramework;
using WaaS.Tests.Wast;

await ConsoleApp.RunAsync(args, Commands.RunAsync);

internal static class Commands
{
    /// <summary>
    ///     Runs the WebAssembly spec tests (*.wast) in a directory.
    /// </summary>
    /// <param name="directory">-d, Directory that contains the *.wast files.</param>
    /// <param name="recursive">-r, Also runs the *.wast files in the subdirectories.</param>
    public static async Task<int> RunAsync(string directory, bool recursive = false)
    {
        var wast2Json = new Wast2Json();
        var summary = new List<(string File, WastResult? Result)>();

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var file in Directory.EnumerateFiles(directory, "*.wast", searchOption).Order())
        {
            var name = Path.GetRelativePath(directory, file);
            Console.WriteLine($"{name}: running...");

            WastResult? result;
            try
            {
                result = WastRunner.Run(await wast2Json.RunAsync(file));
            }
            catch (Exception ex)
            {
                // wast2json failed to convert the script
                Console.WriteLine($"{name}: {ex}");
                result = null;
            }

            summary.Add((name, result));
        }

        Console.WriteLine();
        Console.WriteLine("Summary:");

        var failed = 0;
        foreach (var (file, result) in summary)
        {
            if (result == null)
            {
                failed++;
                Console.WriteLine($"ERROR {file}: failed to convert");
                continue;
            }

            var status = result.Failures.Count == 0 ? "PASS " : "FAIL ";
            if (result.Failures.Count > 0) failed++;
            Console.WriteLine(
                $"{status} {file}: passed {result.Passed}, failed {result.Failures.Count}, unsupported {result.Unsupported}");
            foreach (var failure in result.Failures) Console.WriteLine($"        {failure}");
        }

        Console.WriteLine($"{summary.Count - failed} / {summary.Count} files passed.");
        return failed == 0 ? 0 : 1;
    }
}
