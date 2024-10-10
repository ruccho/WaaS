#nullable enable

using System;
using System.Buffers;

namespace WaaS.ComponentModel.Runtime
{
    public static class ValueTransfer
    {
        private static void TransferString(ValueLifter lifter, ValuePusher pusher)
        {
            var maxLength = lifter.PollStringMaxCharCount();
            if (maxLength > 1024)
            {
                var array = ArrayPool<char>.Shared.Rent(maxLength);
                try
                {
                    TransferStringCore(lifter, pusher, array);
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(array);
                }
            }
            else
            {
                TransferStringCore(lifter, pusher, stackalloc char[maxLength]);
            }

            static void TransferStringCore(ValueLifter lifter, ValuePusher pusher, Span<char> buffer)
            {
                var length = lifter.PullString(buffer);
                buffer = buffer.Slice(0, length);
                pusher.Push(buffer);
            }
        }

        public static void TransferNext(ValueLifter lifter, ValuePusher pusher)
        {
            var next = lifter.GetNextType().Despecialize();
            switch (next)
            {
                case IPrimitiveValueType primitiveValueType:
                {
                    switch (primitiveValueType.Kind)
                    {
                        case PrimitiveValueTypeKind.String:
                            TransferString(lifter, pusher);
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
                    pusher.PushBorrowed<Borrowed<IResourceType>, IResourceType>(lifter.PullBorrowed());
                    break;
                }
                case IOwnedType ownedType:
                {
                    pusher.PushOwned<Owned<IResourceType>, IResourceType>(lifter.PullOwned());
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
                    for (var i = 0; i < length; i++) TransferNext(listLifter, listPusher);

                    break;
                }
                case IRecordType recordType:
                {
                    var recordLifter = lifter.PullRecord();
                    var recordPusher = pusher.PushRecord();
                    for (var i = 0; i < recordType.Fields.Length; i++)
                        TransferNext(recordLifter, recordPusher);

                    break;
                }
                case IVariantType variantType:
                {
                    var recordLifter = lifter.PullVariant(out var caseIndex);
                    var recordPusher = pusher.PushVariant(checked((int)caseIndex));
                    TransferNext(recordLifter, recordPusher);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(next));
            }
        }
    }
}