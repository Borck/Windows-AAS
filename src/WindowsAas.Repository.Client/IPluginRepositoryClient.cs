namespace WindowsAas.Repository.Client;

/// <summary>
/// Client for the online plugin repository: lists available plugins and installs
/// them after verifying their integrity (SHA-256) and authenticity (RSA signature).
/// </summary>
public interface IPluginRepositoryClient
{
  /// <summary>Fetches the repository catalogue.</summary>
  Task<RepositoryIndex> GetIndexAsync(CancellationToken ct = default);

  /// <summary>
  /// Downloads, verifies and extracts a plugin into <paramref name="pluginsDirectory"/>
  /// (one sub-folder per plugin id). Throws if verification fails; the partially
  /// downloaded content is never extracted in that case.
  /// </summary>
  Task InstallAsync(RepositoryEntry entry, string pluginsDirectory, CancellationToken ct = default);
}
