using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;
using Wukuro.Core;

namespace Wukuro
{
    internal class Functions : IFunctions
    {
        public static readonly IInstance Instance = IFunctions.CreateWaaSInstance(new Functions());

        public ValueTask Log(string message)
        {
            Debug.Log(message);
            return default;
        }

        public async ValueTask Delay(float duration, ITypes.UpdateModeKind updateMode)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(duration), updateMode switch
            {
                ITypes.UpdateModeKind.Unscaled => DelayType.UnscaledDeltaTime,
                ITypes.UpdateModeKind.Scaled => DelayType.DeltaTime,
                ITypes.UpdateModeKind.Realtime => DelayType.Realtime,
                _ => throw new ArgumentOutOfRangeException(nameof(updateMode), updateMode, null)
            });
        }

        public async ValueTask DelayFrame(uint numFrames)
        {
            await UniTask.DelayFrame(checked((int)numFrames));
        }

        public ValueTask<Owned<ITypes.ITaskResourceImpl>> Schedule(Borrowed<ITypes.IActionResourceImpl> action,
            float delay, ITypes.UpdateModeKind updateMode)
        {
            var callback = ActionImpl.Instance.Unwrap(action.handle);
            return new ValueTask<Owned<ITypes.ITaskResourceImpl>>(WukuroTasks.Wrap(RunAfterDelay()));

            async UniTask RunAfterDelay()
            {
                await Delay(delay, updateMode);
                callback();
            }
        }

        public ITypes.IContextResourceImpl Context => ContextImpl.Instance;
        public ITypes.IActionResourceImpl Action => ActionImpl.Instance;
        public ITypes.ITaskResourceImpl Task => WukuroTasks.ResourceType;
    }
}