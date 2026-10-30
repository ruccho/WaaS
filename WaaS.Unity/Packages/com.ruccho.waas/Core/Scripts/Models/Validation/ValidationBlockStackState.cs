using System;
using WaaS.Runtime;

namespace WaaS.Models
{
    /// <summary>
    ///     Represents the state of the stack within a block during validation.
    /// </summary>
    public ref struct ValidationBlockStackState
    {
        private readonly Span<ValueType> stack;
        private bool isUnreachable;

        public ushort Depth { get; private set; }

        public ReadOnlySpan<ValueType> CurrentSlice => stack.Slice(0, Depth);

        public ValidationBlockStackState(Span<ValueType> stack)
        {
            this.stack = stack;
            Depth = 0;
            isUnreachable = false;
        }

        public ValueType Pop(ValueType type)
        {
            if (isUnreachable && Depth == 0) return 0; // any

            var popped = stack[--Depth];
            // a value pushed in an unreachable code may be of any type
            if (popped == type || (byte)popped == 0) return type;
            throw new InvalidCodeException();
        }

        /// <summary>
        ///     Pops a value of a reference type. Returns 0 if the type is unknown in an unreachable code.
        /// </summary>
        public ValueType PopReference()
        {
            if (isUnreachable && Depth == 0) return 0; // any

            var popped = stack[--Depth];
            if ((byte)popped == 0 || popped.IsReference()) return popped;
            throw new InvalidCodeException("type mismatch");
        }

        public ValueType PopAny()
        {
            if (isUnreachable && Depth == 0) return 0; // any

            return stack[--Depth];
        }

        public void ValidateResults(ReadOnlySpan<ValueType> resultTypes, bool exact)
        {
            if (isUnreachable)
            {
                if (exact && Depth > resultTypes.Length) throw new InvalidCodeException();
            }
            else
            {
                if (!exact && Depth < resultTypes.Length) throw new InvalidCodeException();
                if (exact && Depth != resultTypes.Length) throw new InvalidCodeException();
            }

            for (var i = 0; i < resultTypes.Length; i++)
            {
                var stackIndex = Depth - resultTypes.Length + i;
                if (isUnreachable && stackIndex < 0) continue;
                var type = stack[stackIndex];
                var resultType = resultTypes[i];
                if ((byte)type == 0 || type == resultType) continue;
                throw new InvalidCodeException();
            }
        }

        public void Push(ValueType type)
        {
            try
            {
                stack[Depth++] = type;
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new InvalidCodeException("validation failed", ex);
            }
        }

        public void MakeUnreachable()
        {
            isUnreachable = true;
            Depth = 0;
        }
    }
}