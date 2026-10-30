#nullable enable

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using WaaS.ComponentModel.Binding;

namespace WaaS.ComponentModel.Runtime
{
    public static class ValueTransfer
    {
        private static void TransferString(ref ValueLifter lifter, ValuePusher pusher)
        {
            var info = lifter.PullStringInfoCore();

            if (pusher.Core is PushPullAdapter adapter && !pusher.IsDisposed)
            {
                // the string is pulled as a string in most cases
                adapter.PushString(lifter.PullString(info));
                return;
            }

            var maxLength = lifter.GetStringMaxCharCount(info);
            if (maxLength > 1024)
            {
                var array = ArrayPool<char>.Shared.Rent(maxLength);
                // for safety
                Array.Clear(array, 0, array.Length);

                try
                {
                    TransferStringCore(info, ref lifter, pusher, array);
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(array);
                }
            }
            else
            {
                Span<char> buffer = stackalloc char[maxLength];
                // suppress escape analysis
                var buffer1 = MemoryMarshal.CreateSpan(ref buffer[0], buffer.Length);
                TransferStringCore(info, ref lifter, pusher, buffer1);
            }

            static void TransferStringCore(in ValueLifter.StringInfo info, ref ValueLifter lifter, ValuePusher pusher,
                Span<char> buffer)
            {
                var length = lifter.GetString(info, buffer);
                pusher.Push(buffer.Slice(0, length));
            }
        }

        public static void TransferNext(ref ValueLifter lifter, ValuePusher pusher)
        {
            var next = lifter.GetNextType()?.Despecialize();
            if (next == null) return;
            TransferNext(ref lifter, pusher, next);
        }

        /// <summary>
        ///     Transfers the next value whose type is already known to be <paramref name="next" />.
        /// </summary>
        internal static void TransferNext(ref ValueLifter lifter, ValuePusher pusher, IDespecializedValueType next)
        {
            switch (next)
            {
                case IPrimitiveValueType primitiveValueType:
                {
                    switch (primitiveValueType.Kind)
                    {
                        case PrimitiveValueTypeKind.String:
                            TransferString(ref lifter, pusher);
                            break;
                        case PrimitiveValueTypeKind.Char:
                            pusher.PushChar32(lifter.PullCharCore());
                            break;
                        case PrimitiveValueTypeKind.F64:
                            pusher.Push(lifter.PullF64Core());
                            break;
                        case PrimitiveValueTypeKind.F32:
                            pusher.Push(lifter.PullF32Core());
                            break;
                        case PrimitiveValueTypeKind.U64:
                            pusher.Push(lifter.PullU64Core());
                            break;
                        case PrimitiveValueTypeKind.S64:
                            pusher.Push(lifter.PullS64Core());
                            break;
                        case PrimitiveValueTypeKind.U32:
                            pusher.Push(lifter.PullU32Core());
                            break;
                        case PrimitiveValueTypeKind.S32:
                            pusher.Push(lifter.PullS32Core());
                            break;
                        case PrimitiveValueTypeKind.U16:
                            pusher.Push(lifter.PullU16Core());
                            break;
                        case PrimitiveValueTypeKind.S16:
                            pusher.Push(lifter.PullS16Core());
                            break;
                        case PrimitiveValueTypeKind.U8:
                            pusher.Push(lifter.PullU8Core());
                            break;
                        case PrimitiveValueTypeKind.S8:
                            pusher.Push(lifter.PullS8Core());
                            break;
                        case PrimitiveValueTypeKind.Bool:
                            pusher.Push(lifter.PullBoolCore());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                }
                case IBorrowedType borrowedType:
                {
                    pusher.PushBorrowed(lifter.PullBorrowed());
                    break;
                }
                case IOwnedType ownedType:
                {
                    pusher.PushOwned(lifter.PullOwned());
                    break;
                }
                case IFlagsType flagsType:
                {
                    pusher.PushFlags(lifter.PullFlags());
                    break;
                }
                case IListType listType:
                {
                    var listLifter = lifter.PullList(out var length);
                    using var listPusher = pusher.PushList(checked((int)length));
                    for (var i = 0; i < length; i++) TransferNext(ref listLifter, listPusher);

                    break;
                }
                case IRecordType recordType:
                {
                    var recordLifter = lifter.PullRecord();
                    using var recordPusher = pusher.PushRecord();
                    for (var i = 0; i < recordType.Fields.Length; i++)
                        TransferNext(ref recordLifter, recordPusher);

                    break;
                }
                case IVariantType variantType:
                {
                    var recordLifter = lifter.PullVariant(out var caseIndex);
                    using var recordPusher = pusher.PushVariant(checked((int)caseIndex));
                    TransferNext(ref recordLifter, recordPusher);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(next));
            }
        }
    }
}