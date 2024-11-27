#nullable enable

using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel
{
    internal static class Utils
    {
        public static uint ElementSizeAlignTo(uint size, byte alignmentRank)
        {
            var exceeds = (size & ((1 << alignmentRank) - 1)) != 0;
            size >>= alignmentRank;
            if (exceeds) size++;
            return size << alignmentRank;
        }

        public static ValuePusher Wrap(this IValuePusherCore core)
        {
            return new ValuePusher(core);
        }
    }
}