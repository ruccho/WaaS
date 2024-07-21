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
}