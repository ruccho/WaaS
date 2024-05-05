using Microsoft.Extensions.DependencyInjection;
using WaaS.Tests;

var builder = ConsoleApp.CreateBuilder(args);

builder.ConfigureServices((ctx, services) => { services.AddSingleton<Wast2Json>(); });


var app = builder.Build();
app.AddRootCommand(async (Wast2Json wast2Json, [Option("d", "directory")] string dir) =>
{
    foreach (var file in Directory.EnumerateFiles(dir, "*.wast", SearchOption.TopDirectoryOnly))
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
});

app.Run();