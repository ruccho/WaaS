using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;
using Wukuro.Core;

namespace Wukuro
{
    /// <summary>
    ///     Creates <c>wukuro:core/types.task</c> resources, which let scripts start a process without waiting for it
    ///     and wait for it later.
    /// </summary>
    public static class WukuroTasks
    {
        public static ITypes.ITaskResourceImpl ResourceType => TaskImpl.Instance;

        /// <summary>
        ///     Starts tracking the task and returns a resource to pass to the script.
        /// </summary>
        public static Owned<ITypes.ITaskResourceImpl> Wrap(UniTask task)
        {
            // a preserved task can be awaited more than once
            return (Owned<ITypes.ITaskResourceImpl>)TaskImpl.Instance.Wrap(task.Preserve());
        }

        private class TaskImpl : HostResourceTypeBase<UniTask>, ITypes.ITaskResourceImpl
        {
            public static new readonly TaskImpl Instance = new();
            public IResourceType Type => this;

            public ValueTask Wait(Borrowed<ITypes.ITaskResourceImpl> self)
            {
                return Unwrap(self).AsValueTask();
            }

            public ValueTask<bool> IsCompleted(Borrowed<ITypes.ITaskResourceImpl> self)
            {
                return new ValueTask<bool>(Unwrap(self).Status != UniTaskStatus.Pending);
            }

            protected override void OnDrop(UniTask value)
            {
                // the process continues; report failures that nobody waits for
                value.Forget(UnityEngine.Debug.LogException);
            }
        }
    }
}
