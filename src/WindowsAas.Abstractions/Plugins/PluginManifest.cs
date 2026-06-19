namespace WindowsAas.Abstractions.Plugins;

/// <summary>
/// Static metadata that describes a plugin package. Shipped as <c>plugin.json</c>
/// inside every plugin package and used by the host and the online repository to
/// identify, version and load a plugin.
/// </summary>
public sealed record PluginManifest
{
  /// <summary>Stable, globally unique plugin id, e.g. <c>windows-aas.av</c>.</summary>
  public required string Id { get; init; }

  /// <summary>Human readable display name.</summary>
  public required string Name { get; init; }

  /// <summary>Semantic version of the plugin package.</summary>
  public required string Version { get; init; }

  /// <summary>Short description shown in the admin UI and repository.</summary>
  public string? Description { get; init; }

  /// <summary>Author / vendor.</summary>
  public string? Author { get; init; }

  /// <summary>
  /// Assembly file (relative to the package root) that contains the
  /// <see cref="IPlugin"/> implementation, e.g. <c>WindowsAas.Plugins.Av.dll</c>.
  /// </summary>
  public required string EntryAssembly { get; init; }

  /// <summary>
  /// Fully qualified type name of the <see cref="IPlugin"/> implementation.
  /// </summary>
  public required string EntryType { get; init; }

  /// <summary>Minimum host (core service) version this plugin supports.</summary>
  public string? MinHostVersion { get; init; }
}
