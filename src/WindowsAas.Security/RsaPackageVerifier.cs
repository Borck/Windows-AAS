using System.Security.Cryptography;

namespace WindowsAas.Security;

/// <summary>
/// <see cref="IPackageVerifier"/> that validates an RSA + SHA-256 detached signature
/// against a set of trusted publisher public keys (PEM-encoded).
/// </summary>
public sealed class RsaPackageVerifier : IPackageVerifier
{
  private readonly IReadOnlyList<RSA> _trustedKeys;

  /// <param name="trustedPublicKeysPem">One or more PEM-encoded RSA public keys.</param>
  public RsaPackageVerifier(IEnumerable<string> trustedPublicKeysPem)
  {
    ArgumentNullException.ThrowIfNull(trustedPublicKeysPem);
    var keys = new List<RSA>();
    foreach (var pem in trustedPublicKeysPem)
    {
      var rsa = RSA.Create();
      rsa.ImportFromPem(pem);
      keys.Add(rsa);
    }

    _trustedKeys = keys;
  }

  public bool Verify(Stream package, ReadOnlySpan<byte> signature)
  {
    ArgumentNullException.ThrowIfNull(package);

    using var sha = SHA256.Create();
    var hash = sha.ComputeHash(package);

    foreach (var key in _trustedKeys)
    {
      if (key.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
      {
        return true;
      }
    }

    return false;
  }
}
