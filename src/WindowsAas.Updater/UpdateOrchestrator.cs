using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace WindowsAas.Updater;

/// <summary>
/// Coordinates the update flow: download the MSI to a temp file, verify its SHA-256
/// (when published) and Authenticode signature, then hand off to the installer.
/// </summary>
public sealed class UpdateOrchestrator(
  HttpClient httpClient,
  IAuthenticodeValidator authenticode,
  IUpdateInstaller installer,
  ILogger<UpdateOrchestrator> logger)
{
  /// <summary>Downloads, verifies and applies <paramref name="update"/>.</summary>
  /// <returns><c>true</c> if the installer was launched.</returns>
  public async Task<bool> ApplyAsync(UpdateInfo update, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(update);

    var bytes = await httpClient.GetByteArrayAsync(update.MsiUrl, ct);

    if (update.Sha256 is { } expected)
    {
      var actual = Convert.ToHexStringLower(SHA256.HashData(bytes));
      if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
      {
        logger.LogError("Update {Version} rejected: SHA-256 mismatch.", update.Version);
        return false;
      }
    }

    var path = Path.Combine(Path.GetTempPath(), $"WindowsAAS-{update.Version}.msi");
    await File.WriteAllBytesAsync(path, bytes, ct);

    if (!authenticode.Validate(path))
    {
      logger.LogError("Update {Version} rejected: Authenticode validation failed.", update.Version);
      File.Delete(path);
      return false;
    }

    logger.LogInformation("Applying update {Version}.", update.Version);
    installer.Install(path);
    return true;
  }
}
