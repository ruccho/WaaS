#nullable enable

namespace Wukuro.Core
{
    // interface functions
    [global::WaaS.ComponentModel.Binding.ComponentInterface(@"wukuro:core/functions")]
    public partial interface IFunctions
    {
        [global::WaaS.ComponentModel.Binding.ComponentApi(@"log")]
        global::System.Threading.Tasks.ValueTask Log(string @message);

        [global::WaaS.ComponentModel.Binding.ComponentApi(@"delay")]
        global::System.Threading.Tasks.ValueTask Delay(float @duration, Wukuro.Core.ITypes.UpdateModeKind @updateMode);

        [global::WaaS.ComponentModel.Binding.ComponentApi(@"delay-frame")]
        global::System.Threading.Tasks.ValueTask DelayFrame(uint @numFrames);

        /// <summary>
        ///     Runs the action after the delay without waiting for it. The returned task completes after the action runs.
        /// </summary>
        [global::WaaS.ComponentModel.Binding.ComponentApi(@"schedule")]
        global::System.Threading.Tasks.ValueTask<global::WaaS.ComponentModel.Binding.Owned<Wukuro.Core.ITypes.ITaskResourceImpl>> Schedule(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.IActionResourceImpl> @action, float @delay, Wukuro.Core.ITypes.UpdateModeKind @updateMode);

        [global::WaaS.ComponentModel.Binding.ComponentApi(@"is-first-time")]
        global::System.Threading.Tasks.ValueTask<bool> IsFirstTime(global::WaaS.ComponentModel.Binding.Borrowed<Wukuro.Core.ITypes.IContextResourceImpl> @context);

    }
}
