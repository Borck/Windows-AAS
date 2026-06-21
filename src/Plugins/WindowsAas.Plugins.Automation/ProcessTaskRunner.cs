using System.Diagnostics;

namespace WindowsAas.Plugins.Automation;

/// <summary>
/// <see cref="ITaskRunner"/> that launches the task as a child process and waits for
/// it to exit. Output is not redirected to a shell; the process is started without a
/// window (UseShellExecute = false) so it runs under the service account.
/// </summary>
public sealed class ProcessTaskRunner : ITaskRunner
{
  public async Task<TaskRunResult> RunAsync(AutomationTask task, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(task);
    var startedUtc = DateTimeOffset.UtcNow;

    var startInfo = new ProcessStartInfo
    {
      FileName = task.FileName,
      Arguments = task.Arguments,
      UseShellExecute = false,
      CreateNoWindow = true,
      WorkingDirectory = task.WorkingDirectory ?? string.Empty,
    };

    try
    {
      using var process = Process.Start(startInfo);
      if (process is null)
      {
        return new TaskRunResult(-1, startedUtc, "Process failed to start.");
      }

      await process.WaitForExitAsync(ct);
      return new TaskRunResult(process.ExitCode, startedUtc, null);
    }
    catch (Exception ex)
    {
      return new TaskRunResult(-1, startedUtc, ex.Message);
    }
  }
}
