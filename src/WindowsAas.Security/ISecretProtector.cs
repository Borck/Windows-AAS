namespace WindowsAas.Security;

/// <summary>
/// Encrypts/decrypts secret configuration values at rest (connection strings,
/// broker credentials, plugin secrets). The default Windows implementation binds
/// the ciphertext to the local machine so only this host can read it.
/// </summary>
public interface ISecretProtector
{
  /// <summary>Encrypts <paramref name="plaintext"/> and returns base64 ciphertext.</summary>
  string Protect(string plaintext);

  /// <summary>Reverses <see cref="Protect"/>.</summary>
  string Unprotect(string protectedValue);
}
