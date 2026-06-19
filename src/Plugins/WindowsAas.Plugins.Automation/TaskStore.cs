using System.Text.Json;

namespace WindowsAas.Plugins.Automation;

/// <summary>
/// Loads automation task definitions from <c>automation.json</c> in the plugin's
/// data directory. The file is authored by the admin UI's plugin configuration page.
/// </summary>
public static class TaskStore
{
  public const string FileName = "automation.json";

  private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
  {
    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
  };

  public static IReadOnlyList<AutomationTask> Load(string dataDirectory)
  {
    var path = Path.Combine(dataDirectory, FileName);
    if (!File.Exists(path))
    {
      return [];
    }

    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<List<AutomationTask>>(json, Json) ?? [];
  }
}
