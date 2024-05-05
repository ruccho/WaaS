using System;

namespace WaaS
{
    public static class Logger
    {
        public static ILogger GlobalLogger { get; set; }

        internal static void Log(string message)
        {
            GlobalLogger?.Log(message);
        }

        internal static void LogWarning(string message)
        {
            GlobalLogger?.LogWarning(message);
        }

        internal static void LogError(string message)
        {
            GlobalLogger?.LogError(message);
        }

        internal static void LogException(Exception exception)
        {
            GlobalLogger?.LogException(exception);
        }
    }
}