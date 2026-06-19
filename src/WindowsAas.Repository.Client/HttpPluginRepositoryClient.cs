using System.IO.Compression;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WindowsAas.Security;

namespace WindowsAas.Repository.Client;

/// <summary>
/// HTTP <see cref="IPluginRepositoryClient"/>. Installation verifies the SHA-256
/// integrity hash and the detached RSA signature (via <see cref="IPackageVerifier"/>)
/// before any file is written to the plugins directory.
/// </summary>
public sealed class HttpPluginRepositoryClient(
  HttpClient httpClient,
  IPackageVerifier verifier,
  IOptions<RepositoryOptions> options,
  ILogger<HttpPluginRepositoryClient> logger) : IPluginRepositoryClient
{
  private readonly RepositoryOptions _options = options.Value;

  public async Task<RepositoryIndex> GetIndexAsync(CancellationToken ct = default)
  {
    var url = $"{_options.BaseUrl.TrimEnd('/')}/index.json";
    var index = await httpClient.GetFromJsonAsync<RepositoryIndex>(url, ct);
    return index ?? new RepositoryIndex();
  }

  public async Task InstallAsync(RepositoryEntry entry, string pluginsDirectory, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(entry);

    var package = await httpClient.GetByteArrayAsync(Resolve(entry.PackageUrl), ct);
    var signature = await httpClient.GetByteArrayAsync(Resolve(entry.SignatureUrl), ct);

    VerifyIntegrity(entry, package);
    VerifySignature(package, signature);

    var target = Path.Combine(pluginsDirectory, entry.Id);
    if (Directory.Exists(target))
    {
      Directory.Delete(target, recursive: true);
    }

    Directory.CreateDirectory(target);
    using var archive = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read);
    archive.ExtractToDirectory(target, overwriteFiles: true);

    logger.LogInformation("Installed plugin {PluginId} v{Version}.", entry.Id, entry.Version);
  }

  private static void VerifyIntegrity(RepositoryEntry entry, byte[] package)
  {
    var actual = Convert.ToHexStringLower(SHA256.HashData(package));
    if (!string.Equals(actual, entry.Sha256, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException(
        $"Integrity check failed for plugin '{entry.Id}': SHA-256 mismatch.");
    }
  }

  private void VerifySignature(byte[] package, byte[] signature)
  {
    using var stream = new MemoryStream(package);
    if (!verifier.Verify(stream, signature))
    {
      throw new InvalidOperationException("Signature verification failed: untrusted or tampered package.");
    }
  }

  private string Resolve(string url) =>
    Uri.TryCreate(url, UriKind.Absolute, out _) ? url : $"{_options.BaseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
}
