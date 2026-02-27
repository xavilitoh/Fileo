using System;

namespace Fileo.Core.Interfaces
{
    public enum LogLevel { Info, Error, DryRun }

    public interface IFileLogger
    {
        // category may be null for general/system messages
        void Log(string message, LogLevel level = LogLevel.Info, string? category = null);
    }
}
