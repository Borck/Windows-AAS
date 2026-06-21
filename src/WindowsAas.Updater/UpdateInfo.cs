namespace WindowsAas.Updater;

/// <summary>Describes an available newer release discovered on the update feed.</summary>
public sealed record UpdateInfo
{
  public required string Version { get; init; }

  /// <summary>Download URL of the signed MSI asset.</summary>
  public required string MsiUrl { get; init; }

  /// <summary>Expected lowercase hex SHA-256 of the MSI (from the release notes/asset).</summary>
  public string? Sha256 { get; init; }
}
