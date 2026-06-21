namespace WindowsAas.Repository.Client;

/// <summary>One installable plugin advertised by the online repository.</summary>
public sealed record RepositoryEntry
{
  public required string Id { get; init; }

  public required string Name { get; init; }

  public required string Version { get; init; }

  public string? Description { get; init; }

  public string? Author { get; init; }

  /// <summary>Relative or absolute URL of the plugin package (a .zip).</summary>
  public required string PackageUrl { get; init; }

  /// <summary>URL of the detached RSA signature over the package bytes.</summary>
  public required string SignatureUrl { get; init; }

  /// <summary>Lowercase hex SHA-256 of the package, for an integrity pre-check.</summary>
  public required string Sha256 { get; init; }
}

/// <summary>The repository's catalogue of available plugins.</summary>
public sealed record RepositoryIndex
{
  public IReadOnlyList<RepositoryEntry> Plugins { get; init; } = [];
}
