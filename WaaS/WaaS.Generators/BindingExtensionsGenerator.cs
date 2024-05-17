using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace WaaS.Generators;

[Generator(LanguageNames.CSharp)]
public class BindingExtensionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var source = context.SyntaxProvider
            .CreateSyntaxProvider(
                (n, ct) => n is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name: { Identifier: { Text: "Invoke" or "ToExternalFunction" } }
                    }
                },
                (ctx, ct) => ctx.SemanticModel.GetOperation(ctx.Node))
            .Select((o, ct) => o as IInvocationOperation)
            .Where(o =>
            {
                if (o is null) return false;
                return o.TargetMethod.ContainingType.Matches("WaaS.Runtime.Bindings.BinderExtensions");
            })
            .Select((o, ct) => o!);

        context.RegisterSourceOutput(source.Collect(), Emit);
    }

    private void Emit(SourceProductionContext sourceContext, ImmutableArray<IInvocationOperation> operations)
    {
        var sourceBuilder = new StringBuilder();

        sourceBuilder.AppendLine(
/*  lang=c# */"""
              namespace WaaS.Runtime.Bindings
              {
                  internal static partial class BinderExtensionsLocal
                  {
              """);

        foreach (var argumentTypes in operations.Where(o => o.TargetMethod.MetadataName is "Invoke``1")
                     .Select(o => o.Arguments.Select(a => a.Type ?? throw new InvalidOperationException()))
                     .Distinct(TypeListEqualityComparer.Instance))
            EmitInvokeOverload(sourceBuilder, argumentTypes);

        foreach (var delegateType in operations.Where(o => o.TargetMethod.MetadataName is "ToExternalFunction")
                     .Where(o => o.Arguments.Length == 2)
                     .Select(o =>
                     {
                         var cursor = o.Arguments[1].Value;
                         while (cursor is IConversionOperation { IsImplicit: true } conversion)
                             cursor = conversion.Operand;

                         var type = cursor.Type;

                         if (type is INamedTypeSymbol { IsGenericType: true } generic)
                             type = generic.ConstructedFrom;

                         return type;
                     })
                     .Distinct(SymbolEqualityComparer.Default)
                     .OfType<INamedTypeSymbol>()
                     .Where(t => t.TypeKind == TypeKind.Delegate))
            EmitToExternalFunctionOverload(sourceBuilder, delegateType);

        sourceBuilder.AppendLine(
/*  lang=c# */"""
                  }
              }
              """);

        //if (!any) return;

        sourceContext.AddSource("BindingExtensions.g.cs",
            SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

    private void EmitToExternalFunctionOverload(StringBuilder sourceBuilder, INamedTypeSymbol delegateBaseType)
    {
        if (delegateBaseType is not { TypeKind: TypeKind.Delegate }) return;

        var typeParams = delegateBaseType.TypeParameters;

        var invokeMethod = delegateBaseType.DelegateInvokeMethod ?? throw new InvalidOperationException();

        var parameters = invokeMethod.Parameters;

        sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                        public static ExternalFunction ToExternalFunction{{(typeParams.Any() ? $"<{string.Join(", ", typeParams.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>" : "")}}(this Binder binder, {{delegateBaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} @delegate) {{string.Join(" ", typeParams.Select(p => p.ToTypeParameterConstraintsClause()))}}
                        {
                            ValueType[] resultTypes = default;
                """);
        if (invokeMethod.ReturnsVoid)
            sourceBuilder.AppendLine(
                /*  lang=c# */
                """            resultTypes = Array.Empty<ValueType>();""");
        else
            sourceBuilder.AppendLine(
                /*  lang=c# */
                $$"""
                              {
                                  using var marshalContext = binder.GetMarshalContext();
                          
                                  var allocated = false;
                                  do
                                  {
                                      MarshalStack<ValueType> marshalStack = default;
                                      switch (marshalContext.MoveNext())
                                      {
                                          case MarshallerActionKind.End:
                                          {
                                              if (!marshalStack.End) throw new InvalidOperationException();
                                              goto End;
                                          }
                                          case MarshallerActionKind.Iterate:
                                          {
                                              marshalStack = new MarshalStack<ValueType>(resultTypes);
                                              marshalContext.IterateValueType<{{invokeMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>(ref marshalStack);
                          
                                              break;
                                          }
                                          case MarshallerActionKind.Allocate:
                                          {
                                              if (allocated) throw new InvalidOperationException();
                                              allocated = true;
                                              resultTypes = new ValueType[marshalContext.AllocateLength];
                                              break;
                                          }
                                          default:
                                              throw new ArgumentOutOfRangeException();
                                      }
                                  } while (true);
                          
                                  End: ;
                              }
                  """);


        sourceBuilder.AppendLine(
/*  lang=c# */"""
                          ValueType[] parameterTypes = default;
                          {
                              using var unmarshalContext = binder.GetUnmarshalContext();
                      
                              var allocated = false;
                              do
                              {
                                  MarshalStack<ValueType> marshalStack = default;
                                  switch (unmarshalContext.MoveNext())
                                  {
                                      case MarshallerActionKind.End:
                                      {
                                          if (!marshalStack.End) throw new InvalidOperationException();
                                          goto End;
                                      }
                                      case MarshallerActionKind.Iterate:
                                      {
                                          marshalStack = new MarshalStack<ValueType>(parameterTypes);
              """);

        foreach (var parameter in parameters)
            sourceBuilder.AppendLine(
                /*  lang=c# */
                $$"""
                                              unmarshalContext.IterateValueType<{{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>(ref marshalStack);
                  """);

        sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                                            break;
                                        }
                                        case MarshallerActionKind.Allocate:
                                        {
                                            if (allocated) throw new InvalidOperationException();
                                            allocated = true;
                                            parameterTypes = new ValueType[unmarshalContext.AllocateLength];
                                            break;
                                        }
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }
                                } while (true);
                        
                                End: ;
                            }
                        
                            var type = new global::WaaS.Models.FunctionType(parameterTypes, resultTypes);
                        
                            return new ExternalFunctionDelegate((state, parameters, result) => Invoke(state, parameters, result), new Tuple<Binder, {{delegateBaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>(binder, @delegate), type);
                        
                        
                            static void Invoke(object state, ReadOnlySpan<StackValueItem> parameters, Span<StackValueItem> result)
                            {
                                if (state is not Tuple<Binder, {{delegateBaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}> tuple) throw new InvalidOperationException();
                                var (binder, @delegate) = tuple;
                """);

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                                {{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} _{{i}} = default;
                """);
        }

        sourceBuilder.AppendLine(
/*  lang=c# */"""
                      
                              {
                                  using var unmarshalContext = binder.GetUnmarshalContext();
                      
                                  do
                                  {
                                      UnmarshalQueue<StackValueItem> unmarshalQueue = default;
                                      switch (unmarshalContext.MoveNext())
                                      {
                                          case MarshallerActionKind.End:
                                          {
                                              if (!unmarshalQueue.End) throw new InvalidOperationException();
                                              goto End;
                                          }
                                          case MarshallerActionKind.Iterate:
                                          {
                                              unmarshalQueue = new UnmarshalQueue<StackValueItem>(parameters);
              """);

        for (var i = 0; i < parameters.Length; i++)
            sourceBuilder.AppendLine(
                /*  lang=c# */
                $$"""
                                                  unmarshalContext.IterateValue(out _{{i}}, ref unmarshalQueue);
                  """);

        sourceBuilder.AppendLine(
/*  lang=c# */"""
                                              break;
                                          }
                                          case MarshallerActionKind.Allocate:
                                          {
                                              break;
                                          }
                                          default:
                                              throw new ArgumentOutOfRangeException();
                                      }
                                  } while (true);
                      
                                  End: ;
                              }
                      
              """);

        if (invokeMethod.ReturnsVoid)
            sourceBuilder.AppendLine(
                /*  lang=c# */
                $$"""
                                  @delegate.Invoke({{string.Join(", ", parameters.Select((p, i) => $"_{i}"))}});
                                  if (result.Length != 0)
                                  {
                                      throw new InvalidOperationException();
                                  }
                  """);
        else
            sourceBuilder.AppendLine(
                /*  lang=c# */
                $$"""
                                  var cliResult = @delegate.Invoke({{string.Join(", ", parameters.Select((p, i) => $"_{i}"))}});
                                  {
                                      using var marshalContext = binder.GetMarshalContext();
                          
                                      do
                                      {
                                          MarshalStack<StackValueItem> marshalStack = default;
                                          switch (marshalContext.MoveNext())
                                          {
                                              case MarshallerActionKind.End:
                                              {
                                                  if (!marshalStack.End) throw new InvalidOperationException();
                                                  goto End;
                                              }
                                              case MarshallerActionKind.Iterate:
                                              {
                                                  marshalStack = new MarshalStack<StackValueItem>(result);
                                                  marshalContext.IterateValue(cliResult, ref marshalStack);
                                                  break;
                                              }
                                              case MarshallerActionKind.Allocate:
                                              {
                                                  break;
                                              }
                                              default:
                                                  throw new ArgumentOutOfRangeException();
                                          }
                                      } while (true);
                          
                                      End: ;
                                  }
                  """);


        sourceBuilder.AppendLine(
/*  lang=c# */"""
                          }
                      }
              """);
    }

    private void EmitInvokeOverload(StringBuilder sourceBuilder, IEnumerable<ITypeSymbol> argumentTypes)
    {
        sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                        public static unsafe TResult Invoke<TResult>({{string.Join(", ", argumentTypes.Select((t, i) => $"{t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} _{i}"))}})
                        {
                            {
                                var marshalContext = binder.GetMarshalContext();
                                var disposed = false;
                                try
                                {
                                    var allocated = false;
                        
                                    Start:
                                    Span<StackValueItem> parameterValues =
                                        stackalloc StackValueItem[allocated ? marshalContext.AllocateLength : 0];
                                    var marshalStack = new MarshalStack<StackValueItem>(parameterValues);
                        
                                    do
                                    {
                                        switch (marshalContext.MoveNext())
                                        {
                                            case MarshallerActionKind.End:
                                            {
                                                if (!marshalStack.End) throw new InvalidOperationException();
                                                goto End;
                                            }
                                            case MarshallerActionKind.Iterate:
                                            {
                                                foreach (var parameter in parameters)
                """);

        sourceBuilder.AppendLine(
/*  lang=c# */"""
                                                  marshalContext.IterateValueBoxed(parameter, ref marshalStack);
                      
                                              break;
                                          }
                                          case MarshallerActionKind.Allocate:
                                          {
                                              if (allocated) throw new InvalidOperationException();
                                              allocated = true;
                                              goto Start;
                                          }
                                          default:
                                              throw new ArgumentOutOfRangeException();
                                      }
                                  } while (true);
                      
                                  End:
                      
                                  disposed = true;
                                  marshalContext.Dispose();
                      
                                  context.Invoke(function, parameterValues);
                              }
                              finally
                              {
                                  if (!disposed) marshalContext.Dispose();
                              }
                          }
                      
                      
                          Span<StackValueItem> resultValues = stackalloc StackValueItem[context.ResultLength];
                          context.TakeResults(resultValues);
                      
                          var unmarshalQueue = new UnmarshalQueue<StackValueItem>(resultValues);
                      
                          TResult result = default;
                          {
                              using var unmarshalContext = binder.GetUnmarshalContext();
                      
                              do
                              {
                                  switch (unmarshalContext.MoveNext())
                                  {
                                      case MarshallerActionKind.End:
                                      {
                                          if (!unmarshalQueue.End) throw new InvalidOperationException();
                                          goto End;
                                      }
                                      case MarshallerActionKind.Iterate:
                                      {
                                          unmarshalContext.IterateValue(out result, ref unmarshalQueue);
                      
                                          break;
                                      }
                                      case MarshallerActionKind.Allocate:
                                      {
                                          break;
                                      }
                                      default:
                                          throw new ArgumentOutOfRangeException();
                                  }
                              } while (true);
                      
                              End: ;
                          }
                      
                          return result;
                      }
              """);
    }

    private class TypeListEqualityComparer : IEqualityComparer<IEnumerable<ITypeSymbol?>>
    {
        public static readonly TypeListEqualityComparer Instance = new();

        public bool Equals(IEnumerable<ITypeSymbol?> x, IEnumerable<ITypeSymbol?> y)
        {
            return x.SequenceEqual(y, SymbolEqualityComparer.Default);
        }

        public int GetHashCode(IEnumerable<ITypeSymbol?> obj)
        {
            var code = 0;
            foreach (var typeSymbol in obj)
                code = unchecked(code * 314159 + SymbolEqualityComparer.Default.GetHashCode(typeSymbol));

            return code;
        }
    }
}