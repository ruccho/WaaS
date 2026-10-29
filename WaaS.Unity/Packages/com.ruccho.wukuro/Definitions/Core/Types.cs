#nullable enable

namespace Wukuro.Core
{
    // interface types
    [global::WaaS.ComponentModel.Binding.ComponentInterface(@"wukuro:core/types")]
    public partial interface ITypes
    {
        [global::WaaS.ComponentModel.Binding.ComponentResource("context")]
        public partial interface IContextResourceImpl : global::WaaS.ComponentModel.Binding.IResourceImpl
        {
            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]context.get-argument")]
            global::System.Threading.Tasks.ValueTask<Wukuro.Core.ITypes.Argument?> GetArgument(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.IContextResourceImpl> @self, string @name);

            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]context.get-variable")]
            global::System.Threading.Tasks.ValueTask<global::WaaS.ComponentModel.Binding.Owned<Wukuro.Core.ITypes.IVariableResourceImpl>> GetVariable(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.IContextResourceImpl> @self, string @name);

        }

        [global::WaaS.ComponentModel.Binding.ComponentResource("variable")]
        public partial interface IVariableResourceImpl : global::WaaS.ComponentModel.Binding.IResourceImpl
        {
            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]variable.get")]
            global::System.Threading.Tasks.ValueTask<Wukuro.Core.ITypes.Argument?> Get(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.IVariableResourceImpl> @self);

            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]variable.set")]
            global::System.Threading.Tasks.ValueTask Set(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.IVariableResourceImpl> @self, Wukuro.Core.ITypes.Argument @value);

        }

        [global::WaaS.ComponentModel.Binding.ComponentResource("action")]
        public partial interface IActionResourceImpl : global::WaaS.ComponentModel.Binding.IResourceImpl
        {
            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]action.run")]
            global::System.Threading.Tasks.ValueTask Run(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.IActionResourceImpl> @self);

            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]action.clone")]
            global::System.Threading.Tasks.ValueTask<global::WaaS.ComponentModel.Binding.Owned<Wukuro.Core.ITypes.IActionResourceImpl>> Clone(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.IActionResourceImpl> @self);

        }

        /// <summary>
        ///     A process started by the host. Dropping it does not stop the process.
        /// </summary>
        [global::WaaS.ComponentModel.Binding.ComponentResource("task")]
        public partial interface ITaskResourceImpl : global::WaaS.ComponentModel.Binding.IResourceImpl
        {
            /// <summary>
            ///     Waits until the process completes. Throws the exception of the process if it failed.
            /// </summary>
            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]task.wait")]
            global::System.Threading.Tasks.ValueTask Wait(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.ITaskResourceImpl> @self);

            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]task.is-completed")]
            global::System.Threading.Tasks.ValueTask<bool> IsCompleted(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.ITaskResourceImpl> @self);

        }

        [global::WaaS.ComponentModel.Binding.ComponentResource("object")]
        public partial interface IObjectResourceImpl : global::WaaS.ComponentModel.Binding.IResourceImpl
        {
            [global::WaaS.ComponentModel.Binding.ComponentApi(@"[method]object.clone")]
            global::System.Threading.Tasks.ValueTask<global::WaaS.ComponentModel.Binding.Owned<Wukuro.Core.ITypes.IObjectResourceImpl>> Clone(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.IObjectResourceImpl> @self);

        }

        [global::WaaS.ComponentModel.Binding.ComponentVariant, global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]
        public readonly partial struct Argument
        {
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public bool? TyBool { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public sbyte? TyS8 { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public short? TyS16 { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public int? TyS32 { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public long? TyS64 { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public byte? TyU8 { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public ushort? TyU16 { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public uint? TyU32 { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public ulong? TyU64 { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public float? TyF32 { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public double? TyF64 { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public global::WaaS.ComponentModel.Binding.ComponentChar? TyChar { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public string? TyString { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public global::WaaS.ComponentModel.Binding.Owned<Wukuro.Core.ITypes.IActionResourceImpl>? TyAction { get; init; }
            [global::WaaS.ComponentModel.Binding.ComponentCaseAttribute]
            public global::WaaS.ComponentModel.Binding.Owned<Wukuro.Core.ITypes.IObjectResourceImpl>? TyObject { get; init; }
        }

        public enum UpdateModeKind : uint
        {
            Unscaled,
            Scaled,
            Realtime,
        }

    }
}
