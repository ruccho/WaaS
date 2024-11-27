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
            if (popped == type) return popped;
            throw new InvalidCodeException();
        }

        public ValueType PopAny()
        {
            if (isUnreachable && Depth == 0) return 0; // any

            return stack[--Depth];
        }

        internal void ValidateResults(in ValidationContext context, BlockResultType blockResultType, bool exact)
        {
            var arity = blockResultType.GetArity(context);

            if (isUnreachable)
            {
                if (exact && Depth > arity) throw new InvalidCodeException();
            }
            else
            {
                if (!exact && Depth < arity) throw new InvalidCodeException();
                if (exact && Depth != arity) throw new InvalidCodeException();
            }

            for (var i = 0; i < arity; i++)
            {
                var stackIndex = Depth - (int)arity + i;
                if (isUnreachable && stackIndex < 0) continue;
                var type = stack[stackIndex];
                var resultType = blockResultType.GetElement(context, i);
                if ((byte)type == 0 || type == resultType) continue;
                throw new InvalidCodeException();
            }
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