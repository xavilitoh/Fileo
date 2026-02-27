using System;
using Spectre.Console;
using Fileo.Core.Interfaces;

class ConsoleFileLogger : IFileLogger
{
    private readonly LogStore? _store;
    public ConsoleFileLogger(LogStore? store = null) => _store = store;

    public void Log(string message, LogLevel level = LogLevel.Info, string? category = null)
    {
        var prefix = level == LogLevel.Error ? "ERR" : level == LogLevel.DryRun ? "DRY" : "INF";
        var formatted = $"{prefix} {message}";
        if (_store != null)
        {
            _store.Enqueue(new LogStore.LogEntry { Category = category, Message = formatted });
            return;
        }

        // Escape message content to avoid Spectre markup parsing issues
        var escMsg = Spectre.Console.Markup.Escape(message);
        var escCat = string.IsNullOrEmpty(category) ? string.Empty : Spectre.Console.Markup.Escape(category);
        switch (level)
        {
            case LogLevel.DryRun:
                AnsiConsole.MarkupLine($"[yellow]{escCat} DRY[/]: {escMsg}");
                break;
            case LogLevel.Error:
                AnsiConsole.MarkupLine($"[red]{escCat} ERR[/]: {escMsg}");
                break;
            default:
                if (!string.IsNullOrEmpty(escCat)) AnsiConsole.MarkupLine($"[green]{escCat}[/]: {escMsg}");
                else AnsiConsole.MarkupLine($"[green]{escMsg}[/]");
                break;
        }
    }
}
