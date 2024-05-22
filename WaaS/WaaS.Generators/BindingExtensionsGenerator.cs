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
    private static readonly SymbolDisplayFormat FullQualifiedButNoTypeParamsFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var source = context.SyntaxProvider
            .CreateSyntaxProvider(
                (n, ct) => n is InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name:
                        {
                            Identifier:
                            {
                                Text: "Invoke" or "ToExternalFunction" or "InvokeAsync" or "ToAsyncExternalFunction"
                            }
                        }
                    }
                },
                (ctx, ct) => ctx.SemanticModel.GetOperation(ctx.Node))
            .Select((o, ct) => o as IInvocationOperation)
            .Where(o =>
            {
                if (o is null) return false;
                return o.TargetMethod.ContainingType.Matches("WaaS.Runtime.Bindings.BindingExtensions");
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
                  internal static partial class BindingExtensionsLocal
                  {
              """);

        foreach (var argumentTypes in operations.Where(o => o.TargetMethod.MetadataName is "Invoke")
                     .Where(o => o.Arguments.Length == 4 && o.Arguments[3] is
                         { ArgumentKind: ArgumentKind.ParamArray })
                     .Select(o => o.Arguments[3])
                     .Where(o => o is { ArgumentKind: ArgumentKind.ParamArray })
                     .Select(o => o.Value)
                     .OfType<IArrayCreationOperation>()
                     .Select(o => o.Initializer)
                     .WhereNotNull()
                     .Select(o => o.ElementValues.Select(v =>
                     {
                         var cursor = v;
                         while (cursor is IConversionOperation { IsImplicit: true } conversion)
                             cursor = conversion.Operand;

                         var type = cursor.Type;
                         return type!;
                     }))
                     .Where(types => types.All(t => !t.HasTypeParameter())) // TODO: support type parameters
                     .Distinct(TypeListEqualityComparer.Instance)
                )
            EmitInvokeOverload(sourceBuilder, argumentTypes);

        foreach (var argumentTypes in operations
                     .Where(o => o.TargetMethod is
                         { MetadataName: "InvokeAsync", TypeParameters.Length: 1 }) // has returns
                     .Where(o => o.Arguments.Length == 4 && o.Arguments[3] is
                         { ArgumentKind: ArgumentKind.ParamArray })
                     .Select(o => o.Arguments[3])
                     .Where(o => o is { ArgumentKind: ArgumentKind.ParamArray })
                     .Select(o => o.Value)
                     .OfType<IArrayCreationOperation>()
                     .Select(o => o.Initializer)
                     .WhereNotNull()
                     .Select(o => o.ElementValues.Select(v =>
                     {
                         var cursor = v;
                         while (cursor is IConversionOperation { IsImplicit: true } conversion)
                             cursor = conversion.Operand;

                         var type = cursor.Type;
                         return type!;
                     }))
                     .Where(types => types.All(t => !t.HasTypeParameter())) // TODO: support type parameters
                     .Distinct(TypeListEqualityComparer.Instance)
                )
            EmitInvokeAsyncOverload(sourceBuilder, argumentTypes, true);

        foreach (var argumentTypes in operations
                     .Where(o => o.TargetMethod is
                         { MetadataName: "InvokeAsync", TypeParameters.Length: 0 }) // doesn't have returns
                     .Where(o => o.Arguments.Length == 4 && o.Arguments[3] is
                         { ArgumentKind: ArgumentKind.ParamArray })
                     .Select(o => o.Arguments[3])
                     .Where(o => o is { ArgumentKind: ArgumentKind.ParamArray })
                     .Select(o => o.Value)
                     .OfType<IArrayCreationOperation>()
                     .Select(o => o.Initializer)
                     .WhereNotNull()
                     .Select(o => o.ElementValues.Select(v =>
                     {
                         var cursor = v;
                         while (cursor is IConversionOperation { IsImplicit: true } conversion)
                             cursor = conversion.Operand;

                         var type = cursor.Type;
                         return type!;
                     }))
                     .Where(types => types.All(t => !t.HasTypeParameter())) // TODO: support type parameters
                     .Distinct(TypeListEqualityComparer.Instance)
                )
            EmitInvokeAsyncOverload(sourceBuilder, argumentTypes, false);

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

        foreach (var delegateType in operations.Where(o => o.TargetMethod.MetadataName is "ToAsyncExternalFunction")
                     .Where(o => o.Arguments.Length == 2)
                     .Select(o =>
                     {
                         var cursor = o.Arguments[1].Value;
                         while (cursor is IConversionOperation { IsImplicit: true } conversion)
                             cursor = conversion.Operand;

                         var type = cursor.Type;

                         if (type is INamedTypeSymbol { IsGenericType: true } generic)
                         {
                             var constructedFrom = generic.ConstructedFrom;

                             // prefix return type
                             var invokeMethod = constructedFrom.DelegateInvokeMethod;
                             var awaitableType = invokeMethod?.ReturnType;
                             if (awaitableType is ITypeParameterSymbol typeParam)
                             {
                                 // get parameter index
                                 var index = constructedFrom.TypeParameters.IndexOf(typeParam);
                                 var argument = generic.TypeArguments[index];

                                 if (argument is INamedTypeSymbol argumentGeneric)
                                     argument = argumentGeneric.ConstructedFrom;

                                 constructedFrom = constructedFrom.Construct(constructedFrom.TypeParameters.Select(a =>
                                     SymbolEqualityComparer.Default.Equals(a, typeParam) ? argument : a).ToArray());
                             }

                             type = constructedFrom;
                         }

                         return type;
                     })
                     .Distinct(SymbolEqualityComparer.Default)
                     .OfType<INamedTypeSymbol>()
                     .Where(t => t.TypeKind == TypeKind.Delegate))
            EmitToAsyncExternalFunctionOverload(sourceBuilder, delegateType);

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
/*  lang=c# */$$"""
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

    private void EmitToAsyncExternalFunctionOverload(StringBuilder sourceBuilder, INamedTypeSymbol delegateBaseType)
    {
        if (delegateBaseType is not { TypeKind: TypeKind.Delegate }) return;

        var typeParams = delegateBaseType.ExtractTypeParameters().ToArray();

        Dictionary<ITypeParameterSymbol, string> typeParamTable = new(SymbolEqualityComparer.Default);
        {
            var i = 0;
            foreach (var typeParam in typeParams) typeParamTable[typeParam] = $"T{i++}";
        }


        string ToDisplayString(ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol typeParam && typeParamTable.TryGetValue(typeParam, out var exp))
                return exp;
            var s = $"{type.ToDisplayString(FullQualifiedButNoTypeParamsFormat)}";
            if (type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
                s += $"<{string.Join(", ", namedTypeSymbol.TypeArguments.Select(ToDisplayString))}>";

            return s;
        }

        var invokeMethod = delegateBaseType.DelegateInvokeMethod ?? throw new InvalidOperationException();

        var awaitableType = invokeMethod.ReturnType;

        var getAwaiterMethod = awaitableType.GetMembers().OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.MetadataName == "GetAwaiter" && m.Parameters.Length == 0 && !m.IsStatic);

        if (getAwaiterMethod == null) return;

        var awaiterType = getAwaiterMethod.ReturnType;

        var getResultMethod = awaiterType.GetMembers().OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.MetadataName == "GetResult" && m.Parameters.Length == 0 && !m.IsStatic);

        if (getResultMethod == null) return;

        var resultType = getResultMethod.ReturnType;

        var parameters = invokeMethod.Parameters;

        sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                        public static AsyncExternalFunction ToAsyncExternalFunction{{(typeParams.Any() ? $"<{string.Join(", ", typeParams.Select(ToDisplayString))}>" : "")}}(this Binder binder, {{ToDisplayString(delegateBaseType)}} @delegate) {{string.Join(" ", typeParams.Select(p => p.ToTypeParameterConstraintsClause()))}}
                        {
                            ValueType[] resultTypes = default;
                """);
        if (resultType.SpecialType == SpecialType.System_Void)
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
                                              marshalContext.IterateValueType<{{ToDisplayString(resultType)}}>(ref marshalStack);
                          
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
                                              unmarshalContext.IterateValueType<{{ToDisplayString(parameter.Type)}}>(ref marshalStack);
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
                        
                            return new AsyncExternalFunctionDelegate((state, parameters, result) => InvokeAsync1(state, parameters, result), new Tuple<Binder, {{ToDisplayString(delegateBaseType)}}>(binder, @delegate),
                                type);
                            
                            static ValueTask InvokeAsync1(object state, ReadOnlySpan<StackValueItem> parameters,
                                Memory<StackValueItem> result)
                            {
                                if (state is not Tuple<Binder, {{ToDisplayString(delegateBaseType)}}> tuple) throw new InvalidOperationException();
                                var (binder, @delegate) = tuple;
                """);

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                                {{ToDisplayString(parameter.Type)}} _{{i}} = default;
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
/*  lang=c# */$$"""
                                                unmarshalContext.IterateValue(out _{{i}}, ref unmarshalQueue);
                """);

        sourceBuilder.AppendLine(
/*  lang=c# */$$"""
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
                                var resultTask = @delegate.Invoke({{string.Join(", ", parameters.Select((p, i) => $"_{i}"))}});
                                return InvokeAsync2(resultTask, binder, result);
                            }
                
                            static async ValueTask InvokeAsync2({{ToDisplayString(awaitableType)}} resultTask, Binder binder,
                                Memory<StackValueItem> result)
                """);
        if (resultType.SpecialType == SpecialType.System_Void)
            /*  lang=c# */
            sourceBuilder.AppendLine("""
                                                 {
                                                     await resultTask;
                                                     if (result.Length != 0)
                                                     {
                                                         throw new InvalidOperationException();
                                                     }
                                                 }
                                     """);
        else
            sourceBuilder.AppendLine(
                /*  lang=c# */
                $$"""
                              {
                                  InvokeAsync3(binder, result, await resultTask);
                              }
                  
                              static void InvokeAsync3(Binder binder, Memory<StackValueItem> result, {{ToDisplayString(resultType)}} cliResult)
                              {
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
                                                  marshalStack = new MarshalStack<StackValueItem>(result.Span);
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
                              }
                  """);

        sourceBuilder.AppendLine(
/*  lang=c# */"""        }""");
    }

    private void EmitInvokeOverload(StringBuilder sourceBuilder, IEnumerable<ITypeSymbol> argumentTypes)
    {
        sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                        public static TResult Invoke<TResult>(this Binder binder, ExecutionContext context, IInvocableFunction function{{(argumentTypes.Any() ? ", " : "")}}{{string.Join(", ", argumentTypes.Select((t, i) => $"{t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} _{i}"))}})
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
                """);

        var i = 0;
        foreach (var _ in argumentTypes)
            sourceBuilder.AppendLine(
                /*  lang=c# */
                $$"""
                                                  marshalContext.IterateValue(_{{i++}}, ref marshalStack);
                  """);

        sourceBuilder.AppendLine(
/*  lang=c# */"""
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

    private void EmitInvokeAsyncOverload(StringBuilder sourceBuilder, IEnumerable<ITypeSymbol> argumentTypes,
        bool withResult)
    {
        sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                        public static ValueTask{{(withResult ? "<TResult>" : "")}} InvokeAsync{{(withResult ? "<TResult>" : "")}}(this Binder binder, ExecutionContext context, IInvocableFunction function{{(argumentTypes.Any() ? ", " : "")}}{{string.Join(", ", argumentTypes.Select((t, i) => $"{t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} _{i}"))}})
                        {
                            ValueTask task;
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
                """);

        var i = 0;
        foreach (var _ in argumentTypes)
            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                                                  marshalContext.IterateValue(_{{i++}}, ref marshalStack);
                """);

        sourceBuilder.AppendLine(
/*  lang=c# */"""
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
              
                                task = context.InvokeAsync(function, parameterValues);
                            }
                            finally
                            {
                                if (!disposed) marshalContext.Dispose();
                            }
                        }
              """);

        if (!withResult)
        {
            sourceBuilder.AppendLine(
/*  lang=c# */"""
                          return task;
                      }
              """);
            return;
        }

        sourceBuilder.AppendLine(
/*  lang=c# */"""
              
                          return InvokeAsyncCore(binder, context, task);
                          
                          static async ValueTask<TResult> InvokeAsyncCore(Binder binder, ExecutionContext context, ValueTask task)
                          {
                              await task;
                              return GetResult(binder, context);
                          }
                          
                          static TResult GetResult(Binder binder, ExecutionContext context)
                          {
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