using Fileo.Core.Interfaces;

class LiveLogger : IFileLogger
{
    private readonly LogStore _store;

    public LiveLogger(LogStore store) => _store = store;

    public void Log(string message, LogLevel level = LogLevel.Info, string? category = null)
    {
        var prefix = level == LogLevel.Error ? "ERR" : level == LogLevel.DryRun ? "DRY" : "INF";
        var line = $"{prefix} {message}";
        _store.Enqueue(new LogStore.LogEntry { Category = category, Message = line });
    }
}
