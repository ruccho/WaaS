#nullable enable

using System;

namespace WaaS.ComponentModel.Runtime
{
    public interface IValuePusherCore : IVersionedDisposable<ushort>
    {
        void Push(bool value);
        void Push(byte value);
        void Push(sbyte value);
        void Push(ushort value);
        void Push(short value);
        void Push(uint value);
        void Push(int value);
        void Push(ulong value);
        void Push(long value);
        void Push(float value);
        void Push(double value);
        void PushChar32(uint value);
        void Push(ReadOnlySpan<char> value);
        ValuePusher PushRecord();
        ValuePusher PushVariant(int caseIndex);
        ValuePusher PushList(int length);
        void PushFlags(uint flagValue);

        void PushOwned(Owned handle);

        void PushBorrowed(Borrowed handle);
    }
}