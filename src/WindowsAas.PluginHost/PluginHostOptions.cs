namespace WindowsAas.PluginHost;

/// <summary>Filesystem locations the plugin host manages.</summary>
public sealed class PluginHostOptions
{
  public const string SectionName = "PluginHost";

  /// <summary>
  /// Root directory containing one sub-directory per installed plugin. Defaults to
  /// a machine-wide ProgramData location writable only by the service account.
  /// </summary>
  public string PluginsDirectory { get; set; } =
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
      "WindowsAAS", "plugins");

  /// <summary>Root directory under which each plugin gets its own data folder.</summary>
  public string DataDirectory { get; set; } =
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
      "WindowsAAS", "data");
}
