using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using WindowsAas.Tests.Testing;
using WindowsAas.Updater;
using Xunit;

namespace WindowsAas.Tests;

public class GitHubUpdateCheckerTests
{
  [Fact]
  public async Task GetAvailableUpdateAsync_returns_the_msi_asset_when_a_newer_release_exists()
  {
    var checker = CreateChecker(currentVersion: "1.0.0", releaseJson: """
      {
        "tag_name": "v1.1.0",
        "assets": [
          { "name": "WindowsAAS-1.1.0.msi", "browser_download_url": "https://example.test/WindowsAAS-1.1.0.msi" }
        ]
      }
      """);

    var update = await checker.GetAvailableUpdateAsync();

    update.ShouldNotBeNull();
    update.Version.ShouldBe("1.1.0");
    update.MsiUrl.ShouldBe("https://example.test/WindowsAAS-1.1.0.msi");
  }

  [Fact]
  public async Task GetAvailableUpdateAsync_returns_null_when_the_release_is_not_newer()
  {
    var checker = CreateChecker(currentVersion: "1.1.0", releaseJson: """
      { "tag_name": "v1.1.0", "assets": [] }
      """);

    var update = await checker.GetAvailableUpdateAsync();

    update.ShouldBeNull();
  }

  [Fact]
  public async Task GetAvailableUpdateAsync_returns_null_when_the_tag_is_unparseable()
  {
    var checker = CreateChecker(currentVersion: "1.0.0", releaseJson: """
      { "tag_name": "not-a-version", "assets": [] }
      """);

    var update = await checker.GetAvailableUpdateAsync();

    update.ShouldBeNull();
  }

  [Fact]
  public async Task GetAvailableUpdateAsync_returns_null_when_no_msi_asset_is_published()
  {
    var checker = CreateChecker(currentVersion: "1.0.0", releaseJson: """
      {
        "tag_name": "v1.1.0",
        "assets": [
          { "name": "WindowsAAS-1.1.0.zip", "browser_download_url": "https://example.test/WindowsAAS-1.1.0.zip" }
        ]
      }
      """);

    var update = await checker.GetAvailableUpdateAsync();

    update.ShouldBeNull();
  }

  private static GitHubUpdateChecker CreateChecker(string currentVersion, string releaseJson)
  {
    var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(releaseJson, Encoding.UTF8, "application/json"),
    });
    var httpClient = new HttpClient(handler);
    var options = Options.Create(new UpdaterOptions
    {
      CurrentVersion = currentVersion,
      FeedUrl = "https://api.github.com/repos/Borck/Windows-AAS/releases/latest",
    });

    return new GitHubUpdateChecker(httpClient, options, NullLogger<GitHubUpdateChecker>.Instance);
  }
}
