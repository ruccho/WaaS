using System;
using System.Threading.Tasks;
using UnityEngine;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;
using Wukuro.Dialog;

namespace Wukuro.Dialog
{
    public class Types : ITypes
    {
        internal static DialogModuleImpl DialogModuleType { get; } = new();

        public ITypes.IDialogModuleResourceImpl DialogModule => DialogModuleType;
    }

    public static class DialogImports
    {
        /// <summary>
        ///     Registers the import providers of <c>wukuro:dialog</c> to <see cref="WukuroRunner" />.
        /// </summary>
        public static void Register()
        {
            var types = ITypes.CreateWaaSInstance(new Types());
            var functions = IFunctions.CreateWaaSInstance(new Functions(WukuroRunner.ContextResourceType));
            WukuroRunner.RegisterImportProvider(
                new DelegateImportProvider<ISortedExportable>("wukuro:dialog/types", () => types));
            WukuroRunner.RegisterImportProvider(
                new DelegateImportProvider<ISortedExportable>("wukuro:dialog/functions", () => functions));
        }
    }

    public interface IDialogModule : IWukuroModule
    {
        public ValueTask OpenDialog(Option<string> provider);

        public ValueTask CloseDialog();

        public ValueTask ShowMessage(ITypes.MessageInfo message);

        public ValueTask<int> ShowOptions(ReadOnlyMemory<ITypes.Text> options);

        public ValueTask FadeIn(float duration);

        public ValueTask FadeOut(float duration);
    }

    internal class DialogModuleImpl : HostResourceTypeBase<IDialogModule>, ITypes.IDialogModuleResourceImpl
    {
        public IResourceType Type => this;

        public ValueTask OpenDialog(Borrowed<ITypes.IDialogModuleResourceImpl> self, Option<string> provider)
        {
            return Unwrap(self).OpenDialog(provider);
        }

        public ValueTask CloseDialog(Borrowed<ITypes.IDialogModuleResourceImpl> self)
        {
            return Unwrap(self).CloseDialog();
        }

        public ValueTask ShowMessage(Borrowed<ITypes.IDialogModuleResourceImpl> self, ITypes.MessageInfo message)
        {
            return Unwrap(self).ShowMessage(message);
        }

        public ValueTask<int> ShowOptions(Borrowed<ITypes.IDialogModuleResourceImpl> self,
            ReadOnlyMemory<ITypes.Text> options)
        {
            return Unwrap(self).ShowOptions(options);
        }

        public ValueTask FadeIn(Borrowed<ITypes.IDialogModuleResourceImpl> self, float duration)
        {
            return Unwrap(self).FadeIn(duration);
        }

        public ValueTask FadeOut(Borrowed<ITypes.IDialogModuleResourceImpl> self, float duration)
        {
            return Unwrap(self).FadeOut(duration);
        }
    }
}