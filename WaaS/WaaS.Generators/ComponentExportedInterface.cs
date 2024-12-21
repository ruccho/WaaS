using Microsoft.CodeAnalysis;

namespace WaaS.Generators;

public record ComponentExportedInterface(INamedTypeSymbol Type, string ComponentName, string PropertyName);