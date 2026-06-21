namespace WindowsAas.Updater;

/// <summary>How updates are applied once detected.</summary>
public enum UpdateChannel
{
  /// <summary>Only surface availability; the admin applies updates from the UI.</summary>
  Manual,

  /// <summary>Download, verify and install automatically on detection.</summary>
  Automatic,
}

/// <summary>Settings for the self-update feature.</summary>
public sealed class UpdaterOptions
{
  public const string SectionName = "Updater";

  /// <summary>Currently installed version (set from the assembly version at startup).</summary>
  public string CurrentVersion { get; set; } = "0.0.0";

  /// <summary>GitHub "latest release" API URL used as the default update feed.</summary>
  public string FeedUrl { get; set; } = "https://api.github.com/repos/Borck/Windows-AAS/releases/latest";

  public UpdateChannel Channel { get; set; } = UpdateChannel.Manual;

  /// <summary>How often the background service polls the feed.</summary>
  public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(6);

  /// <summary>Expected Authenticode certificate subject of the signed MSI.</summary>
  public string ExpectedPublisher { get; set; } = "CN=Windows-AAS";

  /// <summary>Prefer <c>winget upgrade</c> when winget is available on the host.</summary>
  public bool PreferWinget { get; set; } = true;

  /// <summary>winget package identifier used when <see cref="PreferWinget"/> is set.</summary>
  public string WingetPackageId { get; set; } = "Borck.WindowsAas";
}
