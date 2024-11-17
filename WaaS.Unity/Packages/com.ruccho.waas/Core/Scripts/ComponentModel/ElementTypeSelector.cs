#nullable enable

using System;

namespace WaaS.ComponentModel.Runtime
{
    public readonly struct ElementTypeSelector
    {
        private readonly Kind kind;
        private readonly IValueType? type;
        private readonly int listLength;

        private enum Kind
        {
            Record,
            List,
            Single
        }

        private ElementTypeSelector(Kind kind, IValueType? type, int listLength)
        {
            this.kind = kind;
            this.type = type;
            this.listLength = listLength;
        }

        public static ElementTypeSelector FromRecord(IRecordType record)
        {
            return new ElementTypeSelector(Kind.Record, record, -1);
        }

        public static ElementTypeSelector FromList(IListType list, int length)
        {
            return new ElementTypeSelector(Kind.List, list, length);
        }

        public static ElementTypeSelector FromSingle(IValueType? single)
        {
            return new ElementTypeSelector(Kind.Single, single, -1);
        }

        public IValueType? GetNextType(int index)
        {
            switch (kind)
            {
                case Kind.Record:
                {
                    var fields = (type as IRecordType)!.Fields.Span;
                    return index < fields.Length ? fields[index].Type : null;
                }
                case Kind.List:
                    return index < listLength
                        ? (type as IListType)!.ElementType
                        : null;
                case Kind.Single:
                    return type != null && index == 0 ? type : null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}