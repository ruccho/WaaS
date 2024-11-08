using System.Runtime.InteropServices;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a label on the stack.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Label
    {
        public uint BlockInstructionIndex { get; }
        public uint ContinuationIndex { get; }

        public Label(uint blockInstructionIndex, uint continuationIndex)
        {
            BlockInstructionIndex = blockInstructionIndex;
            ContinuationIndex = continuationIndex;
        }
    }
}