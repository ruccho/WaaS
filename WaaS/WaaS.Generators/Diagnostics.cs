using Microsoft.CodeAnalysis;

namespace WaaS.Generators;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor NestedClassDiagnostic = new(
        "WAAS0001",
        "ComponentInstanceAttribute must be applied to a top-level class",
        "ComponentInstanceAttribute must be applied to a top-level class",
        "Compiler",
        DiagnosticSeverity.Error,
        true
    );
}