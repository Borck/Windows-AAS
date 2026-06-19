namespace WindowsAas.Plugins.Automation;

/// <summary>Result of executing an <see cref="AutomationTask"/>.</summary>
/// <param name="ExitCode">Process exit code (or -1 if it failed to start).</param>
/// <param name="StartedUtc">When execution started.</param>
/// <param name="Error">Failure detail, if any.</param>
public readonly record struct TaskRunResult(int ExitCode, DateTimeOffset StartedUtc, string? Error);

/// <summary>Executes automation tasks as host processes.</summary>
public interface ITaskRunner
{
  Task<TaskRunResult> RunAsync(AutomationTask task, CancellationToken ct = default);
}
