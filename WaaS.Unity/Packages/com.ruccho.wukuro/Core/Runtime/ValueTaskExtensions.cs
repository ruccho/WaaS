using System;
using System.Threading.Tasks;

namespace Wukuro
{
    internal static class ValueTaskExtensions
    {
        public static T AssumeCompleted<T>(this ValueTask<T> task)
        {
            var awaiter = task.GetAwaiter();
            if (!awaiter.IsCompleted) throw new InvalidOperationException("Task is not completed yet.");
            return awaiter.GetResult();
        }

        public static void AssumeCompleted(this ValueTask task)
        {
            var awaiter = task.GetAwaiter();
            if (!awaiter.IsCompleted) throw new InvalidOperationException("Task is not completed yet.");
            awaiter.GetResult();
        }
    }
}