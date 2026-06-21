using System.Security.Cryptography;
using System.Text.Json;
using WindowsAas.Repository.Client;

namespace WindowsAas.Repository.Server;

/// <summary>
/// Builds the repository index by scanning a packages directory. Each plugin is a
/// trio of files sharing a base name: <c>{base}.zip</c> (package), <c>{base}.zip.sig</c>
/// (detached RSA signature) and <c>{base}.json</c> (an <see cref="EntryMetadata"/>).
/// </summary>
public sealed class PackageCatalog(string packagesDirectory)
{
  private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

  public string PackagesDirectory { get; } = packagesDirectory;

  /// <summary>Metadata authored alongside each package.</summary>
  public sealed record EntryMetadata(string Id, string Name, string Version, string? Description, string? Author);

  public RepositoryIndex BuildIndex()
  {
    if (!Directory.Exists(PackagesDirectory))
    {
      return new RepositoryIndex();
    }

    var entries = new List<RepositoryEntry>();
    foreach (var zip in Directory.EnumerateFiles(PackagesDirectory, "*.zip"))
    {
      var baseName = Path.GetFileNameWithoutExtension(zip);
      var metaPath = Path.Combine(PackagesDirectory, baseName + ".json");
      var sigPath = zip + ".sig";
      if (!File.Exists(metaPath) || !File.Exists(sigPath))
      {
        continue;
      }

      var meta = JsonSerializer.Deserialize<EntryMetadata>(File.ReadAllText(metaPath), Json);
      if (meta is null)
      {
        continue;
      }

      var sha = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(zip)));
      entries.Add(new RepositoryEntry
      {
        Id = meta.Id,
        Name = meta.Name,
        Version = meta.Version,
        Description = meta.Description,
        Author = meta.Author,
        PackageUrl = $"packages/{Path.GetFileName(zip)}",
        SignatureUrl = $"packages/{Path.GetFileName(sigPath)}",
        Sha256 = sha,
      });
    }

    return new RepositoryIndex { Plugins = entries };
  }
}
