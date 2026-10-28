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

    public static readonly DiagnosticDescriptor WitGenerationDiagnostic = new(
        "WAAS0002",
        "The interface cannot be represented in WIT",
        "The interface cannot be represented in WIT: {0}",
        "Compiler",
        DiagnosticSeverity.Warning,
        true
    );
}