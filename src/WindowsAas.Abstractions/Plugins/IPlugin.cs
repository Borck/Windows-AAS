using WindowsAas.Abstractions.Submodels;

namespace WindowsAas.Abstractions.Plugins;

/// <summary>
/// The contract every Windows-AAS plugin implements. The host discovers the type
/// named by <see cref="PluginManifest.EntryType"/>, instantiates it in an isolated
/// <c>AssemblyLoadContext</c>, and drives it through this lifecycle.
/// </summary>
public interface IPlugin : IAsyncDisposable
{
  /// <summary>Static identity of the plugin (matches the manifest id).</summary>
  string Id { get; }

  /// <summary>
  /// Prepares the plugin for use. Called once after construction, before any other
  /// member. Implementations should capture <paramref name="context"/> for later use.
  /// </summary>
  Task InitializeAsync(IPluginContext context, CancellationToken ct = default);

  /// <summary>
  /// Returns the submodels this plugin contributes to the AAS: one info/overview
  /// submodel plus any capability submodels (e.g. one per audio/video device).
  /// Re-queried when the host reconciles state with the AAS environment.
  /// </summary>
  Task<IReadOnlyList<SubmodelDefinition>> GetSubmodelsAsync(CancellationToken ct = default);

  /// <summary>
  /// Reads the current value of a single property element (host → AAS telemetry path).
  /// </summary>
  Task<string?> ReadAsync(string submodelId, string idShortPath, CancellationToken ct = default);

  /// <summary>
  /// Applies a write coming from an AAS client (AAS → host control path).
  /// </summary>
  Task<PropertyWriteResult> WriteAsync(PropertyWriteRequest request, CancellationToken ct = default);
}
