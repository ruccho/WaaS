using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace WaaS.Generators;

[Generator(LanguageNames.CSharp)]
public class ComponentFormatterGenerator : IIncrementalGenerator
{
    private const string FormatNamespace = "WaaS.ComponentModel.Models";
    private const string InterfaceNamespace = "WaaS.ComponentModel.Runtime";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var source = context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{FormatNamespace}.GenerateFormatterAttribute",
            static (node, ct) => true,
            (context, ct) => context);

        context.RegisterSourceOutput(source, Emit);
    }

    private void Emit(SourceProductionContext sourceContext, GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol) return;

        var variantSelectors = typeSymbol.GetAttributes().Where(attr =>
            attr.AttributeClass.Matches($"{FormatNamespace}.VariantAttribute")).ToArray();
        var variantFallbackSelectors = typeSymbol.GetAttributes().Where(attr =>
            attr.AttributeClass.Matches($"{FormatNamespace}.VariantFallbackAttribute")).ToArray();

        StringBuilder sourceBuilder = new();

        var ns = typeSymbol.ContainingNamespace;

        if (!ns.IsGlobalNamespace) sourceBuilder.AppendLine($"namespace {ns}\n{{");
        sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                partial {{
                    typeSymbol.TypeKind switch
                    {
                        TypeKind.Interface => "interface",
                        TypeKind.Struct => "struct",
                        _ => "class"
                    }
                }} {{typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat.RemoveMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType))}}
                {
                    static {{typeSymbol.Name}}()
                    {
                        global::{{FormatNamespace}}.Formatter<{{typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>.Default = new Formatter();
                    }
                    
                
                    internal class Formatter : global::{{FormatNamespace}}.IFormatter<{{typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>
                    {
                        public bool TryRead(ref global::WaaS.Models.ModuleReader reader, global::{{FormatNamespace}}.IIndexSpace indexSpace, out {{typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} result)
                        {
                """);

        bool generateConstructor = false;
        var properties = typeSymbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p
                .GetAttributes()
                .Any(attr => attr.AttributeClass.Matches($"{FormatNamespace}.IgnoreAttribute"))).ToArray();

        if (variantSelectors.Any() || variantFallbackSelectors.Any())
        {
            // variant code

            if (variantSelectors.Any())
            {
                sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                            var tag = reader.Clone().ReadUnaligned<byte>();
                            switch (tag)
                            {
                """);

                foreach (var variantSelector in variantSelectors)
                {
                    var tag = variantSelector.ConstructorArguments[0].Value;
                    var type = variantSelector.ConstructorArguments[1].Value;
                    if (tag == null) continue;
                    if (type is not ITypeSymbol variantType) continue;
                    var tagValue = (byte)tag;
                    sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                                case 0x{{tagValue:X2}}:
                                    reader.ReadUnaligned<byte>();
                                    result = global::{{FormatNamespace}}.Formatter<{{variantType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>.Read(ref reader, indexSpace, false);
                                    return true;
                """);
                }

                sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                            }
                """);
            }

            if (variantFallbackSelectors.Any())
            {
                for (var i = 0; i < variantFallbackSelectors.Length; i++)
                {
                    var variantFallbackSelector = variantFallbackSelectors[i];
                    var type = variantFallbackSelector.ConstructorArguments[0].Value;
                    if (type is not ITypeSymbol variantType) continue;
                    sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                            if(global::{{FormatNamespace}}.Formatter<{{variantType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>.TryRead(ref reader, indexSpace, out var f{{i}}))
                            {
                                result = f{{i}};
                                return true;
                            }
                """);
                }
            }

            sourceBuilder.AppendLine(
/* lang=c#  */$$"""         
                            result = default;
                            return false;
                """);
        }
        else
        {
            sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                            result = new {{typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}(
                """);

            bool isFirst = true;
            foreach (var propertySymbol in properties)
            {
                generateConstructor = true;
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    sourceBuilder.AppendLine(", ");
                }

                sourceBuilder.Append("                ");

                bool dontAddToSort = propertySymbol.GetAttributes()
                    .Any(attr => attr.AttributeClass.Matches($"{FormatNamespace}.DontAddToSortAttribute"));
                AppendValueReader(propertySymbol.Type, dontAddToSort, sourceBuilder);
            }

            sourceBuilder.AppendLine("\n            );");

            sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                            return true;
                """);
        }

        sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                        }
                    }
                """);

        if (generateConstructor)
        {
            sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                    private {{typeSymbol.Name}}({{string.Join(", ", properties.Select(p => $"{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {Utils.ToCamelCase(p.Name.AsSpan())}"))}})
                    {
                """);

            if (typeSymbol.TypeKind is TypeKind.Struct)
            {
                sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                        this = default;
                """);
            }

            sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                        {{string.Join("\n        ", properties.Select(p => $"{p.Name} = {Utils.ToCamelCase(p.Name.AsSpan())};"))}}
                    }
                """);
        }

        sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                }
                """);

        if (!ns.IsGlobalNamespace) sourceBuilder.AppendLine($"}} // namespace {ns}");

        var fullType = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "")
            .Replace("<", "_")
            .Replace(">", "_");

        sourceContext.AddSource($"{fullType}.formatter.g.cs", sourceBuilder.ToString());
    }


    private static void AppendValueReader(ITypeSymbol type, bool dontAddToSort, StringBuilder sourceBuilder)
    {   
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            sourceBuilder.Append(
                $"reader.ReadOptional(static (ref global::WaaS.Models.ModuleReader reader, IIndexSpace indexSpace) => ");
            AppendValueReader(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated), dontAddToSort, sourceBuilder);
            sourceBuilder.Append($", indexSpace)");
            return;
        }

        if (type.SpecialType is SpecialType.System_Byte)
        {
            sourceBuilder.Append($"reader.ReadUnaligned<byte>()");
            return;
        }

        if (type.SpecialType is SpecialType.System_UInt32)
        {
            sourceBuilder.Append($"reader.ReadUnalignedLeb128U32()");
            return;
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            sourceBuilder.Append($"reader.ReadUtf8String()");
            return;
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            sourceBuilder.Append(
                $"reader.ReadUnaligned<{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()");
            return;
        }

        if (type is INamedTypeSymbol namedType)
        {
            if (type.Matches("System.ReadOnlyMemory`1"))
            {
                sourceBuilder.Append(
                    $"reader.ReadVector(static (ref global::WaaS.Models.ModuleReader reader, IIndexSpace indexSpace) => ");
                AppendValueReader(namedType.TypeArguments[0], dontAddToSort, sourceBuilder);
                sourceBuilder.Append($", indexSpace)");
                return;
            }

            if (namedType.Matches($"{FormatNamespace}.IUnresolved`1"))
            {
                sourceBuilder.Append(
                    $"indexSpace.Get<{namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(reader.ReadUnalignedLeb128U32())");
                return;
            }
        }

        sourceBuilder.Append(
            $"global::{FormatNamespace}.Formatter<{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Read(ref reader, indexSpace, {(dontAddToSort ? "false" : "true")})");
    }
}