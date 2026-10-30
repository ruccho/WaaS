namespace WaaS.Tests;

/// <summary>
///     Measures the performance of the interpreter on guest workloads. Run it explicitly with the Release configuration,
///     e.g. <c>dotnet test -c Release --filter "FullyQualifiedName~InterpreterBenchmarks"</c>. Setting
///     <c>DOTNET_TieredCompilation=0</c> makes the results stable on CoreCLR.
///     The Rust workloads require cargo with the wasm32-unknown-unknown target and wasm-tools.
/// </summary>
[Explicit("benchmark")]
public class InterpreterBenchmarks
{
    private const int Iterations = 7;

    private static readonly Lazy<IReadOnlyList<InterpreterWorkload>> Workloads =
        new(() => InterpreterWorkloads.Create(GuestBuilder.Build("interpreter-bench")));

    // the workloads are created lazily so that discovering the tests does not build the guest
    [TestCase("loop")]
    [TestCase("memory")]
    [TestCase("fib")]
    [TestCase("float")]
    [TestCase("strings")]
    [TestCase("lists")]
    public void Run(string name)
    {
        var workload = Workloads.Value.Single(w => w.Name == name);
        var result = InterpreterWorkloads.Measure(workload, Iterations);
        TestContext.Out.WriteLine(result.ToString());
    }
}
