using WindowsAas.Abstractions.Plugins;
using WindowsAas.Abstractions.Submodels;

namespace WindowsAas.PluginHost;

/// <summary>
/// Manages the lifecycle of installed plugins: discovery, load/unload, and routing
/// of reads and writes to the owning plugin. Backing service for the admin UI's
/// plugin management page.
/// </summary>
public interface IPluginHost
{
  /// <summary>Discovers installed plugins and enables those marked enabled.</summary>
  Task InitializeAsync(CancellationToken ct = default);

  /// <summary>
  /// Discovers plugins added since the last scan (e.g. just installed from the
  /// repository) and enables them, leaving already-tracked plugins untouched.
  /// </summary>
  Task RescanAsync(CancellationToken ct = default);

  /// <summary>Lists all installed plugins and their current state.</summary>
  IReadOnlyList<PluginInfo> List();

  /// <summary>Loads and initializes a plugin (state → Enabled).</summary>
  Task EnableAsync(string pluginId, CancellationToken ct = default);

  /// <summary>Unloads a plugin (state → Disabled), unloading its load context.</summary>
  Task DisableAsync(string pluginId, CancellationToken ct = default);

  /// <summary>Returns the submodels contributed by all currently enabled plugins.</summary>
  Task<IReadOnlyList<SubmodelDefinition>> GetSubmodelsAsync(CancellationToken ct = default);

  /// <summary>
  /// Routes an inbound write to the plugin that owns the target submodel.
  /// </summary>
  Task<PropertyWriteResult> WriteAsync(PropertyWriteRequest request, CancellationToken ct = default);
}
