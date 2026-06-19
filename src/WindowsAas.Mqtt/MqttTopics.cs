using System.Buffers.Text;
using System.Text;

namespace WindowsAas.Mqtt;

/// <summary>
/// Topic conventions for the host ↔ AAS bridge. Submodel ids are IRIs that may
/// contain <c>/</c> and <c>:</c>, so they are base64url-encoded into a single
/// topic level to keep the hierarchy well-formed.
/// <para>
/// Layout (under <see cref="MqttOptions.TopicPrefix"/>):
/// <list type="bullet">
///   <item><c>{prefix}/{sm}/{path}/set</c> — desired value, AAS → host (control path).</item>
///   <item><c>{prefix}/{sm}/{path}/value</c> — actual value, host → AAS (telemetry path).</item>
/// </list>
/// </para>
/// </summary>
public sealed class MqttTopics(string prefix)
{
  private readonly string _prefix = prefix.TrimEnd('/');

  /// <summary>Topic a plugin publishes the actual value to (host → AAS).</summary>
  public string Value(string submodelId, string idShortPath) =>
    $"{_prefix}/{Encode(submodelId)}/{idShortPath}/value";

  /// <summary>Topic the host subscribes to for desired-value writes (AAS → host).</summary>
  public string Set(string submodelId, string idShortPath) =>
    $"{_prefix}/{Encode(submodelId)}/{idShortPath}/set";

  /// <summary>Wildcard subscription that captures every inbound write.</summary>
  public string AllSets() => $"{_prefix}/+/#";

  /// <summary>
  /// Parses a <c>/set</c> topic back into its (submodelId, idShortPath) parts.
  /// Returns <c>false</c> for topics that are not inbound writes.
  /// </summary>
  public bool TryParseSet(string topic, out string submodelId, out string idShortPath)
  {
    submodelId = string.Empty;
    idShortPath = string.Empty;

    if (!topic.StartsWith(_prefix + "/", StringComparison.Ordinal) ||
        !topic.EndsWith("/set", StringComparison.Ordinal))
    {
      return false;
    }

    var inner = topic[(_prefix.Length + 1)..^"/set".Length];
    var separator = inner.IndexOf('/', StringComparison.Ordinal);
    if (separator <= 0 || separator == inner.Length - 1)
    {
      return false;
    }

    submodelId = Decode(inner[..separator]);
    idShortPath = inner[(separator + 1)..];
    return true;
  }

  private static string Encode(string value) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(value));

  private static string Decode(string value) => Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value));
}
