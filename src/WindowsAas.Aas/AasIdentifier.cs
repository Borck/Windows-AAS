using System.Buffers.Text;
using System.Text;

namespace WindowsAas.Aas;

/// <summary>
/// Helpers for the BaSyx / AAS v3 REST convention of addressing identifiables by
/// their <b>base64url-encoded</b> identifier in the URL path.
/// </summary>
public static class AasIdentifier
{
  /// <summary>Encodes an AAS identifier (IRI/IRDI) for use in a REST URL segment.</summary>
  public static string Encode(string identifier)
  {
    ArgumentNullException.ThrowIfNull(identifier);
    var bytes = Encoding.UTF8.GetBytes(identifier);
    return Base64Url.EncodeToString(bytes);
  }

  /// <summary>Reverses <see cref="Encode"/>.</summary>
  public static string Decode(string encoded)
  {
    ArgumentNullException.ThrowIfNull(encoded);
    var bytes = Base64Url.DecodeFromChars(encoded);
    return Encoding.UTF8.GetString(bytes);
  }
}
