using System.Collections.Concurrent;
using System.Linq;
using System;

class LogStore
{
    public class LogEntry
    {
        public DateTime Time { get; set; }
        public string? Category { get; set; }
        public string? Message { get; set; }
    }

    private readonly ConcurrentQueue<LogEntry> _q = new();

    public void Enqueue(LogEntry entry)
    {
        entry.Time = DateTime.UtcNow;
        _q.Enqueue(entry);
        while (_q.Count > 200)
        {
            _q.TryDequeue(out _);
        }
    }

    public LogEntry[] GetLast(int n)
    {
        var arr = _q.ToArray();
        if (arr.Length <= n) return arr;
        return arr.Skip(arr.Length - n).ToArray();
    }

    public LogEntry[] GetLastForCategory(string? category, int n)
    {
        if (string.IsNullOrEmpty(category)) return GetLast(n);
        var arr = _q.Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (arr.Length <= n) return arr;
        return arr.Skip(arr.Length - n).ToArray();
    }
}
