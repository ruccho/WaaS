#nullable enable

using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace WaaS.ComponentModel.Runtime
{
    public static class ValueTransfer
    {
        private static void TransferString(ref ValueLifter lifter, ValuePusher pusher)
        {
            var info = lifter.PullStringInfo();

            var maxLength = lifter.GetStringMaxCharCount(info);
            if (maxLength > 1024)
            {
                var array = ArrayPool<char>.Shared.Rent(maxLength);
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
            var next = lifter.GetNextType().Despecialize();
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
                            pusher.PushChar32(lifter.PullChar());
                            break;
                        case PrimitiveValueTypeKind.F64:
                            pusher.Push(lifter.PullF64());
                            break;
                        case PrimitiveValueTypeKind.F32:
                            pusher.Push(lifter.PullF32());
                            break;
                        case PrimitiveValueTypeKind.U64:
                            pusher.Push(lifter.PullU64());
                            break;
                        case PrimitiveValueTypeKind.S64:
                            pusher.Push(lifter.PullS64());
                            break;
                        case PrimitiveValueTypeKind.U32:
                            pusher.Push(lifter.PullU32());
                            break;
                        case PrimitiveValueTypeKind.S32:
                            pusher.Push(lifter.PullS32());
                            break;
                        case PrimitiveValueTypeKind.U16:
                            pusher.Push(lifter.PullU16());
                            break;
                        case PrimitiveValueTypeKind.S16:
                            pusher.Push(lifter.PullS16());
                            break;
                        case PrimitiveValueTypeKind.U8:
                            pusher.Push(lifter.PullU8());
                            break;
                        case PrimitiveValueTypeKind.S8:
                            pusher.Push(lifter.PullS8());
                            break;
                        case PrimitiveValueTypeKind.Bool:
                            pusher.Push(lifter.PullBool());
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
                    var listPusher = pusher.PushList(checked((int)length));
                    for (var i = 0; i < length; i++) TransferNext(ref listLifter, listPusher);

                    break;
                }
                case IRecordType recordType:
                {
                    var recordLifter = lifter.PullRecord();
                    var recordPusher = pusher.PushRecord();
                    for (var i = 0; i < recordType.Fields.Length; i++)
                        TransferNext(ref recordLifter, recordPusher);

                    break;
                }
                case IVariantType variantType:
                {
                    var recordLifter = lifter.PullVariant(out var caseIndex);
                    var recordPusher = pusher.PushVariant(checked((int)caseIndex));
                    TransferNext(ref recordLifter, recordPusher);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(next));
            }
        }
    }
}