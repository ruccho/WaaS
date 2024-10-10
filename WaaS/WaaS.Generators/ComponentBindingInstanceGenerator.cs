using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace WaaS.Generators;

[Generator(LanguageNames.CSharp)]
public class ComponentBindingInstanceGenerator : IIncrementalGenerator
{
    private const string BindingNamespace = "WaaS.ComponentModel.Binding";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        {
            var source = context.SyntaxProvider.ForAttributeWithMetadataName(
                $"{BindingNamespace}.ComponentInterfaceAttribute",
                static (node, ct) => true,
                (context, ct) => context);

            context.RegisterSourceOutput(source, EmitInstance);
        }
    }

    private void EmitInstance(SourceProductionContext source, GeneratorAttributeSyntaxContext context)
    {
        StringBuilder sourceBuilder = new();

        var symbol = context.TargetSymbol;

        if (symbol is not INamedTypeSymbol namedSymbol) return;

        if (namedSymbol.TypeKind != TypeKind.Interface) return;

        if (namedSymbol.ContainingType != null)
        {
            source.ReportDiagnostic(Diagnostic.Create(Diagnostics.NestedClassDiagnostic,
                context.TargetNode.GetLocation()));
            return;
        }

        var identifier = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .WithGenericsOptions(SymbolDisplayGenericsOptions.None));
        if (namedSymbol.Arity > 0) identifier += $"`{namedSymbol.Arity}";

        sourceBuilder.AppendLine(
/* lang=c#  */"""#nullable enable""");

        if (!namedSymbol.ContainingNamespace.IsGlobalNamespace)
            sourceBuilder.AppendLine(
                /* lang=c#  */
                $$"""
                  namespace {{namedSymbol.ContainingNamespace}}
                  {
                  """);

        var name = namedSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var nameWithoutTypeParams =
            namedSymbol.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None));

        var members = namedSymbol.GetMembers()
            .Where(member =>
                member.DeclaredAccessibility == Accessibility.Public &&
                member.IsAbstract &&
                !member.IsStatic &&
                member is not (IMethodSymbol and ({ IsGenericMethod: true } or
                    { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet })));

        // TODO: generics
        sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                    partial interface {{namedSymbol.Name}}
                    {
                        private class Instance : global::WaaS.ComponentModel.Runtime.IInstance
                        {
                            private readonly {{name}} __waas__target;
                
                            public Instance({{name}} target)
                            {
                                this.__waas__target = target;
                            }
                
                            public bool TryGetExport<T>(string name, [global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? result) where T : global::WaaS.ComponentModel.Runtime.ISortedExportable
                            {
                                if (!typeof(global::WaaS.ComponentModel.Runtime.IFunction).IsAssignableFrom(typeof(T)))
                                {
                                    result = default;
                                    return false;
                                }
                
                                var function = name switch
                                {
                """);


        foreach (var member in members)
            sourceBuilder.AppendLine(
                /* lang=c#  */
                $$"""
                                      @"{{ToApiName(member)}}" => __{{member.Name}} ??= new {{member.Name}}(__waas__target),
                  """);

        sourceBuilder.AppendLine(
/* lang=c#  */"""
                                  _ => null
                              };
              
                              result = (T)function!;
                              return function != null;
                          }
              """);

        foreach (var member in members)
        {
            ITypeSymbol? returnType;
            IEnumerable<IParameterSymbol> parameterTypes;
            {
                if (member is IMethodSymbol method)
                {
                    returnType = method.ReturnsVoid ? null : method.ReturnType;
                    parameterTypes = method.Parameters;
                }
                else if (member is IPropertySymbol property)
                {
                    returnType = property.Type;
                    parameterTypes = Array.Empty<IParameterSymbol>();
                }
                else
                {
                    returnType = null;
                    parameterTypes = Array.Empty<IParameterSymbol>();
                }
            }

            sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                            private global::WaaS.ComponentModel.Runtime.IFunction? __{{member.Name}};
                
                            class {{member.Name}} : global::WaaS.ComponentModel.Binding.ExternalFunction
                            {
                                private readonly {{name}} __waas__target;
                            
                                public {{member.Name}}({{name}} target)
                                {
                                    this.__waas__target = target;
                                }
                            
                                public override global::WaaS.ComponentModel.Runtime.IFunctionType Type { get; } =
                                    new global::WaaS.ComponentModel.Models.ResolvedFunctionType(
                """);

            if (!parameterTypes.Any())
            {
                sourceBuilder.AppendLine(
/* lang=c#  */
                    """                        global::System.Array.Empty<global::WaaS.ComponentModel.Runtime.IParameter>(),""");
            }
            else
            {
                sourceBuilder.AppendLine(
/* lang=c#  */"""
                                      new global::WaaS.ComponentModel.Runtime.IParameter[]
                                      {
              """);
                foreach (var parameterType in parameterTypes)
                    sourceBuilder.AppendLine(
                        /* lang=c#  */
                        $$"""
                                                      new global::WaaS.ComponentModel.Models.ResolvedParameter(@"{{parameterType.Name}}", global::WaaS.ComponentModel.Binding.FormatterProvider.GetFormatter<{{parameterType.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>().Type),  
                          """);

                sourceBuilder.AppendLine(
/* lang=c#  */"""                        },""");
            }

            sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                                        {{(returnType != null ? $"global::WaaS.ComponentModel.Binding.FormatterProvider.GetFormatter<{returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>().Type" : "null")}});
                """);

            sourceBuilder.AppendLine(
/* lang=c#  */"""
                          
                              protected override async global::System.Threading.Tasks.ValueTask PullArgumentsAsync(
                                  global::WaaS.Runtime.ExecutionContext context,
                                  global::WaaS.ComponentModel.Binding.PushPullAdapter adapter,
                                  global::System.Threading.Tasks.ValueTask frameMove,
                                  global::System.Threading.Tasks.ValueTask<global::WaaS.ComponentModel.Runtime.ValuePusher> resultPusherTask)
                              {
              """);
            foreach (var parameterSymbol in parameterTypes)
                sourceBuilder.AppendLine(
                    /* lang=c#  */
                    $$"""
                                          var {{parameterSymbol.Name}} = await adapter.PullValueAsync<{{parameterSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>();
                      """);

            sourceBuilder.AppendLine(
/* lang=c#  */"""                    await frameMove;""");

            sourceBuilder.Append(
/* lang=c#  */"""                    """);

            if (returnType != null) sourceBuilder.Append("var __waas__result =");

            sourceBuilder.AppendLine(
                member switch
                {
                    IMethodSymbol method =>
                        $"__waas__target.{method.Name}({string.Join(", ", parameterTypes.Select(param => param.Name))});",
                    IPropertySymbol property => $"__waas__target.{property.Name};"
                });

            if (returnType != null)
                sourceBuilder.AppendLine(
                    /* lang=c#  */
                    $$"""
                                          var __waas__resultPusher = await resultPusherTask;
                                          global::WaaS.ComponentModel.Binding.FormatterProvider.GetFormatter<{{returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>().Push(__waas__result, __waas__resultPusher);
                      """);
            sourceBuilder.AppendLine(
/* lang=c#  */"""
                              }
                          }
              """);
        }

        sourceBuilder.AppendLine(
/* lang=c#  */"""
                      }
                  }
              """);


        if (!namedSymbol.ContainingNamespace.IsGlobalNamespace)
            sourceBuilder.AppendLine(
                /* lang=c#  */
                $$"""
                      } // namespace {{namedSymbol.ContainingNamespace}}
                  """);


        source.AddSource($"{identifier}.cs", sourceBuilder.ToString());
    }

    private static string ToApiName(ISymbol member)
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
}