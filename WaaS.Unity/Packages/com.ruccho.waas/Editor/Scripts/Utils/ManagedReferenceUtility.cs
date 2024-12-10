using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.Serialization;

namespace WaaS.Unity.Editor
{
    internal static class ManagedReferenceUtils
    {
        private static readonly Dictionary<string, Type> ManagedReferenceExpressionCache = new();

        public static Type GetManagedReferenceFieldType(this SerializedProperty property)
        {
            return FromManagedReferenceTypeExpression(property.managedReferenceFieldTypename);
        }

        public static Type GetManagedReferenceType(this SerializedProperty property)
        {
            if (property.managedReferenceId is ManagedReferenceUtility.RefIdNull
                or ManagedReferenceUtility.RefIdUnknown) return null;
            return FromManagedReferenceTypeExpression(property.managedReferenceFullTypename);
        }

        public static Type FromManagedReferenceTypeExpression(string expression)
        {
            if (string.IsNullOrEmpty(expression)) return null;
            if (ManagedReferenceExpressionCache.TryGetValue(expression, out var cachedType)) return cachedType;

            var expressionSpan = expression.AsSpan();
            var space = expressionSpan.IndexOf(' ');
            var assemblyName = expressionSpan[..space];
            expressionSpan = expressionSpan[(space + 1)..];

            Assembly matchedAssembly = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName();
                if (name.Name.AsSpan().SequenceEqual(assemblyName))
                {
                    matchedAssembly = assembly;
                    break;
                }
            }


            if (matchedAssembly == null) return null;

            var reader = new TypeNameReader(expressionSpan);

            return ManagedReferenceExpressionCache[expression] = reader.ReadFullName(matchedAssembly);
        }

        private ref struct TypeNameReader
        {
            private ReadOnlySpan<char> sequence;

            public TypeNameReader(ReadOnlySpan<char> expression)
            {
                sequence = expression;
            }

            public Type ReadAssemblyQualifiedName()
            {
                var typeName = ReadFullName(out var typeArguments);
                if (sequence.Length == 0 || sequence[0] != ',') throw new ArgumentException(nameof(sequence));

                sequence = sequence[1..].TrimStart(' ');
                var nextComma = sequence.IndexOf(',');
                if (nextComma < 0) throw new ArgumentException(nameof(sequence));
                var assemblyName = sequence[..nextComma];

                Assembly matchedAssembly = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    if (assembly.GetName().Name.AsSpan().SequenceEqual(assemblyName))
                    {
                        matchedAssembly = assembly;
                        break;
                    }

                if (matchedAssembly == null) return null;

                var endBrackets = sequence.IndexOf(']');
                if (endBrackets < 0)
                    sequence = default; // empty
                else
                    sequence = sequence[endBrackets..];

                var type = matchedAssembly.GetType(typeName.ToString());
                if (typeArguments is { Length: > 0 }) return type.MakeGenericType(typeArguments);

                return type;
            }

            public Type ReadFullName(Assembly assembly)
            {
                var typeName = ReadFullName(out var typeArguments);
                var type = assembly.GetType(typeName.ToString());
                if (typeArguments is { Length: > 0 }) return type.MakeGenericType(typeArguments);

                return type;
            }

            public ReadOnlySpan<char> ReadFullName(out Type[] typeArguments)
            {
                var bracket = sequence.IndexOf('[');
                ReadOnlySpan<char> result;
                if (bracket >= 0)
                {
                    result = sequence[..bracket];
                    sequence = sequence[(bracket + 1)..];

                    List<Type> typeArgumentsList = new();

                    while (true)
                    {
                        if (sequence.Length == 0) throw new ArgumentException(nameof(sequence));

                        var first = sequence[0];

                        sequence = sequence[1..];

                        switch (first)
                        {
                            case '[':
                                typeArgumentsList.Add(ReadAssemblyQualifiedName() ??
                                                      throw new InvalidOperationException());
                                if (sequence.Length == 0 || sequence[0] != ']')
                                    throw new ArgumentException(nameof(sequence));
                                sequence = sequence[1..];
                                if (sequence.Length >= 1 && sequence[0] == ',') sequence = sequence[1..];
                                sequence = sequence.TrimStart();
                                break;
                            case ']':
                                goto END;
                        }
                    }

                    END:
                    typeArguments = typeArgumentsList.ToArray();
                }
                else
                {
                    var comma = sequence.IndexOf(',');
                    if (comma < 0)
                    {
                        result = sequence;
                        sequence = default;
                    }
                    else
                    {
                        result = sequence[..comma];
                        sequence = sequence[comma..];
                    }

                    typeArguments = Array.Empty<Type>();
                }


                return result;
            }
        }
    }
}