using Microsoft.Extensions.Logging;

namespace WindowsAas.Abstractions.Plugins;

/// <summary>
/// Services the host provides to a plugin at runtime. Passed to
/// <see cref="IPlugin.InitializeAsync"/>.
/// </summary>
public interface IPluginContext
{
  /// <summary>Logger scoped to the plugin (flows into the shared log / UI viewer).</summary>
  ILogger Logger { get; }

  /// <summary>
  /// Per-plugin configuration values set by the admin in the web UI. Persisted by
  /// the host in the encrypted configuration store.
  /// </summary>
  IReadOnlyDictionary<string, string> Configuration { get; }

  /// <summary>
  /// Directory the plugin may use for its own state (created/owned by the host).
  /// </summary>
  string DataDirectory { get; }

  /// <summary>
  /// Pushes a changed property value toward the AAS (host → MQTT → AAS environment).
  /// Used for telemetry and to report the actual value after a write.
  /// </summary>
  ValueTask ReportValueAsync(string submodelId, string idShortPath, string value, CancellationToken ct = default);
}
