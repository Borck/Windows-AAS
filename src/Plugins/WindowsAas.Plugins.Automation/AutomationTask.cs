namespace WindowsAas.Plugins.Automation;

/// <summary>Whether a task launches an application or runs a script via an interpreter.</summary>
public enum TaskKind
{
  Application,
  Script,
}

/// <summary>
/// A configurable automation task: an application or script the admin attaches and
/// that can be executed from the AAS. Loaded from <c>automation.json</c> in the
/// plugin's data directory (written by the admin UI).
/// </summary>
public sealed record AutomationTask
{
  /// <summary>Stable id, unique within the plugin (used in the submodel id).</summary>
  public required string Id { get; init; }

  public required string Name { get; init; }

  public TaskKind Kind { get; init; } = TaskKind.Application;

  /// <summary>Executable or interpreter to launch (e.g. <c>powershell.exe</c>).</summary>
  public required string FileName { get; init; }

  /// <summary>Arguments (for a script, the script path plus its arguments).</summary>
  public string Arguments { get; init; } = string.Empty;

  public string? WorkingDirectory { get; init; }

  public bool Enabled { get; init; } = true;

  /// <summary>
  /// Optional automatic trigger interval in seconds. When &gt; 0 the task runs on
  /// this cadence in addition to AAS-initiated runs. 0 disables the timer trigger.
  /// </summary>
  public int TriggerIntervalSeconds { get; init; }
}
