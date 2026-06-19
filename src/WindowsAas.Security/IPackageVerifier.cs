namespace WindowsAas.Security;

/// <summary>
/// Verifies the authenticity of a downloaded plugin package before the plugin host
/// installs it. Plugins must carry a detached signature produced by a trusted
/// publisher key; unsigned or tampered packages are rejected (mitigates Tampering
/// and Spoofing from the STRIDE analysis).
/// </summary>
public interface IPackageVerifier
{
  /// <summary>
  /// Returns <c>true</c> when <paramref name="signature"/> is a valid signature over
  /// the bytes of <paramref name="package"/> for one of the trusted publisher keys.
  /// </summary>
  bool Verify(Stream package, ReadOnlySpan<byte> signature);
}
