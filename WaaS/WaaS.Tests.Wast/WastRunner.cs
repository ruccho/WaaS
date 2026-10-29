using WaaS.Models;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests.Wast;

public class WastRunner : IDisposable
{
    private readonly Dictionary<string, Instance> instances = new();

    private WastRunner(string directory)
    {
        Directory = directory;
    }

    public string Directory { get; }
    public Instance? CurrentInstance { get; private set; }
    public Imports CurrentImports { get; } = SpecTest.CreateImports();

    public ExecutionContext Context { get; } = new(ushort.MaxValue);

    public void Dispose()
    {
        CurrentInstance?.Dispose();
        Context.Dispose();
    }

    private void AddInstance(Module module, string? name)
    {
        CurrentInstance = new Instance(module, CurrentImports);
        if (!string.IsNullOrEmpty(name)) instances[name] = CurrentInstance;
    }

    public void AddInstance(string filename, string? name)
    {
        AddInstance(Module.Create(File.ReadAllBytes(Path.Combine(Directory, filename))), name);
    }

    public Instance Instantiate(string filename)
    {
        return new Instance(Module.Create(File.ReadAllBytes(Path.Combine(Directory, filename))), CurrentImports);
    }


    public Instance GetInstance(string name)
    {
        return instances[name];
    }

    public void Register(Instance instance, string name)
    {
        var imports = new ModuleExports();
        foreach (var (key, value) in instance.ExportInstance.Items)
            if (value is IExternal importValue)
                imports[key] = importValue;
            else
                throw new InvalidOperationException($"item {key} of type {value.GetType()} is not importable.");

        CurrentImports[name] = imports;
    }

    /// <summary>
    ///     Runs all commands in the script. A failed command is reported and does not stop the following ones.
    /// </summary>
    public static WastResult Run(WastProc proc)
    {
        var directory = Path.Combine(Path.GetTempPath(), "WaaS.Tests.Wast", "Wast2Json") ??
                        throw new InvalidOperationException();

        using var runner = new WastRunner(directory);

        var result = new WastResult();
        foreach (var command in proc.Commands.Span)
            try
            {
                command.Execute(runner);
                result.Passed++;
            }
            catch (NotSupportedException)
            {
                Console.WriteLine($"command in line {command.Line} is not supported!");
                result.Unsupported++;
            }
            catch (NotImplementedException)
            {
                Console.WriteLine($"command in line {command.Line} is not supported!");
                result.Unsupported++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"command in line {command.Line} failed! {ex.GetType().Name}: {ex.Message}");
                result.Failures.Add($"line {command.Line}: {ex.GetType().Name}: {FirstLine(ex.Message)}");
            }

        return result;
    }

    private static string FirstLine(string text)
    {
        var index = text.IndexOf('\n');
        return index < 0 ? text : text[..index].TrimEnd('\r');
    }
}

public class WastResult
{
    public int Passed { get; set; }
    public int Unsupported { get; set; }
    public List<string> Failures { get; } = new();
}
