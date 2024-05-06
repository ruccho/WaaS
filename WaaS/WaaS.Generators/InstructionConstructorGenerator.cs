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
public class InstructionConstructorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var source = context.SyntaxProvider
            .CreateSyntaxProvider(
                (n, ct) =>
                {
                    return n is TypeDeclarationSyntax typeDecl &&
                           typeDecl.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword));
                },
                (context, ct) => context.SemanticModel.GetDeclaredSymbol(context.Node) as INamedTypeSymbol)
            .Where(s => s is not null)
            .Where(type => type.GetBaseTypes(false).Any(t => t.Matches("WaaS.Models.Instruction")));

        context.RegisterSourceOutput(source, EmitConstructor);
        context.RegisterSourceOutput(source.Collect(), EmitReader);
    }

    private void EmitReader(SourceProductionContext sourceContext, ImmutableArray<INamedTypeSymbol?> types)
    {
        var targetTypes = types.WhereNotNull().Where(t => !t.IsAbstract);

        if (!targetTypes.Any()) return;

        var sourceBuilder = new StringBuilder();

        sourceBuilder.AppendLine(
/*  lang=c# */"""
              namespace WaaS.Models
              {
                  partial class InstructionReader
                  {
                      private static partial global::WaaS.Models.Instruction Read(ref global::WaaS.Models.ModuleReader reader, uint count)
                      {
                          var opCode = reader.ReadUnaligned<byte>();
                          byte opCode1;
                          return opCode switch
                          {
              """);


        foreach (var group0 in targetTypes.Select(t => new InstructionTypeInfo(t)).GroupBy(t => t.opCode0))
        {
            if (group0.Key is null) continue;

            if (group0.Any(t => t.opCode1 != null))
            {
                sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                                0x{{group0.Key.Value:X2}} => (opCode1 = reader.ReadUnaligned<byte>()) switch
                                {
                """);
                foreach (var group1 in group0.GroupBy(t => t.opCode1))
                {
                    if (group1.Key is null) continue;
                    sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                                    0x{{group1.Key.Value:X2}} => {{group1.First().type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.Read(ref reader, count),
                """);
                }

                sourceBuilder.AppendLine(
/*  lang=c# */"""
                                  _ => throw new global::System.InvalidOperationException($"Unsupported OpCpde:0x{opCode:X2} 0x{opCode1:X2} at 0x{(reader.Position - 2):X8}")
                              },
              """);
            }
            else
            {
                foreach (var typeInfo in group0)
                    sourceBuilder.AppendLine(
                        /*  lang=c# */
                        $$"""
                                          0x{{group0.Key.Value:X2}} => {{typeInfo.type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.Read(ref reader, count),
                          """);
            }
        }


        sourceBuilder.AppendLine(
/*  lang=c# */"""
                              _ => throw new global::System.InvalidOperationException($"Unsupported OpCpde:0x{opCode:X2} at 0x{(reader.Position - 1):X8}")
                          };
                      }
                  }
              }
              """);

        sourceContext.AddSource("InstructionReader.g.cs",
            SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

    private void EmitConstructor(SourceProductionContext sourceContext, INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return;

        var opcodeAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass.Matches("WaaS.Models.OpCodeAttribute"));

        var baseTypes = typeSymbol.GetBaseTypes(true).ToArray();

        List<OperandItem> targets = new();

        var inBlockInstrCursor = int.MaxValue;
        foreach (var type in baseTypes)
        {
            var members = type.GetMembers();
            foreach (var member in members)
            {
                ITypeSymbol memberType;
                if (member is IPropertySymbol property)
                    memberType = property.Type;
                else if (member is IFieldSymbol field)
                    memberType = field.Type;
                else
                    continue;

                var operand = member.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Matches("WaaS.Models.OperandAttribute") ?? false);

                if (operand == null) continue;
                var indexValue = operand.ConstructorArguments.Length >= 1
                    ? operand.ConstructorArguments[0].Value
                    : null;
                if (indexValue == null) continue;

                var isSignedObj = operand.NamedArguments.FirstOrDefault(arg => arg.Key == "Signed").Value.Value;
                var signed = isSignedObj != null && (bool)isSignedObj;
                var minValueObj = operand.NamedArguments.FirstOrDefault(arg => arg.Key == "MinValue").Value.Value;
                var minValue = (ulong?)minValueObj;
                if (minValue is 0) minValue = null;


                int index = (sbyte)indexValue;

                targets.Add(new OperandItem(index, member.Name,
                    memberType,
                    SymbolEqualityComparer.Default.Equals(type, typeSymbol),
                    signed, minValue));
            }
        }

        var orderedTargets = targets.OrderBy(t => t.Index).ToArray();

        var sourceBuilder = new StringBuilder();

        void PrintContainingSymbols(ISymbol symbol, bool isOpen)
        {
            if (symbol.ContainingSymbol != null) PrintContainingSymbols(symbol.ContainingSymbol, isOpen);

            if (symbol is INamespaceSymbol { IsGlobalNamespace: false })
                sourceBuilder.AppendLine(isOpen
                    ?
                    /*  lang=c# */
                    $$"""
                      namespace {{symbol.Name}}
                      {
                      """
                    :
                    /*  lang=c# */
                    $$"""
                      } // {{symbol.Name}}
                      """);
            else if (symbol is ITypeSymbol type)
                sourceBuilder.AppendLine(isOpen
                    ?
                    /*  lang=c# */
                    $$"""
                      partial class {{type.ToCSharpClassDeclarationString()}}
                      {
                      """
                    :
                    /*  lang=c# */
                    $$"""
                      } // {{symbol.Name}}
                      """);
        }

        PrintContainingSymbols(typeSymbol, true);


        sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                    protected {{typeSymbol.Name}} ({{string.Join(", ", orderedTargets.Select(t => $"{t.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {t.Expression}").Prepend("uint index"))}}) : base({{string.Join(", ", orderedTargets.Where(t => !t.IsSelf).Select(t => t.Expression).Prepend("index"))}})
                    {
                """);

        foreach (var item in orderedTargets.Where(t => t.IsSelf))
        {
            var expression = item.Expression;
            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                        this.{{expression}} = {{expression}};
                """);
        }

        sourceBuilder.AppendLine(
/*  lang=c# */"""    }""");

        if (!typeSymbol.IsAbstract && opcodeAttr != null)
        {
            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                    internal static {{typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} Read(ref global::WaaS.Models.ModuleReader reader, uint index)
                    {
                """);

            var prevIndex = -1;
            foreach (var (index, expression, type, isSelf, isSigned, minValue) in orderedTargets)
            {
                var skipped = index - prevIndex - 1;
                prevIndex = index;

                if (skipped > 0)
                    sourceBuilder.AppendLine(
                        /*  lang=c# */
                        $$"""
                                  reader.Read({{skipped}});
                          """);

                static string GetRhsExpression(ITypeSymbol type, bool isSigned)
                {
                    if (isSigned && type.SpecialType is SpecialType.System_UInt32)
                        return "reader.ReadUnalignedLeb128S32()";
                    if (isSigned && type.SpecialType is SpecialType.System_UInt64)
                        return "reader.ReadUnalignedLeb128S64()";
                    if (type.SpecialType is SpecialType.System_UInt32) return "reader.ReadUnalignedLeb128U32()";
                    if (type.SpecialType is SpecialType.System_UInt64) return "reader.ReadUnalignedLeb128U64()";
                    if (type.Matches("WaaS.Models.BlockType")) return "new global::WaaS.Models.BlockType(ref reader)";

                    return $"reader.ReadUnaligned<{type}>()";
                }

                if (type is INamedTypeSymbol { IsGenericType: true } named &&
                    named.ConstructUnboundGenericType().Matches("System.ReadOnlyMemory`1"))
                {
                    // read vector
                    var elementType = named.TypeArguments[0];
                    sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                        var num_{{expression}} = reader.ReadVectorSize();
                        var {{expression}} = new {{elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}[num_{{expression}}];
                        for(int i = 0; i < {{expression}}.Length; i++)
                        {
                            {{expression}}[i] = {{GetRhsExpression(elementType, isSigned)}};
                        }
                """);
                }
                else
                {
                    sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                        var {{expression}} = {{GetRhsExpression(type, isSigned)}};
                """);

                    if (minValue != null)
                        sourceBuilder.AppendLine(
                            /*  lang=c# */
                            $$"""
                                      if({{expression}} < {{minValue}}) throw new global::WaaS.Models.InvalidModuleException();
                              """);
                }
            }

            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                        return new {{typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}({{string.Join(", ", orderedTargets.Select(t => t.Expression).Prepend("index"))}});
                """);

            sourceBuilder.AppendLine(
/*  lang=c# */"""    }""");
        }

        PrintContainingSymbols(typeSymbol, false);

        sourceContext.AddSource($"{typeSymbol.ToFullMetadataName()}.g.cs",
            SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

    private readonly record struct InstructionTypeInfo
    {
        public readonly byte? opCode0;
        public readonly byte? opCode1;
        public readonly ITypeSymbol type;

        public InstructionTypeInfo(ITypeSymbol type)
        {
            this.type = type;

            var opCodeAttr = type.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Matches("WaaS.Models.OpCodeAttributes") ?? false);

            if (opCodeAttr is null) return;

            opCode0 = opCodeAttr.ConstructorArguments.Length > 0
                ? (byte?)opCodeAttr.ConstructorArguments[0].Value
                : null;

            opCode1 = opCodeAttr.ConstructorArguments.Length > 1
                ? (byte?)opCodeAttr.ConstructorArguments[1].Value
                : null;
        }
    }

    private readonly record struct OperandItem(
        int Index,
        string Expression,
        ITypeSymbol Type,
        bool IsSelf,
        bool IsSigned,
        ulong? MinValue = null)
    {
    }
}