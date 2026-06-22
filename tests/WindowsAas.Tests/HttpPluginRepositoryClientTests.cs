using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using WindowsAas.Repository.Client;
using WindowsAas.Security;
using WindowsAas.Tests.Testing;
using Xunit;

namespace WindowsAas.Tests;

public class HttpPluginRepositoryClientTests : IDisposable
{
  private readonly string _pluginsDirectory = Directory.CreateTempSubdirectory("waas-repo").FullName;
  private static readonly byte[] Signature = "fake-signature"u8.ToArray();

  [Fact]
  public async Task InstallAsync_extracts_a_package_that_passes_integrity_and_signature_checks()
  {
    var package = CreateZip(("plugin.json", "{}"));
    var client = CreateClient(package, verifierResult: true);
    var entry = CreateEntry(package);

    await client.InstallAsync(entry, _pluginsDirectory);

    var extracted = Path.Combine(_pluginsDirectory, entry.Id, "plugin.json");
    File.Exists(extracted).ShouldBeTrue();
  }

  [Fact]
  public async Task InstallAsync_rejects_a_package_whose_hash_does_not_match()
  {
    var package = CreateZip(("plugin.json", "{}"));
    var client = CreateClient(package, verifierResult: true);
    var entry = CreateEntry(package) with { Sha256 = Convert.ToHexStringLower(SHA256.HashData("tampered"u8.ToArray())) };

    await Should.ThrowAsync<InvalidOperationException>(() => client.InstallAsync(entry, _pluginsDirectory));
    Directory.Exists(Path.Combine(_pluginsDirectory, entry.Id)).ShouldBeFalse();
  }

  [Fact]
  public async Task InstallAsync_rejects_a_package_with_an_untrusted_signature()
  {
    var package = CreateZip(("plugin.json", "{}"));
    var client = CreateClient(package, verifierResult: false);
    var entry = CreateEntry(package);

    await Should.ThrowAsync<InvalidOperationException>(() => client.InstallAsync(entry, _pluginsDirectory));
    Directory.Exists(Path.Combine(_pluginsDirectory, entry.Id)).ShouldBeFalse();
  }

  [Fact]
  public async Task InstallAsync_refuses_to_extract_an_entry_that_escapes_the_target_directory()
  {
    var package = CreateZip(("../escaped.txt", "evil"));
    var client = CreateClient(package, verifierResult: true);
    var entry = CreateEntry(package);

    await Should.ThrowAsync<IOException>(() => client.InstallAsync(entry, _pluginsDirectory));
    File.Exists(Path.Combine(_pluginsDirectory, "escaped.txt")).ShouldBeFalse();
  }

  public void Dispose() => Directory.Delete(_pluginsDirectory, recursive: true);

  private static RepositoryEntry CreateEntry(byte[] package) => new()
  {
    Id = "demo-plugin",
    Name = "Demo",
    Version = "1.0.0",
    PackageUrl = "https://example.test/demo-plugin.zip",
    SignatureUrl = "https://example.test/demo-plugin.zip.sig",
    Sha256 = Convert.ToHexStringLower(SHA256.HashData(package)),
  };

  private static HttpPluginRepositoryClient CreateClient(byte[] package, bool verifierResult)
  {
    var handler = new FakeHttpMessageHandler(request =>
    {
      var bytes = request.RequestUri!.AbsoluteUri.EndsWith(".sig", StringComparison.Ordinal) ? Signature : package;
      return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
    });
    var httpClient = new HttpClient(handler);
    var verifier = new FakeVerifier(verifierResult);
    var options = Options.Create(new RepositoryOptions { BaseUrl = "https://example.test" });

    return new HttpPluginRepositoryClient(
      httpClient, verifier, options, NullLogger<HttpPluginRepositoryClient>.Instance);
  }

  private static byte[] CreateZip(params (string Name, string Content)[] entries)
  {
    using var stream = new MemoryStream();
    using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
    {
      foreach (var (name, content) in entries)
      {
        var zipEntry = archive.CreateEntry(name);
        using var writer = new StreamWriter(zipEntry.Open());
        writer.Write(content);
      }
    }

    return stream.ToArray();
  }

  private sealed class FakeVerifier(bool result) : IPackageVerifier
  {
    public bool Verify(Stream package, ReadOnlySpan<byte> signature) => result;
  }
}
