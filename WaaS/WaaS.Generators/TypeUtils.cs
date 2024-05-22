using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace WaaS.Generators;

public static class TypeUtils
{
    [ThreadStatic] private static StringBuilder? tempStringBuilder;

    public static bool Matches(this ISymbol symbol, string fullMetadataName)
    {
        var span = fullMetadataName.AsSpan();

        static bool MatchSymbol(ref ReadOnlySpan<char> span, ISymbol? symbol)
        {
            if (symbol == null) return true;
            if (symbol is INamespaceSymbol { IsGlobalNamespace: true }) return true;
            if (symbol is IModuleSymbol) return true;

            if (!MatchSymbol(ref span, symbol.ContainingSymbol)) return false;

            var segment = symbol.MetadataName.AsSpan();
            if (!span.StartsWith(segment, StringComparison.Ordinal)) return false;
            span = span.Slice(segment.Length);
            if (span.Length > 0 && span[0] == '.') span = span.Slice(1);

            return true;
        }

        return MatchSymbol(ref span, symbol);
    }

    public static string ToFullMetadataName(this ISymbol symbol)
    {
        var c = symbol.ContainingSymbol;

        if (c != null && c is not IModuleSymbol && c is not INamespaceSymbol { IsGlobalNamespace: true })
            return $"{ToFullMetadataName(c)}.{symbol.MetadataName}";

        return symbol.MetadataName;
    }

    public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol type, bool includeSelf)
    {
        if (includeSelf) yield return type;
        var c = type;

        while ((c = c.BaseType) != null) yield return c;
    }


    public static string ToCSharpClassDeclarationString(this ITypeSymbol type)
    {
        tempStringBuilder ??= new StringBuilder();
        tempStringBuilder.Clear();
        ToCSharpClassDeclarationString(type, tempStringBuilder);
        return tempStringBuilder.ToString();
    }

    private static void ToCSharpClassDeclarationString(this ITypeSymbol type, StringBuilder sb)
    {
        sb.Append(type.Name);

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            sb.Append("<");
            if (namedType.IsUnboundGenericType)
            {
                var isFirst = false;
                foreach (var typeParam in namedType.TypeParameters)
                {
                    if (!isFirst)
                        isFirst = true;
                    else
                        sb.Append(", ");
                    sb.Append(typeParam.Name);
                }
            }
            else
            {
                var isFirst = false;
                foreach (var typeArgument in namedType.TypeArguments)
                {
                    if (!isFirst)
                        isFirst = true;
                    else
                        sb.Append(", ");
                    ToCSharpClassDeclarationString(typeArgument, sb);
                }

                sb.Append(">");
            }
        }
    }

    public static bool HasTypeParameter(this ITypeSymbol type)
    {
        var containingType = type.ContainingType;
        if (containingType != null && HasTypeParameter(type)) return true;

        switch (type)
        {
            case IArrayTypeSymbol arrayTypeSymbol:
                return HasTypeParameter(arrayTypeSymbol.ElementType);
            case IDynamicTypeSymbol dynamicTypeSymbol:
                return false;
            case IErrorTypeSymbol errorTypeSymbol:
                return false;
            case IFunctionPointerTypeSymbol functionPointerTypeSymbol:
            {
                var sig = functionPointerTypeSymbol.Signature;
                return HasTypeParameter(sig.ReturnType) || sig.Parameters.Any(p => HasTypeParameter(p.Type));
            }
            case INamedTypeSymbol namedTypeSymbol:
                return namedTypeSymbol.TypeArguments.Any(HasTypeParameter);
            case IPointerTypeSymbol pointerTypeSymbol:
                return HasTypeParameter(pointerTypeSymbol.PointedAtType);
            case ITypeParameterSymbol typeParameterSymbol:
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public static IEnumerable<ITypeParameterSymbol> ExtractTypeParameters(this ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol arrayTypeSymbol:
            {
                foreach (var t in ExtractTypeParameters(arrayTypeSymbol.ElementType)) yield return t;

                break;
            }
            case IDynamicTypeSymbol dynamicTypeSymbol:
                break;
            case IErrorTypeSymbol errorTypeSymbol:
                break;
            case IFunctionPointerTypeSymbol functionPointerTypeSymbol:
            {
                var sig = functionPointerTypeSymbol.Signature;
                foreach (var t in ExtractTypeParameters(sig.ReturnType)) yield return t;

                foreach (var p in sig.Parameters)
                foreach (var t in ExtractTypeParameters(p.Type))
                    yield return t;

                break;
            }
            case INamedTypeSymbol namedTypeSymbol:
            {
                foreach (var a in namedTypeSymbol.TypeArguments)
                foreach (var t in ExtractTypeParameters(a))
                    yield return t;

                break;
            }
            case IPointerTypeSymbol pointerTypeSymbol:
            {
                foreach (var t in ExtractTypeParameters(pointerTypeSymbol.PointedAtType)) yield return t;

                break;
            }
            case ITypeParameterSymbol typeParameterSymbol:
            {
                yield return typeParameterSymbol;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}