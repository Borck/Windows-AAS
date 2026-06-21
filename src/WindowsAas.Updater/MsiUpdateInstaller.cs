using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindowsAas.Updater;

/// <summary>
/// Applies the update on Windows by launching a detached <c>msiexec</c> (or
/// <c>winget upgrade</c>) process. The new MSI re-registers the service so it
/// restarts after the in-place upgrade.
/// </summary>
public sealed class MsiUpdateInstaller(
  IOptions<UpdaterOptions> options,
  ILogger<MsiUpdateInstaller> logger) : IUpdateInstaller
{
  private readonly UpdaterOptions _options = options.Value;

  public void Install(string msiPath)
  {
    if (!OperatingSystem.IsWindows())
    {
      throw new PlatformNotSupportedException("Update installation is only supported on Windows.");
    }

    var startInfo = _options.PreferWinget && IsWingetAvailable()
      ? new ProcessStartInfo("winget", $"upgrade --id {_options.WingetPackageId} --silent --accept-source-agreements")
      : new ProcessStartInfo("msiexec", $"/i \"{msiPath}\" /qn /norestart");

    startInfo.UseShellExecute = true;
    startInfo.CreateNoWindow = true;

    logger.LogInformation("Launching update installer: {File} {Args}", startInfo.FileName, startInfo.Arguments);
    Process.Start(startInfo);
  }

  private static bool IsWingetAvailable()
  {
    try
    {
      using var process = Process.Start(new ProcessStartInfo("winget", "--version")
      {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
      });
      process?.WaitForExit(3000);
      return process is { ExitCode: 0 };
    }
    catch
    {
      return false;
    }
  }
}
