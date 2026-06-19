using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace WindowsAas.Security;

/// <summary>
/// Windows DPAPI-based <see cref="ISecretProtector"/>. Uses
/// <see cref="DataProtectionScope.LocalMachine"/> so the system-level service
/// (running as LocalSystem) can decrypt regardless of the interactive user, while
/// the ciphertext remains bound to this machine.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiSecretProtector : ISecretProtector
{
  // Extra entropy mixed into the protected blob to scope it to this application.
  private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("WindowsAAS.v1");

  public string Protect(string plaintext)
  {
    ArgumentNullException.ThrowIfNull(plaintext);
    var bytes = Encoding.UTF8.GetBytes(plaintext);
    var protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.LocalMachine);
    return Convert.ToBase64String(protectedBytes);
  }

  public string Unprotect(string protectedValue)
  {
    ArgumentNullException.ThrowIfNull(protectedValue);
    var protectedBytes = Convert.FromBase64String(protectedValue);
    var bytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.LocalMachine);
    return Encoding.UTF8.GetString(bytes);
  }
}
