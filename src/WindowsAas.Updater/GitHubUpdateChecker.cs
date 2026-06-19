using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WindowsAas.Updater;

/// <summary>
/// <see cref="IUpdateChecker"/> that reads a GitHub "latest release" document and
/// picks the <c>.msi</c> asset. Versions are compared numerically after stripping a
/// leading <c>v</c>.
/// </summary>
public sealed class GitHubUpdateChecker(
  HttpClient httpClient,
  IOptions<UpdaterOptions> options,
  ILogger<GitHubUpdateChecker> logger) : IUpdateChecker
{
  private readonly UpdaterOptions _options = options.Value;

  public async Task<UpdateInfo?> GetAvailableUpdateAsync(CancellationToken ct = default)
  {
    using var request = new HttpRequestMessage(HttpMethod.Get, _options.FeedUrl);
    request.Headers.UserAgent.ParseAdd("WindowsAAS-Updater");
    request.Headers.Accept.ParseAdd("application/vnd.github+json");

    using var response = await httpClient.SendAsync(request, ct);
    response.EnsureSuccessStatusCode();

    await using var stream = await response.Content.ReadAsStreamAsync(ct);
    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    var root = doc.RootElement;

    var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() : null;
    if (tag is null || !TryParseVersion(tag, out var latest))
    {
      return null;
    }

    if (!TryParseVersion(_options.CurrentVersion, out var current) || latest <= current)
    {
      return null;
    }

    var msiUrl = FindMsiAsset(root);
    if (msiUrl is null)
    {
      logger.LogWarning("Release {Tag} has no .msi asset; skipping.", tag);
      return null;
    }

    return new UpdateInfo { Version = latest.ToString(), MsiUrl = msiUrl };
  }

  private static string? FindMsiAsset(JsonElement root)
  {
    if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
    {
      return null;
    }

    foreach (var asset in assets.EnumerateArray())
    {
      var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
      if (name is not null && name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) &&
          asset.TryGetProperty("browser_download_url", out var url))
      {
        return url.GetString();
      }
    }

    return null;
  }

  private static bool TryParseVersion(string value, out Version version) =>
    Version.TryParse(value.TrimStart('v', 'V'), out version!);
}
