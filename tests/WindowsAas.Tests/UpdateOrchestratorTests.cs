using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using WindowsAas.Tests.Testing;
using WindowsAas.Updater;
using Xunit;

namespace WindowsAas.Tests;

public class UpdateOrchestratorTests
{
  private static readonly byte[] MsiBytes = "fake-msi-bytes"u8.ToArray();

  [Fact]
  public async Task ApplyAsync_installs_when_hash_and_signature_are_valid()
  {
    var installer = new FakeInstaller();
    var orchestrator = CreateOrchestrator(authenticodeValid: true, installer, out _);
    var update = new UpdateInfo
    {
      Version = "1.2.3",
      MsiUrl = "https://example.test/update.msi",
      Sha256 = Convert.ToHexStringLower(SHA256.HashData(MsiBytes)),
    };

    var applied = await orchestrator.ApplyAsync(update);

    applied.ShouldBeTrue();
    installer.InstalledPaths.ShouldHaveSingleItem();
    File.Exists(installer.InstalledPaths[0]).ShouldBeTrue();
  }

  [Fact]
  public async Task ApplyAsync_rejects_a_sha256_mismatch_without_installing()
  {
    var installer = new FakeInstaller();
    var orchestrator = CreateOrchestrator(authenticodeValid: true, installer, out _);
    var update = new UpdateInfo
    {
      Version = "1.2.3",
      MsiUrl = "https://example.test/update.msi",
      Sha256 = Convert.ToHexStringLower(SHA256.HashData("different-bytes"u8.ToArray())),
    };

    var applied = await orchestrator.ApplyAsync(update);

    applied.ShouldBeFalse();
    installer.InstalledPaths.ShouldBeEmpty();
  }

  [Fact]
  public async Task ApplyAsync_rejects_a_failed_authenticode_validation_and_deletes_the_file()
  {
    var installer = new FakeInstaller();
    var orchestrator = CreateOrchestrator(authenticodeValid: false, installer, out var capturedPath);
    var update = new UpdateInfo { Version = "1.2.3", MsiUrl = "https://example.test/update.msi" };

    var applied = await orchestrator.ApplyAsync(update);

    applied.ShouldBeFalse();
    installer.InstalledPaths.ShouldBeEmpty();
    File.Exists(capturedPath.Value).ShouldBeFalse();
  }

  [Fact]
  public async Task ApplyAsync_skips_the_hash_check_when_none_is_published()
  {
    var installer = new FakeInstaller();
    var orchestrator = CreateOrchestrator(authenticodeValid: true, installer, out _);
    var update = new UpdateInfo { Version = "1.2.3", MsiUrl = "https://example.test/update.msi" };

    var applied = await orchestrator.ApplyAsync(update);

    applied.ShouldBeTrue();
    installer.InstalledPaths.ShouldHaveSingleItem();
  }

  private static UpdateOrchestrator CreateOrchestrator(
    bool authenticodeValid, FakeInstaller installer, out StrongBox<string?> capturedPath)
  {
    capturedPath = new StrongBox<string?>();
    var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new ByteArrayContent(MsiBytes),
    });
    var httpClient = new HttpClient(handler);
    var authenticode = new FakeAuthenticodeValidator(authenticodeValid, capturedPath);

    return new UpdateOrchestrator(httpClient, authenticode, installer, NullLogger<UpdateOrchestrator>.Instance);
  }

  private sealed class FakeAuthenticodeValidator(bool result, StrongBox<string?> capturedPath) : IAuthenticodeValidator
  {
    public bool Validate(string path)
    {
      capturedPath.Value = path;
      return result;
    }
  }

  private sealed class FakeInstaller : IUpdateInstaller
  {
    public List<string> InstalledPaths { get; } = [];

    public void Install(string msiPath) => InstalledPaths.Add(msiPath);
  }
}
