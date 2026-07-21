using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WaaS.Tests.Wast;

public class Wast2Json
{
    private readonly ILogger<Wast2Json> logger;

    public Wast2Json(ILogger<Wast2Json> logger)
    {
        this.logger = logger;
    }

    public async Task<WastProc> RunAsync(string wastPath, CancellationToken ct = default)
    {
        var dir = Path.Combine(Path.GetTempPath(), "WaaS.Tests.Wast", "Wast2Json");
        var path = Path.GetFullPath(wastPath);

        Directory.CreateDirectory(dir);

        Console.WriteLine($"wast: {path}");
        Console.WriteLine($"output: {dir}");

        var startInfo = new ProcessStartInfo("wasm-tools", $@"json-from-wast -o ""out.json"" ""{path}""")
        {
            UseShellExecute = false,
            WorkingDirectory = dir,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        using (var process = new Process { StartInfo = startInfo })
        {
            process!.OutputDataReceived += (sender, args) => { Console.WriteLine($"{args.Data}"); };

            process.ErrorDataReceived += (sender, args) => { Console.WriteLine($"{args.Data}"); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0) throw new InvalidOperationException();
        }

        WastProc proc;
        using (var file = File.OpenRead(Path.Combine(dir, "out.json")))
        {
            proc = JsonSerializer.Deserialize<WastProc>(file, WastProc.SerializerOptions) ??
                   throw new InvalidOperationException();
        }

        return proc;
    }
}