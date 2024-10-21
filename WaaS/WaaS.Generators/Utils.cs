using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace WaaS.Generators;

public static class Utils
{
    [ThreadStatic] private static List<string>? _tempStringList;

    public static string ToTypeParameterConstraintsClause(this ITypeParameterSymbol symbol)
    {
        _tempStringList ??= new List<string>();
        _tempStringList.Clear();

        if (symbol.HasValueTypeConstraint && !symbol.HasUnmanagedTypeConstraint) _tempStringList.Add("struct");

        if (symbol.HasReferenceTypeConstraint) _tempStringList.Add("class");

        if (symbol.HasNotNullConstraint) _tempStringList.Add("notnull");

        if (symbol.HasUnmanagedTypeConstraint) _tempStringList.Add("unmanaged");

        foreach (var baseTypeConstraint in symbol.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Class))
            _tempStringList.Add(baseTypeConstraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        foreach (var baseTypeConstraint in symbol.ConstraintTypes.Where(t => t.TypeKind == TypeKind.Interface))
            _tempStringList.Add(baseTypeConstraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        if (symbol.HasConstructorConstraint) _tempStringList.Add("new()");

        if (_tempStringList.Count == 0) return "";

        return $"where {symbol.Name} : {string.Join(", ", _tempStringList)}";
    }

    public static string ToCamelCase(ReadOnlySpan<char> source)
    {
        if (source.Length == 0) return source.ToString();

        Span<char> span = stackalloc char[source.Length];
        source.CopyTo(span);
        span[0] = span[0] is >= 'A' and <= 'Z' ? (char)(span[0] + ('a' - 'A')) : span[0];
        return span.ToString();
    }

    public static string ToComponentApiName(ISymbol member)
    {
        {
            var attr = member.GetAttributes()
                .FirstOrDefault(attr =>
                    attr.AttributeClass?.Matches("WaaS.ComponentModel.Binding.ComponentApiAttribute") ?? false);
            if (attr != null && attr.ConstructorArguments.Length == 1 &&
                attr.ConstructorArguments[0].Value is string name && !string.IsNullOrEmpty(name))
                return name;
        }

        // count heads
        {
            var name = member.Name;
            var isLower = false;
            var numHeads = 0;
            for (var i = 0; i < member.Name.Length; i++)
            {
                var isLowerCurrent = !char.IsUpper(name[i]);
                if (isLower && !isLowerCurrent) numHeads++;

                isLower = isLowerCurrent;
            }

            Span<char> apiName = stackalloc char[name.Length + numHeads];
            var cursor = 0;
            isLower = false;
            for (var i = 0; i < member.Name.Length; i++)
            {
                var isLowerCurrent = !char.IsUpper(name[i]);
                if (isLower && !isLowerCurrent) apiName[cursor++] = '-';

                apiName[cursor++] = char.ToLowerInvariant(name[i]);
                isLower = isLowerCurrent;
            }

            return apiName.ToString();
        }
    }

    public static bool IsAwaitable(this ITypeSymbol symbol, out ITypeSymbol? resultType)
    {
        var getAwaiters = symbol.GetMembers().OfType<IMethodSymbol>().Where(x =>
            x.Name == WellKnownMemberNames.GetAwaiter && x.DeclaredAccessibility == Accessibility.Public &&
            !x.Parameters.Any());
        foreach (var methodSymbol in getAwaiters)
        {
            if (!VerifyGetAwaiter(methodSymbol, out resultType)) continue;
            return true;
        }

        resultType = null;
        return false;
    }

    private static bool VerifyGetAwaiter(IMethodSymbol getAwaiter, out ITypeSymbol? resultType)
    {
        resultType = default;

        var returnType = getAwaiter.ReturnType;
        if (returnType == null) return false;

        if (!returnType.GetMembers().OfType<IPropertySymbol>().Any(p =>
                p.Name == WellKnownMemberNames.IsCompleted && p.Type.SpecialType == SpecialType.System_Boolean &&
                p.GetMethod != null))
            return false;

        var methods = returnType.GetMembers().OfType<IMethodSymbol>();

        if (!methods.Any(x =>
                x.Name == WellKnownMemberNames.OnCompleted && x.ReturnsVoid &&
                x.Parameters.Length == 1 && x.Parameters.First().Type.TypeKind == TypeKind.Delegate))
            return false;

        foreach (var m in methods)
            if (m.Name == WellKnownMemberNames.GetResult && !m.Parameters.Any())
            {
                if (!m.ReturnsVoid) resultType = m.ReturnType;
                return true;
            }

        return false;
    }
}