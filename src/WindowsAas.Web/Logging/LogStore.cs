using System.Collections.Concurrent;

namespace WindowsAas.Web.Logging;

/// <summary>A single captured log entry surfaced in the admin UI log viewer.</summary>
/// <param name="Timestamp">When the entry was written.</param>
/// <param name="Level">Log level name (Information, Warning, Error, …).</param>
/// <param name="Message">Rendered message.</param>
/// <param name="Source">Originating category / logger name.</param>
public readonly record struct LogEntry(DateTimeOffset Timestamp, string Level, string Message, string Source);

/// <summary>
/// Bounded in-memory ring buffer of recent log entries. Fed by a Serilog sink in the
/// service and read by the UI's "Logs" page so admins can see errors without opening
/// the log file. Capacity is intentionally small to bound memory.
/// </summary>
public sealed class LogStore
{
  private readonly int _capacity;
  private readonly ConcurrentQueue<LogEntry> _entries = new();

  public LogStore(int capacity = 2000) => _capacity = capacity;

  /// <summary>Raised whenever a new entry is added (so the UI can refresh live).</summary>
  public event Action? Changed;

  public void Add(LogEntry entry)
  {
    _entries.Enqueue(entry);
    while (_entries.Count > _capacity && _entries.TryDequeue(out _))
    {
    }

    Changed?.Invoke();
  }

  /// <summary>Returns a snapshot of the current entries, newest last.</summary>
  public IReadOnlyList<LogEntry> Snapshot() => _entries.ToArray();
}
