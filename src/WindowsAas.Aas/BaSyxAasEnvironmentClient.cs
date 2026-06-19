using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WindowsAas.Abstractions.Submodels;

namespace WindowsAas.Aas;

/// <summary>
/// <see cref="IAasEnvironmentClient"/> implementation that speaks the AAS v3 REST API
/// exposed by the BaSyx <c>aas-environment</c> container.
/// </summary>
public sealed class BaSyxAasEnvironmentClient(
  HttpClient httpClient,
  IOptions<AasOptions> options,
  ILogger<BaSyxAasEnvironmentClient> logger) : IAasEnvironmentClient
{
  private readonly AasOptions _options = options.Value;

  public async Task EnsureHostShellAsync(CancellationToken ct = default)
  {
    var shell = new JsonObject
    {
      ["modelType"] = "AssetAdministrationShell",
      ["id"] = _options.HostShellId,
      ["idShort"] = _options.HostShellIdShort,
      ["assetInformation"] = new JsonObject { ["assetKind"] = "Instance" },
      ["submodels"] = new JsonArray(),
    };

    // POST is idempotent enough for our purposes: a 409 means the shell already exists.
    using var response = await PostJsonAsync($"{_options.EnvironmentUrl}/shells", shell, ct);
    if (response.StatusCode is HttpStatusCode.Conflict)
    {
      logger.LogDebug("Host AAS shell {ShellId} already exists.", _options.HostShellId);
      return;
    }

    response.EnsureSuccessStatusCode();
    logger.LogInformation("Registered host AAS shell {ShellId}.", _options.HostShellId);
  }

  public async Task PutSubmodelAsync(SubmodelDefinition submodel, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(submodel);
    var json = SubmodelJsonMapper.ToJson(submodel);
    var encodedId = AasIdentifier.Encode(submodel.Id);

    using var request = new HttpRequestMessage(HttpMethod.Put, $"{_options.EnvironmentUrl}/submodels/{encodedId}")
    {
      Content = JsonContent(json),
    };
    using var response = await httpClient.SendAsync(request, ct);
    response.EnsureSuccessStatusCode();
    logger.LogInformation("Registered submodel {SubmodelId}.", submodel.Id);
  }

  public async Task DeleteSubmodelAsync(string submodelId, CancellationToken ct = default)
  {
    var encodedId = AasIdentifier.Encode(submodelId);
    using var response = await httpClient.DeleteAsync($"{_options.EnvironmentUrl}/submodels/{encodedId}", ct);
    if (response.StatusCode is not HttpStatusCode.NotFound)
    {
      response.EnsureSuccessStatusCode();
    }

    logger.LogInformation("Removed submodel {SubmodelId}.", submodelId);
  }

  public async Task SetElementValueAsync(
    string submodelId,
    string idShortPath,
    string value,
    CancellationToken ct = default)
  {
    var encodedId = AasIdentifier.Encode(submodelId);
    var url = $"{_options.EnvironmentUrl}/submodels/{encodedId}/submodel-elements/{idShortPath}/$value";

    using var request = new HttpRequestMessage(HttpMethod.Patch, url)
    {
      Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json"),
    };
    using var response = await httpClient.SendAsync(request, ct);
    response.EnsureSuccessStatusCode();
  }

  private Task<HttpResponseMessage> PostJsonAsync(string url, JsonNode body, CancellationToken ct) =>
    httpClient.PostAsync(url, JsonContent(body), ct);

  private static StringContent JsonContent(JsonNode node) =>
    new(node.ToJsonString(), Encoding.UTF8, "application/json");
}
