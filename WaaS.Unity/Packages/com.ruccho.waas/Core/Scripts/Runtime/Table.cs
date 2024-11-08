using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a table in the WebAssembly module.
    /// </summary>
    public abstract class Table : IExternal
    {
        protected Table(Limits limits)
        {
            Limits = limits;
        }

        public Limits Limits { get; }
    }

    /// <summary>
    ///     Represents a table in the WebAssembly module.
    /// </summary>
    /// <typeparam name="T"></typeparam>
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