using System;
using Cysharp.Threading.Tasks;
using Wukuro.Core;

namespace Wukuro
{
    /// <summary>
    ///     Waits of the scripts (<c>delay</c>, <c>delay-frame</c> and <c>schedule</c> of <c>wukuro:core</c>).
    /// </summary>
    public interface IWukuroClock
    {
        UniTask Delay(float duration, ITypes.UpdateModeKind updateMode);
        UniTask DelayFrame(uint numFrames);
    }

    /// <summary>
    ///     The clock used by the scripts. Tests can replace it with <see cref="Immediate" /> to run the scripts without
    ///     waiting for the time.
    /// </summary>
    public static class WukuroClock
    {
        private static IWukuroClock current;

        /// <summary>
        ///     Waits for the time and the frames of Unity.
        /// </summary>
        public static IWukuroClock Default { get; } = new UnityClock();

        /// <summary>
        ///     Completes each wait on the next frame regardless of the duration. A script waiting in a loop still yields,
        ///     so that it does not block the editor.
        /// </summary>
        public static IWukuroClock Immediate { get; } = new ImmediateClock();

        /// <summary>
        ///     The clock used by the scripts. Setting null restores <see cref="Default" />.
        /// </summary>
        public static IWukuroClock Current
        {
            get => current ?? Default;
            set => current = value;
        }

        private class UnityClock : IWukuroClock
        {
            public UniTask Delay(float duration, ITypes.UpdateModeKind updateMode)
            {
                return UniTask.Delay(TimeSpan.FromSeconds(duration), updateMode switch
                {
                    ITypes.UpdateModeKind.Unscaled => DelayType.UnscaledDeltaTime,
                    ITypes.UpdateModeKind.Scaled => DelayType.DeltaTime,
                    ITypes.UpdateModeKind.Realtime => DelayType.Realtime,
                    _ => throw new ArgumentOutOfRangeException(nameof(updateMode), updateMode, null)
                });
            }

            public UniTask DelayFrame(uint numFrames)
            {
                return UniTask.DelayFrame(checked((int)numFrames));
            }
        }

        private class ImmediateClock : IWukuroClock
        {
            public UniTask Delay(float duration, ITypes.UpdateModeKind updateMode)
            {
                return UniTask.Yield().ToUniTask();
            }

            public UniTask DelayFrame(uint numFrames)
            {
                return UniTask.Yield().ToUniTask();
            }
        }
    }
}
