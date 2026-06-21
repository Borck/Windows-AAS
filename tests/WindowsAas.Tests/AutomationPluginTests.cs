using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using WindowsAas.Abstractions.Plugins;
using WindowsAas.Plugins.Automation;
using Xunit;

namespace WindowsAas.Tests;

public class AutomationPluginTests : IDisposable
{
  private readonly string _dataDir = Directory.CreateTempSubdirectory("waas-auto").FullName;

  [Fact]
  public async Task Writing_Run_true_executes_the_task_and_reports_exit_code()
  {
    WriteTasks("""
      [{ "id": "t1", "name": "Echo", "kind": "Application", "fileName": "echo", "enabled": true }]
      """);

    var runner = new FakeRunner(exitCode: 0);
    var context = new FakeContext(_dataDir);
    var plugin = new AutomationPlugin(runner);
    await plugin.InitializeAsync(context);

    var result = await plugin.WriteAsync(
      new PropertyWriteRequest($"{AutomationPlugin.TaskSubmodelPrefix}t1", "Run", "true"));

    result.Success.ShouldBeTrue();
    result.ActualValue.ShouldBe("0");
    runner.RunCount.ShouldBe(1);
    context.Reported.ShouldContain(r => r.path == "LastExitCode" && r.value == "0");
  }

  [Fact]
  public async Task Exposes_an_info_submodel_and_one_submodel_per_task()
  {
    WriteTasks("""
      [{ "id": "a", "name": "A", "fileName": "x" }, { "id": "b", "name": "B", "fileName": "y" }]
      """);

    var plugin = new AutomationPlugin(new FakeRunner(0));
    await plugin.InitializeAsync(new FakeContext(_dataDir));

    var submodels = await plugin.GetSubmodelsAsync();

    submodels.ShouldContain(s => s.Id == AutomationPlugin.InfoSubmodelId);
    submodels.Count(s => s.Id.StartsWith(AutomationPlugin.TaskSubmodelPrefix, StringComparison.Ordinal))
      .ShouldBe(2);
  }

  [Fact]
  public async Task Writing_a_non_Run_property_is_rejected()
  {
    WriteTasks("""[{ "id": "t1", "name": "T", "fileName": "x" }]""");
    var plugin = new AutomationPlugin(new FakeRunner(0));
    await plugin.InitializeAsync(new FakeContext(_dataDir));

    var result = await plugin.WriteAsync(
      new PropertyWriteRequest($"{AutomationPlugin.TaskSubmodelPrefix}t1", "Name", "nope"));

    result.Success.ShouldBeFalse();
  }

  private void WriteTasks(string json) =>
    File.WriteAllText(Path.Combine(_dataDir, TaskStore.FileName), json);

  public void Dispose() => Directory.Delete(_dataDir, recursive: true);

  private sealed class FakeRunner(int exitCode) : ITaskRunner
  {
    public int RunCount { get; private set; }

    public Task<TaskRunResult> RunAsync(AutomationTask task, CancellationToken ct = default)
    {
      RunCount++;
      return Task.FromResult(new TaskRunResult(exitCode, DateTimeOffset.UtcNow, null));
    }
  }

  private sealed class FakeContext(string dataDir) : IPluginContext
  {
    public List<(string submodelId, string path, string value)> Reported { get; } = [];

    public ILogger Logger => NullLogger.Instance;

    public IReadOnlyDictionary<string, string> Configuration { get; } = new Dictionary<string, string>();

    public string DataDirectory => dataDir;

    public ValueTask ReportValueAsync(string submodelId, string idShortPath, string value, CancellationToken ct = default)
    {
      Reported.Add((submodelId, idShortPath, value));
      return ValueTask.CompletedTask;
    }
  }
}
