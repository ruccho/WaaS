using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace WaaS.Generators;

[Generator(LanguageNames.CSharp)]
public class ComponentBindingFormatterGenerator : IIncrementalGenerator
{
    private const string BindingNamespace = "WaaS.ComponentModel.Binding";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        {
            var source = context.SyntaxProvider.ForAttributeWithMetadataName(
                $"{BindingNamespace}.ComponentRecordAttribute",
                static (node, ct) => true,
                (context, ct) => context);

            context.RegisterSourceOutput(source, EmitRecordFormatter);
        }
    }

    private void EmitRecordFormatter(SourceProductionContext source, GeneratorAttributeSyntaxContext context)
    {
        StringBuilder sourceBuilder = new();

        var symbol = context.TargetSymbol;

        if (symbol is not INamedTypeSymbol namedSymbol) return;

        var identifier = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .WithGenericsOptions(SymbolDisplayGenericsOptions.None));
        if (namedSymbol.Arity > 0) identifier += $"`{namedSymbol.Arity}";

        sourceBuilder.AppendLine(
/* lang=c#  */"""
              #nullable enable
              using System;
              """);

        // TODO: nested class check

        if (!namedSymbol.ContainingNamespace.IsGlobalNamespace)
            sourceBuilder.AppendLine(
                /* lang=c#  */
                $$"""
                  namespace {{namedSymbol.ContainingNamespace}}
                  {
                  """);

        var name = namedSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var fields = namedSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(field =>
            {
                // has attribute
                return field.GetAttributes().Select(attr => attr.AttributeClass).WhereNotNull().All(attr =>
                    !attr.Matches("WaaS.ComponentModel.Binding.ComponentFieldAttribute"));
            });

        // TODO: generics
        sourceBuilder.AppendLine(
/* lang=c#  */$$"""
                    partial {{namedSymbol.TypeKind switch {
                        TypeKind.Class => "class",
                        TypeKind.Struct => "struct"
                    }}} {{namedSymbol.Name}}
                    {
                        static {{namedSymbol.Name}}()
                        {
                            FormatterProvider.Register<{{name}}>(new __GeneratedFormatter());
                            StaticConstructor();
                        }
                        
                        static partial void StaticConstructor();
                
                        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                        private class __GeneratedFormatter : IFormatter<{{name}}>
                        {
                            public async global::System.Threading.Tasks.ValueTask<{{name}}> PullAsync(global::WaaS.ComponentModel.Binding.Pullable pullable)
                            {
                                var prelude = await pullable.PullRecordAsync();
                                pullable = prelude.BodyPullable;
                                
                                var result = new {{name}}();
                """);

        foreach (var fieldSymbol in fields)
            // TODO: pull directly for primitive types 
            sourceBuilder.AppendLine(
                /* lang=c#  */
                $$"""
                                  result.{{fieldSymbol.Name}} = await pullable.PullValueAsync<{{fieldSymbol.Type}}>();
                  """);

        sourceBuilder.AppendLine(
/* lang=c#  */"""
                              return result;
                          }
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
}