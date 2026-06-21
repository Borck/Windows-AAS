using Serilog.Core;
using Serilog.Events;
using WindowsAas.Web.Logging;

namespace WindowsAas.Service.Logging;

/// <summary>
/// Serilog sink that mirrors log events into the in-memory <see cref="LogStore"/>
/// so the admin UI's "Logs" page can show recent activity and errors.
/// </summary>
public sealed class LogStoreSink(LogStore store, IFormatProvider? formatProvider = null) : ILogEventSink
{
  public void Emit(LogEvent logEvent)
  {
    ArgumentNullException.ThrowIfNull(logEvent);
    var source = logEvent.Properties.TryGetValue("SourceContext", out var ctx)
      ? ctx.ToString().Trim('"')
      : "Service";

    store.Add(new LogEntry(
      logEvent.Timestamp,
      logEvent.Level.ToString(),
      logEvent.RenderMessage(formatProvider),
      source));
  }
}
