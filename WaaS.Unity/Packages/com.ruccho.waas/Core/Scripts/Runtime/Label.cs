using System;
using System.Runtime.InteropServices;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a label on the stack.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Label : IEquatable<Label>
    {
        public uint BlockInstructionIndex { get; }
        public uint ContinuationIndex { get; }

        public Label(uint blockInstructionIndex, uint continuationIndex)
        {
            BlockInstructionIndex = blockInstructionIndex;
            ContinuationIndex = continuationIndex;
        }

        public bool Equals(Label other)
        {
            return BlockInstructionIndex == other.BlockInstructionIndex && ContinuationIndex == other.ContinuationIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is Label other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BlockInstructionIndex, ContinuationIndex);
        }

        public static bool operator ==(Label left, Label right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Label left, Label right)
        {
            return !left.Equals(right);
        }
    }
}