namespace WindowsAas.Aas;

/// <summary>
/// Connection settings for the BaSyx AAS environment and registries the core
/// service registers against. Bound from the (encrypted) configuration store.
/// </summary>
public sealed class AasOptions
{
  public const string SectionName = "Aas";

  /// <summary>Base URL of the BaSyx <c>aas-environment</c> REST API.</summary>
  public string EnvironmentUrl { get; set; } = "http://localhost:8081";

  /// <summary>Base URL of the AAS registry.</summary>
  public string AasRegistryUrl { get; set; } = "http://localhost:8082";

  /// <summary>Base URL of the submodel registry.</summary>
  public string SubmodelRegistryUrl { get; set; } = "http://localhost:8083";

  /// <summary>Id of the AAS shell that represents this Windows host.</summary>
  public string HostShellId { get; set; } = "urn:windows-aas:host";

  /// <summary>idShort of the host AAS shell.</summary>
  public string HostShellIdShort { get; set; } = "WindowsHost";
}
