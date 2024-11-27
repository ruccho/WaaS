using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace WaaS.Generators;

internal readonly record struct ComponentApi
{
    public readonly bool isAwaitable;
    public readonly ISymbol memberSymbol;
    public readonly ImmutableArray<IParameterSymbol> parameters;
    public readonly ITypeSymbol? returnType;
    public readonly ITypeSymbol? unawaitedReturnType;

    public ComponentApi(ISymbol memberSymbol)
    {
        this.memberSymbol = memberSymbol;
        {
            if (memberSymbol is IMethodSymbol method)
            {
                unawaitedReturnType = method.ReturnsVoid ? null : method.ReturnType;
                parameters = method.Parameters;
            }
            else if (memberSymbol is IPropertySymbol property)
            {
                unawaitedReturnType = property.Type;
                parameters = ImmutableArray<IParameterSymbol>.Empty;
            }
            else
            {
                unawaitedReturnType = null;
                parameters = ImmutableArray<IParameterSymbol>.Empty;
            }
        }
        isAwaitable = false;
        if (unawaitedReturnType?.IsAwaitable(out var taskResultType) ?? false)
        {
            returnType = taskResultType;
            isAwaitable = true;
        }
        else
        {
            returnType = unawaitedReturnType;
        }
    }

    public string ExternalFunctionName => $"{memberSymbol.ContainingType.Name}_{memberSymbol.Name}";
}