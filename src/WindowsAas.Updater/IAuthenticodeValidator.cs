namespace WindowsAas.Updater;

/// <summary>
/// Validates the Authenticode signature of a downloaded installer before it is run.
/// Mitigates Tampering / Spoofing for the update channel (STRIDE).
/// </summary>
public interface IAuthenticodeValidator
{
  /// <summary>
  /// Returns <c>true</c> when the file at <paramref name="path"/> is signed by the
  /// expected publisher with a trusted certificate chain.
  /// </summary>
  bool Validate(string path);
}
