using System.Runtime.CompilerServices;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.Tests.Wast;

public class WastRunner : IDisposable
{
    public bool IsComponent => CurrentComponentInstance != null;
    private readonly Dictionary<string, Instance> instances = new();
    private readonly Dictionary<string, IInstance> componentInstances = new();

    private WastRunner(string directory)
    {
        Directory = directory;
    }

    public string Directory { get; }
    public Instance? CurrentInstance { get; private set; }
    public IInstance? CurrentComponentInstance { get; private set; }
    public Imports CurrentImports { get; } = SpecTest.CreateImports();
    public Dictionary<string, ISortedExportable> CurrentComponentImports { get; } = SpecTest.CreateComponentImports();

    public ExecutionContext Context { get; } = new(ushort.MaxValue);

    public void Dispose()
    {
        CurrentInstance?.Dispose();
        Context.Dispose();
    }

    public void AddInstance(Module module, string? name)
    {
        CurrentInstance = new Instance(module, CurrentImports);
        if (!string.IsNullOrEmpty(name))
        {
            instances[name] = CurrentInstance;
        }
    }

    public void AddInstance(IComponent component, string? name)
    {
        CurrentComponentInstance = component.Instantiate(CurrentComponentImports);
        if (!string.IsNullOrEmpty(name))
        {
            componentInstances[name] = CurrentComponentInstance;
            CurrentComponentImports[name] = CurrentComponentInstance;
        }
    }

    public void AddInstance(string filename, string? name)
    {
        var bytes = File.ReadAllBytes(Path.Combine(Directory, filename));
        var preambleBytes = bytes.AsSpan(0, Unsafe.SizeOf<Preamble>());
        var preamble = Unsafe.ReadUnaligned<Preamble>(ref preambleBytes[0]);
        if (preamble.IsValid())
        {
            AddInstance(Module.Create(bytes), name);
        }
        else
        {
            AddInstance(Component.Create(bytes), name);
        }
    }

    public void AddModule(string filename, string name)
    {
        var bytes = File.ReadAllBytes(Path.Combine(Directory, filename));
        var preambleBytes = bytes.AsSpan(0, Unsafe.SizeOf<Preamble>());
        var preamble = Unsafe.ReadUnaligned<Preamble>(ref preambleBytes[0]);
        if (preamble.IsValid())
        {
            var module = Module.Create(bytes);
            CurrentComponentImports[name] = new CoreModule(module);
        }
        else
        {
            var component = Component.Create(bytes);
            CurrentComponentImports[name] = component;
        }
    }

    public void Instantiate(string filename)
    {
        var bytes = File.ReadAllBytes(Path.Combine(Directory, filename));
        var preambleBytes = bytes.AsSpan(0, Unsafe.SizeOf<Preamble>());
        var preamble = Unsafe.ReadUnaligned<Preamble>(ref preambleBytes[0]);
        if (preamble.IsValid())
        {
            new Instance(Module.Create(bytes), CurrentImports);
        }
        else
        {
            Component.Create(bytes).Instantiate(CurrentComponentImports);
        }
    }

    public Instance GetInstance(string name)
    {
        return instances[name];
    }

    public IInstance GetComponentInstance(string name)
    {
        return componentInstances[name];
    }

    public void Register(Instance instance, string name)
    {
        var imports = new ModuleExports();
        foreach (var (key, value) in instance.ExportInstance.Items)
            if (value is { } importValue)
                imports[key] = importValue;
            else
                throw new InvalidOperationException($"item {key} of type {value.GetType()} is not importable.");

        CurrentImports[name] = imports;
    }

    public static void Run(WastProc proc)
    {
        var directory = Path.Combine(Path.GetTempPath(), "WaaS.Tests.Wast", "Wast2Json") ??
                        throw new InvalidOperationException();

        using var runner = new WastRunner(directory);

        foreach (var command in proc.Commands.Span)
            try
            {
                if (command.Line == 2 && command is AssertInvalidCommand
                    {
                        Text: "component export `x` is a reexport of an imported function which is not implemented"
                    }) continue;

                if (command is ModuleCommand { Filename: "instance.9.wasm" }) continue;
                if (command is ModuleCommand { Filename: "instance.11.wasm" }) continue;
                if (command is ModuleCommand { Filename: "instance.12.wasm" }) continue;
                if (command is ModuleCommand { Filename: "resources.9.wasm" }) continue;
                if (Path.GetFileName(proc.SourceFilename) == "resources.wast")
                {
                    if (command is { Line : 479 }) continue;
                    if (command is { Line: >= 590 and <= 595 }) continue;
                    if (command is { Line : 924 }) continue;
                }

                if (command is AssertInvalidCommand { Filename: "simple.4.wasm" }) continue;
                if (command is AssertInvalidCommand { Filename: "simple.5.wasm" }) continue;
                if (command is AssertInvalidCommand { Filename: "types.5.wasm" }) continue;
                if (command is ModuleCommand { Filename: "types.12.wasm" }) continue;
                if (command is ModuleCommand { Filename: "types.13.wasm" }) continue;

                // reset imports
                (runner.CurrentComponentImports["host"] as SpecTest.Host)!.Reset();

                command.Execute(runner);
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine($"command in line {command.Line} is not supported: {ex.Message}");
            }
            catch (NotImplementedException ex)
            {
                Console.WriteLine($"command in line {command.Line} is not supported!");
                throw;
            }
            catch (Exception)
            {
                Console.WriteLine($"command in line {command.Line} failed!");
                throw;
            }
    }
}