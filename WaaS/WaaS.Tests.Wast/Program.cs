using ConsoleAppFramework;
using WaaS.Tests.Wast;

await ConsoleApp.RunAsync(args, Commands.RunAsync);

internal static class Commands
{
    /// <summary>
    ///     Runs the WebAssembly spec tests (*.wast) in a directory.
    /// </summary>
    /// <param name="directory">-d, Directory that contains the *.wast files.</param>
    public static async Task RunAsync(string directory)
    {
        var wast2Json = new Wast2Json();
        foreach (var file in Directory.EnumerateFiles(directory, "*.wast", SearchOption.TopDirectoryOnly))
        {
            var proc = await wast2Json.RunAsync(file);
            Console.WriteLine($"{proc.SourceFilename}: running...");
            try
            {
                WastRunner.Run(proc);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{proc.SourceFilename}: {ex}");
                throw;
            }

            Console.WriteLine($"{proc.SourceFilename}: succeeded");
        }
    }
}
