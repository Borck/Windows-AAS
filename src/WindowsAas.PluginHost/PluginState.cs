namespace WindowsAas.PluginHost;

/// <summary>Lifecycle state of an installed plugin.</summary>
public enum PluginState
{
  /// <summary>Present on disk but not loaded.</summary>
  Installed,

  /// <summary>Loaded, initialized and contributing submodels.</summary>
  Enabled,

  /// <summary>Installed but intentionally not loaded.</summary>
  Disabled,

  /// <summary>Loading or initialization failed; see the log for details.</summary>
  Faulted,
}
