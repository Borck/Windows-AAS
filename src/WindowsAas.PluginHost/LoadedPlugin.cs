using WindowsAas.Abstractions.Plugins;

namespace WindowsAas.PluginHost;

/// <summary>
/// Snapshot of an installed plugin's identity and current state, exposed to the
/// admin UI. The live <see cref="IPlugin"/> instance is held internally by the host.
/// </summary>
public sealed record PluginInfo
{
  public required string Id { get; init; }

  public required string Name { get; init; }

  public required string Version { get; init; }

  public required PluginState State { get; init; }

  public string? Error { get; init; }
}

/// <summary>Internal record tracking a plugin across its lifecycle.</summary>
internal sealed class LoadedPlugin
{
  public required PluginManifest Manifest { get; init; }

  public required string Directory { get; init; }

  public PluginState State { get; set; } = PluginState.Installed;

  public string? Error { get; set; }

  public IPlugin? Instance { get; set; }

  public PluginLoadContext? LoadContext { get; set; }

  public PluginInfo ToInfo() => new()
  {
    Id = Manifest.Id,
    Name = Manifest.Name,
    Version = Manifest.Version,
    State = State,
    Error = Error,
  };
}
