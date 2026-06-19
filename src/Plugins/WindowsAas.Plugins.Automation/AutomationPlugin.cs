using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using WindowsAas.Abstractions.Plugins;
using WindowsAas.Abstractions.Submodels;

namespace WindowsAas.Plugins.Automation;

/// <summary>
/// Automation plugin: attaches scripts and applications and lets them be executed
/// from the AAS, either on demand (write <c>Run = true</c>) or on a timer trigger.
/// Each task is exposed as its own submodel; an info submodel lists them.
/// </summary>
public sealed class AutomationPlugin : IPlugin
{
  public const string InfoSubmodelId = "urn:windows-aas:automation:info";
  public const string TaskSubmodelPrefix = "urn:windows-aas:automation:task:";

  private readonly ITaskRunner _runner;
  private readonly ConcurrentDictionary<string, TaskState> _states = new(StringComparer.Ordinal);
  private readonly List<Timer> _timers = [];

  private IPluginContext? _context;
  private IReadOnlyList<AutomationTask> _tasks = [];

  public AutomationPlugin() : this(new ProcessTaskRunner())
  {
  }

  /// <summary>Test seam allowing the runner to be injected.</summary>
  public AutomationPlugin(ITaskRunner runner) => _runner = runner;

  public string Id => "windows-aas.automation";

  public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
  {
    _context = context;
    _tasks = TaskStore.Load(context.DataDirectory);

    foreach (var task in _tasks.Where(t => t is { Enabled: true, TriggerIntervalSeconds: > 0 }))
    {
      var period = TimeSpan.FromSeconds(task.TriggerIntervalSeconds);
      var captured = task;
      _timers.Add(new Timer(_ => _ = RunAsync(captured, CancellationToken.None), null, period, period));
    }

    return Task.CompletedTask;
  }

  public Task<IReadOnlyList<SubmodelDefinition>> GetSubmodelsAsync(CancellationToken ct = default)
  {
    var submodels = new List<SubmodelDefinition>
    {
      new()
      {
        Id = InfoSubmodelId,
        IdShort = "AutomationInfo",
        SemanticId = "urn:windows-aas:automation:semantics:info",
        Elements = [Prop("TaskCount", _tasks.Count.ToString(CultureInfo.InvariantCulture), AasValueType.Integer)],
      },
    };

    submodels.AddRange(_tasks.Select(BuildTaskSubmodel));
    return Task.FromResult<IReadOnlyList<SubmodelDefinition>>(submodels);
  }

  public Task<string?> ReadAsync(string submodelId, string idShortPath, CancellationToken ct = default)
  {
    var task = TaskFor(submodelId);
    return Task.FromResult(task is null ? null : Read(task, idShortPath));
  }

  public async Task<PropertyWriteResult> WriteAsync(PropertyWriteRequest request, CancellationToken ct = default)
  {
    var task = TaskFor(request.SubmodelId);
    if (task is null)
    {
      return PropertyWriteResult.Fail($"Unknown task submodel '{request.SubmodelId}'.");
    }

    if (request.IdShortPath != "Run" || !bool.TryParse(request.Value, out var run) || !run)
    {
      return PropertyWriteResult.Fail("Only 'Run = true' is accepted on a task.");
    }

    if (!task.Enabled)
    {
      return PropertyWriteResult.Fail($"Task '{task.Id}' is disabled.");
    }

    var result = await RunAsync(task, ct);
    return result.Error is null
      ? PropertyWriteResult.Ok(result.ExitCode.ToString(CultureInfo.InvariantCulture))
      : PropertyWriteResult.Fail(result.Error);
  }

  public ValueTask DisposeAsync()
  {
    foreach (var timer in _timers)
    {
      timer.Dispose();
    }

    _timers.Clear();
    return ValueTask.CompletedTask;
  }

  private async Task<TaskRunResult> RunAsync(AutomationTask task, CancellationToken ct)
  {
    var state = _states.GetOrAdd(task.Id, _ => new TaskState());
    state.Running = true;
    _context?.Logger.LogInformation("Running automation task {TaskId} ({Name}).", task.Id, task.Name);

    var result = await _runner.RunAsync(task, ct);

    state.Running = false;
    state.LastResult = result;

    if (_context is { } context)
    {
      await context.ReportValueAsync(
        $"{TaskSubmodelPrefix}{task.Id}",
        "LastExitCode",
        result.ExitCode.ToString(CultureInfo.InvariantCulture),
        ct);
    }

    return result;
  }

  private AutomationTask? TaskFor(string submodelId)
  {
    if (!submodelId.StartsWith(TaskSubmodelPrefix, StringComparison.Ordinal))
    {
      return null;
    }

    var id = submodelId[TaskSubmodelPrefix.Length..];
    return _tasks.FirstOrDefault(t => t.Id == id);
  }

  private string? Read(AutomationTask task, string idShortPath)
  {
    var state = _states.GetValueOrDefault(task.Id);
    return idShortPath switch
    {
      "Name" => task.Name,
      "Kind" => task.Kind.ToString(),
      "Command" => $"{task.FileName} {task.Arguments}".Trim(),
      "Enabled" => task.Enabled ? "true" : "false",
      "Running" => state is { Running: true } ? "true" : "false",
      "LastExitCode" => state?.LastResult?.ExitCode.ToString(CultureInfo.InvariantCulture),
      "LastRunUtc" => state?.LastResult?.StartedUtc.ToString("O", CultureInfo.InvariantCulture),
      _ => null,
    };
  }

  private static SubmodelDefinition BuildTaskSubmodel(AutomationTask task) => new()
  {
    Id = $"{TaskSubmodelPrefix}{task.Id}",
    IdShort = $"Task_{task.Id}",
    SemanticId = "urn:windows-aas:automation:semantics:task",
    Elements =
    [
      Prop("Name", task.Name),
      Prop("Kind", task.Kind.ToString()),
      Prop("Command", $"{task.FileName} {task.Arguments}".Trim()),
      Prop("Enabled", task.Enabled ? "true" : "false", AasValueType.Boolean),
      Prop("Run", "false", AasValueType.Boolean, writable: true),
      Prop("Running", "false", AasValueType.Boolean),
      Prop("LastExitCode", null, AasValueType.Integer),
      Prop("LastRunUtc", null),
    ],
  };

  private static SubmodelElement Prop(
    string idShort,
    string? value,
    AasValueType type = AasValueType.String,
    bool writable = false) => new()
  {
    IdShort = idShort,
    Kind = SubmodelElementKind.Property,
    ValueType = type,
    Value = value,
    Writable = writable,
  };

  private sealed class TaskState
  {
    public volatile bool Running;
    public TaskRunResult? LastResult;
  }
}
