using WindowsAas.Abstractions.Submodels;

namespace WindowsAas.Aas;

/// <summary>
/// Client for the BaSyx AAS environment. Used to register the host AAS shell and
/// plugin submodels, and (as the MQTT→environment ingestion bridge) to reflect
/// changed property values back into the environment so it stays queryable.
/// </summary>
public interface IAasEnvironmentClient
{
  /// <summary>Creates (or replaces) the AAS shell representing this Windows host.</summary>
  Task EnsureHostShellAsync(CancellationToken ct = default);

  /// <summary>Creates or replaces a submodel and links it to the host shell.</summary>
  Task PutSubmodelAsync(SubmodelDefinition submodel, CancellationToken ct = default);

  /// <summary>Removes a submodel (e.g. when a plugin is disabled or uninstalled).</summary>
  Task DeleteSubmodelAsync(string submodelId, CancellationToken ct = default);

  /// <summary>
  /// Updates the value of a single property element (the ingestion bridge / telemetry
  /// write-back path).
  /// </summary>
  Task SetElementValueAsync(string submodelId, string idShortPath, string value, CancellationToken ct = default);
}
