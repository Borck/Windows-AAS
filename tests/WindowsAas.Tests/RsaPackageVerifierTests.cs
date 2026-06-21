using System.Security.Cryptography;
using System.Text;
using Shouldly;
using WindowsAas.Security;
using Xunit;

namespace WindowsAas.Tests;

public class RsaPackageVerifierTests
{
  [Fact]
  public void Verify_accepts_a_correctly_signed_package()
  {
    using var rsa = RSA.Create(2048);
    var package = Encoding.UTF8.GetBytes("plugin-bytes");
    var signature = SignSha256(rsa, package);

    var verifier = new RsaPackageVerifier([rsa.ExportSubjectPublicKeyInfoPem()]);

    verifier.Verify(new MemoryStream(package), signature).ShouldBeTrue();
  }

  [Fact]
  public void Verify_rejects_a_tampered_package()
  {
    using var rsa = RSA.Create(2048);
    var signature = SignSha256(rsa, Encoding.UTF8.GetBytes("original"));
    var verifier = new RsaPackageVerifier([rsa.ExportSubjectPublicKeyInfoPem()]);

    verifier.Verify(new MemoryStream(Encoding.UTF8.GetBytes("tampered")), signature).ShouldBeFalse();
  }

  [Fact]
  public void Verify_rejects_an_untrusted_signer()
  {
    using var signer = RSA.Create(2048);
    using var other = RSA.Create(2048);
    var package = Encoding.UTF8.GetBytes("plugin-bytes");
    var signature = SignSha256(signer, package);

    var verifier = new RsaPackageVerifier([other.ExportSubjectPublicKeyInfoPem()]);

    verifier.Verify(new MemoryStream(package), signature).ShouldBeFalse();
  }

  private static byte[] SignSha256(RSA rsa, byte[] data)
  {
    var hash = SHA256.HashData(data);
    return rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
  }
}
