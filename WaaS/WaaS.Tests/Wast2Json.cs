using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WaaS.Tests;

public class Wast2Json
{
    private readonly ILogger<Wast2Json> logger;

    public Wast2Json(ILogger<Wast2Json> logger)
    {
        this.logger = logger;
    }

    public async Task<WastProc> RunAsync(string wastPath, string binaryPath = "wast2json",
        CancellationToken ct = default)
    {
        var dir = Path.Combine(Path.GetTempPath(), "WaaS.Tests", "Wast2Json");
        var path = Path.GetFullPath(wastPath);

        Directory.CreateDirectory(dir);

        Console.WriteLine($"output: {dir}");

        var startInfo = new ProcessStartInfo(binaryPath, $@"--disable-bulk-memory -o ""out.json"" ""{path}""")
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
            proc = await JsonSerializer.DeserializeAsync<WastProc>(file, WastProc.SerializerOptions, ct) ??
                   throw new InvalidOperationException();
        }

        return proc;
    }
}