using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;
using WaaS.Runtime;
using WaaS.Unity;
using Wukuro.Core;

namespace Wukuro
{
    public class WukuroRunner
    {
        private static readonly Dictionary<string, IImportProvider<ISortedExportable>> ImportProviders = new();

        /// <summary>
        ///     The resource type of <c>wukuro:core/types.context</c>. Modules use it to resolve <see cref="IContext" />.
        /// </summary>
        public static ITypes.IContextResourceImpl ContextResourceType => ContextImpl.Instance;

        public static bool TryRegisterImportProvider(IImportProvider<ISortedExportable> provider)
        {
            return ImportProviders.TryAdd(provider.Name, provider);
        }

        /// <summary>
        ///     Registers an import provider, replacing the provider registered with the same name.
        /// </summary>
        public static void RegisterImportProvider(IImportProvider<ISortedExportable> provider)
        {
            ImportProviders[provider.Name] = provider;
        }

        public static bool TryGetImportProvider(string name, out IImportProvider<ISortedExportable> provider)
        {
            return ImportProviders.TryGetValue(name, out provider);
        }

        static WukuroRunner()
        {
            // Register default import providers
            TryRegisterImportProvider(
                new DelegateImportProvider<ISortedExportable>("wukuro:core/types", () => Types.Instance));
            TryRegisterImportProvider(
                new DelegateImportProvider<ISortedExportable>("wukuro:core/functions", () => Functions.Instance));
        }

        private readonly List<IWukuroModule> _modules = new();

        public void AddModule(IWukuroModule module)
        {
            _modules.Add(module);
        }

        public UniTask RunAsync(WukuroEntryReference entry, IVariables variables,
            bool loadComponentAsync = true)
        {
            return RunAsync(entry, variables, null, loadComponentAsync);
        }

        /// <summary>
        ///     Runs the entry point with the arguments of <paramref name="entry" />. <paramref name="runtimeArguments" />
        ///     take precedence over them if given.
        /// </summary>
        public async UniTask RunAsync(WukuroEntryReference entry, IVariables variables, IArguments runtimeArguments,
            bool loadComponentAsync = true)
        {
            if (!entry.IsValid) throw new ArgumentException("The entry point is not specified.", nameof(entry));

            var componentAsset = entry.Script;
            var component = loadComponentAsync
                ? await componentAsset.LoadComponentAsync()
                // ReSharper disable once MethodHasAsyncOverload
                : componentAsset.LoadComponent();
            var arguments = runtimeArguments != null
                ? new WukuroRuntimeArguments.Combined(runtimeArguments, entry.Arguments)
                : entry.Arguments;
            await RunAsync(component, entry.Entry, arguments, variables);
        }

        internal static IInstance Instantiate(IComponent component)
        {
            var imports = new Dictionary<string, ISortedExportable>();

            foreach (var sourceImport in component.Source.Imports)
            {
                var name = sourceImport.Name.Name;
                if (ImportProviders.TryGetValue(name, out var provider))
                {
                    imports[name] = provider.Provide();
                }
            }

            return component.Instantiate(imports);
        }

        /// <summary>
        ///     Runs an entry point of the script. The parameters of the entry point are given by <paramref name="arguments" />
        ///     matched by the parameter name, except the context.
        /// </summary>
        public async UniTask RunAsync(IComponent component, string entry, IArguments arguments, IVariables variables)
        {
            var instance = Instantiate(component);

            if (!instance.TryGetExport(WukuroEntryPoints.GetExportName(entry), out IInstance entryInstance) ||
                !entryInstance.TryGetExport(WukuroEntryPoints.FunctionName, out IFunction run))
                throw new ArgumentException($"The entry point \"{entry}\" is not found in the script.",
                    nameof(entry));

            using var executionContext = new ExecutionContext();

            var context = new Context(arguments, variables, _modules);
            var ownedContext = ContextImpl.Instance.Wrap(context);

            // resources passed as borrowed arguments, released after the call
            var ownedArguments = new List<Owned>();

            try
            {
                using var binder = run.GetBinder(executionContext);
                PushArguments(binder.ArgumentPusher, run.Type, context, ownedContext, ownedArguments);

                await binder.InvokeAsync(executionContext);
            }
            finally
            {
                foreach (var owned in ownedArguments) owned.Dispose();
                ownedContext.Dispose();

                // resources the script did not drop (e.g. a MoonBit script, which has no destructors) are dropped
                // when the run ends, as they are when they go out of scope in a Rust script
                instance.DropOwnedResources();
            }
        }

        private static void PushArguments(ValuePusher pusher, IFunctionType type, IContext context,
            Owned ownedContext, List<Owned> ownedArguments)
        {
            foreach (var parameter in type.Parameters.Span)
                PushArgument(pusher, parameter, context, ownedContext, ownedArguments);
        }

        private static void PushArgument(ValuePusher pusher, IParameter parameter, IContext context,
            Owned ownedContext, List<Owned> ownedArguments)
        {
            var type = parameter.Type.Despecialize();

            if (type is IBorrowedType { Type: var resourceType } && resourceType == ContextImpl.Instance)
            {
                pusher.PushBorrowed(ownedContext.Borrow());
                return;
            }

            if (context.GetArgument(parameter.Label) is not { } argument)
                throw new ArgumentException($"The argument \"{parameter.Label}\" is not set.");

            switch (type)
            {
                case IBorrowedType { Type: var borrowedType } when borrowedType == ActionImpl.Instance &&
                                                                  argument.TyAction is { } action:
                    ownedArguments.Add(action.handle);
                    pusher.PushBorrowed(action.handle.Borrow());
                    return;
                case IBorrowedType { Type: var borrowedType } when borrowedType == ObjectImpl.Instance &&
                                                                  argument.TyObject is { } obj:
                    ownedArguments.Add(obj.handle);
                    pusher.PushBorrowed(obj.handle.Borrow());
                    return;
                case IPrimitiveValueType primitive:
                    PushPrimitive(pusher, primitive.Kind, argument, parameter.Label);
                    return;
            }

            throw new ArgumentException(
                $"The argument \"{parameter.Label}\" ({argument.Case}) does not match the parameter type.");
        }

        private static void PushPrimitive(ValuePusher pusher, PrimitiveValueTypeKind kind, ITypes.Argument argument,
            string label)
        {
            switch (kind)
            {
                case PrimitiveValueTypeKind.Bool when argument.TyBool is { } value:
                    pusher.Push(value);
                    return;
                case PrimitiveValueTypeKind.S8 when argument.TyS8 is { } value:
                    pusher.Push(value);
                    return;
                case PrimitiveValueTypeKind.S16 when argument.TyS16 is { } value:
                    pusher.Push(value);
                    return;
                case PrimitiveValueTypeKind.S32 when argument.TyS32 is { } value:
                    pusher.Push(value);
                    return;
                case PrimitiveValueTypeKind.S64 when argument.TyS64 is { } value:
                    pusher.Push(value);
                    return;
                case PrimitiveValueTypeKind.U8 when argument.TyU8 is { } value:
                    pusher.Push(value);
                    return;
                case PrimitiveValueTypeKind.U16 when argument.TyU16 is { } value:
                    pusher.Push(value);
                    return;
                case PrimitiveValueTypeKind.U32 when argument.TyU32 is { } value:
                    pusher.Push(value);
                    return;
                case PrimitiveValueTypeKind.U64 when argument.TyU64 is { } value:
                    pusher.Push(value);
                    return;
                case PrimitiveValueTypeKind.F32 when argument.TyF32 is { } value:
                    pusher.Push(value);
                    return;
                case PrimitiveValueTypeKind.F64 when argument.TyF64 is { } value:
                    pusher.Push(value);
                    return;
                case PrimitiveValueTypeKind.Char when argument.TyChar is { } value:
                    pusher.PushChar32(value.value);
                    return;
                case PrimitiveValueTypeKind.String when argument.TyString is { } value:
                    pusher.Push(value.AsSpan());
                    return;
            }

            throw new ArgumentException(
                $"The argument \"{label}\" ({argument.Case}) does not match the parameter type ({kind}).");
        }
    }
}
