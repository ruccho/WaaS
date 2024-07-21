using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    public abstract class Table : IExternal
    {
        protected Table(Limits limits)
        {
            Limits = limits;
        }

        public Limits Limits { get; }
    }

    public class Table<T> : Table
    {
        private readonly T[] buffer;

        public Table(Limits limits) : base(limits)
        {
            buffer = new T[limits.Min];
        }

        public Span<T> Elements => buffer;
    }
}